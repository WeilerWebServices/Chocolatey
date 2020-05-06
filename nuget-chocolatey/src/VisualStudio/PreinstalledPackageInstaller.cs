﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    /// <summary>
    /// Provides functionality for installing packages already on disk from an installer (MSI or VSIX).
    /// </summary>
    internal class PreinstalledPackageInstaller
    {
        private const string RegistryKeyRoot = @"SOFTWARE\NuGet\Repository";
        private readonly IVsWebsiteHandler _websiteHandler;
        private readonly IVsPackageInstallerServices _packageServices;
        private readonly IVsCommonOperations _vsCommonOperations;
        private readonly ISolutionManager _solutionManager;

        public Action<string> InfoHandler { get; set; }

        public PreinstalledPackageInstaller(
            IVsWebsiteHandler websiteHandler,
            IVsPackageInstallerServices packageServices,
            IVsCommonOperations vsCommonOperations,
            ISolutionManager solutionManager)
        {
            _websiteHandler = websiteHandler;
            _packageServices = packageServices;
            _vsCommonOperations = vsCommonOperations;
            _solutionManager = solutionManager;
        }

        /// <summary>
        /// Gets the folder location where packages have been laid down for the specified extension.
        /// </summary>
        /// <param name="extensionId">The installed extension.</param>
        /// <param name="vsExtensionManager">The VS Extension manager instance.</param>
        /// <param name="throwingErrorHandler">An error handler that accepts the error message string and then throws the appropriate exception.</param>
        /// <returns>The absolute path to the extension's packages folder.</returns>
        internal string GetExtensionRepositoryPath(string extensionId, object vsExtensionManager, Action<string> throwingErrorHandler)
        {
            var extensionManagerShim = new ExtensionManagerShim(vsExtensionManager, throwingErrorHandler);
            string installPath;

            if (!extensionManagerShim.TryGetExtensionInstallPath(extensionId, out installPath))
            {
                throwingErrorHandler(String.Format(VsResources.PreinstalledPackages_InvalidExtensionId,
                    extensionId));
                Debug.Fail("The throwingErrorHandler did not throw");
            }

            return Path.Combine(installPath, "Packages");
        }

        /// <summary>
        /// Gets the folder location where packages have been laid down in a registry-specified location.
        /// </summary>
        /// <param name="keyName">The registry key name that specifies the packages location.</param>
        /// <param name="registryKeys">The optional list of parent registry keys to look in (used for unit tests).</param>
        /// <param name="throwingErrorHandler">An error handler that accepts the error message string and then throws the appropriate exception.</param>
        /// <returns>The absolute path to the packages folder specified in the registry.</returns>
        internal string GetRegistryRepositoryPath(string keyName, IEnumerable<IRegistryKey> registryKeys, Action<string> throwingErrorHandler)
        {
            IRegistryKey repositoryKey = null;
            string repositoryValue = null;

            // When pulling the repository from the registry, use CurrentUser first, falling back onto LocalMachine
            registryKeys = registryKeys ??
                new[] {
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.CurrentUser),
                            new RegistryKeyWrapper(Microsoft.Win32.Registry.LocalMachine)
                      };

            // Find the first registry key that supplies the necessary subkey/value
            foreach (var registryKey in registryKeys)
            {
                repositoryKey = registryKey.OpenSubKey(RegistryKeyRoot);

                if (repositoryKey != null)
                {
                    repositoryValue = repositoryKey.GetValue(keyName) as string;

                    if (!String.IsNullOrEmpty(repositoryValue))
                    {
                        break;
                    }

                    repositoryKey.Close();
                }
            }

            if (repositoryKey == null)
            {
                throwingErrorHandler(String.Format(VsResources.PreinstalledPackages_RegistryKeyError, RegistryKeyRoot));
                Debug.Fail("throwingErrorHandler did not throw");
            }

            if (String.IsNullOrEmpty(repositoryValue))
            {
                throwingErrorHandler(String.Format(VsResources.PreinstalledPackages_InvalidRegistryValue, keyName, RegistryKeyRoot));
                Debug.Fail("throwingErrorHandler did not throw");
            }

            // Ensure a trailing slash so that the path always gets read as a directory
            repositoryValue = PathUtility.EnsureTrailingSlash(repositoryValue);

            return Path.GetDirectoryName(repositoryValue);
        }

        /// <summary>
        /// Installs one or more packages into the specified project.
        /// </summary>
        /// <param name="packageInstaller">The package installer service that performs the actual package installation.</param>
        /// <param name="project">The target project for installation.</param>
        /// <param name="configuration">The packages to install, where to install them from, and additional options for their installation.</param>
        /// <param name="repositorySettings">The repository settings for the packages being installed.</param>
        /// <param name="warningHandler">An action that accepts a warning message and presents it to the user, allowing execution to continue.</param>
        /// <param name="errorHandler">An action that accepts an error message and presents it to the user, allowing execution to continue.</param>
        internal void PerformPackageInstall(
            IVsPackageInstaller packageInstaller,
            Project project,
            PreinstalledPackageConfiguration configuration,
            Lazy<IRepositorySettings> repositorySettings,
            Action<string> warningHandler,
            Action<string> errorHandler)
        {
            string repositoryPath = configuration.RepositoryPath;
            var failedPackageErrors = new List<string>();

            IPackageRepository repository = configuration.IsPreunzipped
                                                ? (IPackageRepository)new UnzippedPackageRepository(repositoryPath)
                                                : (IPackageRepository)new LocalPackageRepository(repositoryPath);

            foreach (var package in configuration.Packages)
            {
                // Does the project already have this package installed?
                if (_packageServices.IsPackageInstalled(project, package.Id))
                {
                    // If so, is it the right version?
                    if (!_packageServices.IsPackageInstalled(project, package.Id, package.Version))
                    {
                        // No? Raise a warning (likely written to the Output window) and ignore this package.
                        warningHandler(String.Format(VsResources.PreinstalledPackages_VersionConflict, package.Id, package.Version));
                    }
                    // Yes? Just silently ignore this package!
                }
                else
                {
                    try
                    {
                        if (InfoHandler != null)
                        {
                            InfoHandler(String.Format(CultureInfo.CurrentCulture, VsResources.PreinstalledPackages_PackageInstallStatus, package.Id, package.Version));
                        }

                        packageInstaller.InstallPackage(repository, project, package.Id, package.Version.ToString(), ignoreDependencies: package.IgnoreDependencies, skipAssemblyReferences: package.SkipAssemblyReferences);
                    }
                    catch (InvalidOperationException exception)
                    {
                        failedPackageErrors.Add(package.Id + "." + package.Version + " : " + exception.Message);
                    }
                }
            }

            if (failedPackageErrors.Any())
            {
                var errorString = new StringBuilder();
                errorString.AppendFormat(VsResources.PreinstalledPackages_FailedToInstallPackage, repositoryPath);
                errorString.AppendLine();
                errorString.AppendLine();
                errorString.Append(String.Join(Environment.NewLine, failedPackageErrors));

                errorHandler(errorString.ToString());
            }

            // RepositorySettings = null in unit tests
            if (project.IsWebSite() && repositorySettings != null)
            {
                using (_vsCommonOperations.SaveSolutionExplorerNodeStates(_solutionManager))
                {
                    CreateRefreshFilesInBin(
                        project,
                        repositorySettings.Value.RepositoryPath,
                        configuration.Packages.Where(p => p.SkipAssemblyReferences));

                    CopyNativeBinariesToBin(project, repositorySettings.Value.RepositoryPath, configuration.Packages);
                }
            }
        }

        /// <summary>
        /// For Website projects, adds necessary "refresh" files in the bin folder for added references.
        /// </summary>
        /// <param name="project">The target Website project.</param>
        /// <param name="repositoryPath">The local repository path.</param>
        /// <param name="packageInfos">The packages that were installed.</param>
        private void CreateRefreshFilesInBin(Project project, string repositoryPath, IEnumerable<PreinstalledPackageInfo> packageInfos)
        {
            IEnumerable<PackageName> packageNames = packageInfos.Select(pi => new PackageName(pi.Id, pi.Version));
            _websiteHandler.AddRefreshFilesForReferences(project, new PhysicalFileSystem(repositoryPath), packageNames);
        }

        /// <summary>
        /// By convention, we copy all files under the NativeBinaries folder under package root to the bin folder of the Website.
        /// </summary>
        /// <param name="project">The target Website project.</param>
        /// <param name="repositoryPath">The local repository path.</param>
        /// <param name="packageInfos">The packages that were installed.</param>
        private void CopyNativeBinariesToBin(Project project, string repositoryPath, IEnumerable<PreinstalledPackageInfo> packageInfos)
        {
            IEnumerable<PackageName> packageNames = packageInfos.Select(pi => new PackageName(pi.Id, pi.Version));
            _websiteHandler.CopyNativeBinaries(project, new PhysicalFileSystem(repositoryPath), packageNames);
        }

        private class ExtensionManagerShim
        {
            private static Type _iInstalledExtensionType;
            private static Type _iVsExtensionManagerType;
            private static PropertyInfo _installPathProperty;
            private static Type _sVsExtensionManagerType;
            private static MethodInfo _tryGetInstalledExtensionMethod;
            private static bool _typesInitialized;

            private readonly object _extensionManager;

            public ExtensionManagerShim(object extensionManager, Action<string> errorHandler)
            {
                InitializeTypes(errorHandler);
                _extensionManager = extensionManager ?? Package.GetGlobalService(_sVsExtensionManagerType);
            }

            private static void InitializeTypes(Action<string> errorHandler)
            {
                if (_typesInitialized)
                {
                    return;
                }

                try
                {
                    Assembly extensionManagerAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .First(a => a.FullName.StartsWith("Microsoft.VisualStudio.ExtensionManager,"));
                    _sVsExtensionManagerType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager");
                    _iVsExtensionManagerType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.IVsExtensionManager");
                    _iInstalledExtensionType =
                        extensionManagerAssembly.GetType("Microsoft.VisualStudio.ExtensionManager.IInstalledExtension");
                    _tryGetInstalledExtensionMethod = _iVsExtensionManagerType.GetMethod("TryGetInstalledExtension",
                        new[] { typeof(string), _iInstalledExtensionType.MakeByRefType() });
                    _installPathProperty = _iInstalledExtensionType.GetProperty("InstallPath", typeof(string));
                    if (_installPathProperty == null || _tryGetInstalledExtensionMethod == null ||
                        _sVsExtensionManagerType == null)
                    {
                        throw new Exception();
                    }

                    _typesInitialized = true;
                }
                catch
                {
                    // if any of the types or methods cannot be loaded throw an error. this indicates that some API in
                    // Microsoft.VisualStudio.ExtensionManager got changed.
                    errorHandler(VsResources.PreinstalledPackages_ExtensionManagerError);
                }
            }

            public bool TryGetExtensionInstallPath(string extensionId, out string installPath)
            {
                installPath = null;
                object[] parameters = new object[] { extensionId, null };
                bool result = (bool)_tryGetInstalledExtensionMethod.Invoke(_extensionManager, parameters);
                if (!result)
                {
                    return false;
                }
                object extension = parameters[1];
                installPath = _installPathProperty.GetValue(extension, index: null) as string;
                return true;
            }
        }
    }
}
