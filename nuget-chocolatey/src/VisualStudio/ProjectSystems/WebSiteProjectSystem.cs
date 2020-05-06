using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using EnvDTE;
using NuGet.VisualStudio.Resources;
using VsWebSite;

namespace NuGet.VisualStudio
{
    public class WebSiteProjectSystem : WebProjectSystem, IBatchProcessor<string>
    {
        private const string RootNamespace = "RootNamespace";
        private const string DefaultNamespace = "ASP";
        private const string AppCodeFolder = "App_Code";
        private const string GeneratedFilesFolder = "Generated___Files";

        private readonly HashSet<string> _excludedCodeFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] _sourceFileExtensions = new[] { ".cs", ".vb" };

        public WebSiteProjectSystem(Project project, IFileSystemProvider fileSystemProvider)
            : base(project, fileSystemProvider)
        {
        }

        public override string ProjectName
        {
            get
            {
                string path = Project.GetFullPath();
                return Path.GetFileName(path);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to catch all exceptions")]
        public override void AddReference(string referencePath, Stream stream)
        {
            string name = Path.GetFileNameWithoutExtension(referencePath);
            try
            {
                Project.GetAssemblyReferences().AddFromFile(PathUtility.GetAbsolutePath(Root, referencePath));
                
                // Always create a refresh file. Vs does this for us in most cases, however for GACed binaries, it resorts to adding a web.config entry instead.
                // This may result in deployment issues. To work around ths, we'll always attempt to add a file to the bin.
                this.CreateRefreshFile(PathUtility.GetAbsolutePath(Root, referencePath));

                Logger.Log(MessageLevel.Debug, VsResources.Debug_AddReference, name, ProjectName);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, VsResources.FailedToAddReference, name), e);
            }
        }

        public override void RemoveReference(string name)
        {
            // Remove the reference via DTE.
            RemoveDTEReference(name);
            
            // For GACed binaries, VS would not clear the refresh files for us since it assumes the reference exists in web.config. 
            // We'll clean up any remaining .refresh files.
            var refreshFilePath = Path.Combine("bin", Path.GetFileName(name) + ".refresh");
            if (FileExists(refreshFilePath))
            {
                try
                {
                    DeleteFile(refreshFilePath);
                }
                catch (Exception e)
                {
                    Logger.Log(MessageLevel.Warning, e.Message);
                }
            }
        }

        public override string ResolvePath(string path)
        {
            // If we're adding a source file that isn't already in the app code folder then add App_Code to the path
            if (RequiresAppCodeRemapping(path))
            {
                path = Path.Combine(AppCodeFolder, path);
            }

            return base.ResolvePath(path);
        }

        public override IEnumerable<string> GetDirectories(string path)
        {
            if (IsUnderAppCode(path))
            {
                // There is an invisible folder called Generated___Files under app code that we want to exclude from our search
                return base.GetDirectories(path).Except(new[] { GeneratedFilesFolder }, StringComparer.OrdinalIgnoreCase);
            }
            return base.GetDirectories(path);
        }

        protected override void AddGacReference(string name)
        {
            Project.GetAssemblyReferences().AddFromGAC(name);
        }

        public override dynamic GetPropertyValue(string propertyName)
        {
            if (propertyName.Equals(RootNamespace, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultNamespace;
            }
            return base.GetPropertyValue(propertyName);
        }

        protected override bool ExcludeFile(string path)
        {
            // Exclude nothing from website projects
            return false;
        }

        public void BeginProcessing(IEnumerable<string> batch, PackageAction action)
        {
            var files = batch.OrderBy(path => path)
                             .ToList();

            foreach (var path1 in files)
            {
                foreach (var path2 in files)
                {
                    if (path1.Equals(path2, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (path1.StartsWith(path2, StringComparison.OrdinalIgnoreCase) &&
                        IsSourceFile(path1))
                    {
                        _excludedCodeFiles.Add(path1);
                    }
                }
            }
        }

        public void EndProcessing()
        {
            _excludedCodeFiles.Clear();
        }

        public override void AddImport(string targetPath, ProjectImportLocation location)
        {
            // Web sites are not msbuild based and do not support imports.
        }

        public override void RemoveImport(string targetPath)
        {
            // Web sites are not msbuild based and do not support imports.
        }

        /// <summary>
        /// Determines if we need a source file to be under the App_Code folder
        /// </summary>
        private bool RequiresAppCodeRemapping(string path)
        {
            return !_excludedCodeFiles.Contains(path) && !IsUnderAppCode(path) && IsSourceFile(path);
        }

        /// <summary>
        /// Removes a reference via the DTE. 
        /// </summary>
        /// <remarks>This is identical to VsProjectSystem.RemoveReference except in the way we process exceptions.</remarks>
        private void RemoveDTEReference(string name)
        {
            // Get the reference name without extension
            string referenceName = Path.GetFileNameWithoutExtension(name);

            // Remove the reference from the project
            AssemblyReference reference = null;
            try
            {
                reference = Project.GetAssemblyReferences().Item(referenceName);
                if (reference != null)
                {
                    reference.Remove();
                    Logger.Log(MessageLevel.Debug, VsResources.Debug_RemoveReference, name, ProjectName);
                }
            }
            catch (Exception ex)
            {
                MessageLevel messageLevel = MessageLevel.Warning;
                if (reference != null && reference.ReferenceKind == AssemblyReferenceType.AssemblyReferenceConfig)
                {
                    // Bug 2319: Strong named assembly references are specified via config and may be specified in the root web.config. Attempting to remove these 
                    // references always throws and there isn't an easy way to identify this. Instead, we'll attempt to lower the level of the message so it doesn't
                    // appear as readily.

                    messageLevel = MessageLevel.Debug;
                }
                Logger.Log(messageLevel, ex.Message);
            }
        }

        private static bool IsUnderAppCode(string path)
        {
            return PathUtility.EnsureTrailingSlash(path).StartsWith(AppCodeFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSourceFile(string path)
        {
            string extension = Path.GetExtension(path);
            return _sourceFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }
    }
}