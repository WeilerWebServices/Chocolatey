using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using NuGet.VisualStudio.Resources;

namespace NuGet.VisualStudio
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IVsPackageSourceProvider))]
    [Export(typeof(IPackageSourceProvider))]
    public class VsPackageSourceProvider : IVsPackageSourceProvider
    {
        private static readonly string NuGetLegacyOfficialFeedName = VsResources.NuGetLegacyOfficialSourceName;
        private static readonly string NuGetOfficialFeedName = VsResources.NuGetOfficialSourceName;
        private static readonly PackageSource NuGetDefaultSource = new PackageSource(NuGetConstants.DefaultFeedUrl, NuGetOfficialFeedName);
        
        private static readonly PackageSource Windows8Source = new PackageSource(
            NuGetConstants.VSExpressForWindows8FeedUrl,
            VsResources.VisualStudioExpressForWindows8SourceName,
            isEnabled: true,
            isOfficial: true);

        private static readonly Dictionary<PackageSource, PackageSource> _feedsToMigrate = new Dictionary<PackageSource, PackageSource>
        {
            { new PackageSource(NuGetConstants.V1FeedUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
            { new PackageSource(NuGetConstants.V2LegacyFeedUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
            { new PackageSource(NuGetConstants.V2LegacyOfficialPackageSourceUrl, NuGetLegacyOfficialFeedName), NuGetDefaultSource },
        };

        internal const string ActivePackageSourceSectionName = "activePackageSource";

        private readonly IPackageSourceProvider _packageSourceProvider;
        private readonly IVsShellInfo _vsShellInfo;
        private readonly ISettings _settings;
        private readonly ISolutionManager _solutionManager;
        private bool _initialized;
        private List<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        [ImportingConstructor]
        public VsPackageSourceProvider(
            ISettings settings,            
            IVsShellInfo vsShellInfo,
            ISolutionManager solutionManager) :
            this(settings, new PackageSourceProvider(settings, new[] { NuGetDefaultSource}, _feedsToMigrate), vsShellInfo, solutionManager)
        {
        }

        internal VsPackageSourceProvider(
            ISettings settings,
            IPackageSourceProvider packageSourceProvider,
            IVsShellInfo vsShellInfo)
            :this(settings, packageSourceProvider, vsShellInfo, null)
        {
        }

        private VsPackageSourceProvider(
            ISettings settings,            
            IPackageSourceProvider packageSourceProvider,
            IVsShellInfo vsShellInfo,
            ISolutionManager solutionManager)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (packageSourceProvider == null)
            {
                throw new ArgumentNullException("packageSourceProvider");
            }

            if (vsShellInfo == null)
            {
                throw new ArgumentNullException("vsShellInfo");
            }

            _packageSourceProvider = packageSourceProvider;
            _solutionManager = solutionManager;
            _settings = settings;
            _vsShellInfo = vsShellInfo;

            if (null != _solutionManager)
            {
                _solutionManager.SolutionClosed += OnSolutionOpenedOrClosed;
                _solutionManager.SolutionOpened += OnSolutionOpenedOrClosed;
            }
        }

        private void OnSolutionOpenedOrClosed(object sender, EventArgs e)
        {
            _initialized = false;
        }

        public PackageSource ActivePackageSource
        {
            get
            {
                EnsureInitialized();

                return _activePackageSource;
            }
            set
            {
                EnsureInitialized();

                if (value != null &&
                    !IsAggregateSource(value) &&
                    !_packageSources.Contains(value) &&
                    !value.Name.Equals(NuGetOfficialFeedName, StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new ArgumentException(VsResources.PackageSource_Invalid, "value");
                }

                _activePackageSource = value;

                PersistActivePackageSource(_settings, _activePackageSource);
            }
        }

        internal static IEnumerable<PackageSource> DefaultSources
        {
            get { return new[] { NuGetDefaultSource}; }
        }

        internal static Dictionary<PackageSource, PackageSource> FeedsToMigrate
        {
            get { return _feedsToMigrate; }
        }

        public IEnumerable<PackageSource> LoadPackageSources()
        {
            EnsureInitialized();
            // assert that we are not returning aggregate source
            Debug.Assert(_packageSources == null || !_packageSources.Any(IsAggregateSource));
            return _packageSources;
        }

        public void SavePackageSources(IEnumerable<PackageSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException("sources");
            }

            EnsureInitialized();
            Debug.Assert(!sources.Any(IsAggregateSource));

            ActivePackageSource = null;
            _packageSources.Clear();
            _packageSources.AddRange(sources);

            PersistPackageSources(_packageSourceProvider, _vsShellInfo, _packageSources);
        }

        public void DisablePackageSource(PackageSource source)
        {
            // There's no scenario for this method to get called, so do nothing here.
            Debug.Fail("This method shouldn't get called.");
        }

        public bool IsPackageSourceEnabled(PackageSource source)
        {
            EnsureInitialized();

            var sourceInUse = _packageSources.FirstOrDefault(ps => ps.Equals(source));
            return sourceInUse != null && sourceInUse.IsEnabled;
        }

        private void EnsureInitialized()
        {
            while (!_initialized)
            {
                _initialized = true;
                _packageSources = _packageSourceProvider.LoadPackageSources().ToList();

                // When running Visual Studio Express for Windows 8, we insert the curated feed at the top
                if (_vsShellInfo.IsVisualStudioExpressForWindows8)
                {
                    bool windows8SourceIsEnabled = _packageSourceProvider.IsPackageSourceEnabled(Windows8Source);

                    // defensive coding: make sure we don't add duplicated win8 source
                    _packageSources.RemoveAll(p => p.Equals(Windows8Source));

                    // Windows8Source is a static object which is meant for doing comparison only. 
                    // To add it to the list of package sources, we make a clone of it first.
                    var windows8SourceClone = Windows8Source.Clone();
                    windows8SourceClone.IsEnabled = windows8SourceIsEnabled;
                    _packageSources.Insert(0, windows8SourceClone);
                }

                InitializeActivePackageSource();
            }
        }

        private void InitializeActivePackageSource()
        {
            _activePackageSource = DeserializeActivePackageSource(_settings, _vsShellInfo);

            PackageSource migratedActiveSource;
            bool activeSourceChanged = false;
            if (_activePackageSource == null)
            {
                // If there are no sources, pick the first source that's enabled.
                activeSourceChanged = true;
                _activePackageSource = _packageSources.FirstOrDefault(p => p.IsEnabled) ?? AggregatePackageSource.Instance;
            }
            else if (_feedsToMigrate.TryGetValue(_activePackageSource, out migratedActiveSource))
            {
                // Check if we need to migrate the active source.
                activeSourceChanged = true;
                _activePackageSource = migratedActiveSource;
            }

            if (activeSourceChanged)
            {
                PersistActivePackageSource(_settings, _activePackageSource);
            }
        }

        private static void PersistActivePackageSource(ISettings settings, PackageSource activePackageSource)
        {
            settings.DeleteSection(ActivePackageSourceSectionName);

            if (activePackageSource != null)
            {
                settings.SetValue(ActivePackageSourceSectionName, activePackageSource.Name, activePackageSource.Source);
            }
        }

        private static PackageSource DeserializeActivePackageSource(
            ISettings settings,
            IVsShellInfo vsShellInfo)
        {
            var settingValues = settings.GetValues(ActivePackageSourceSectionName, isPath: false);

            PackageSource packageSource = null;
            if (settingValues != null && settingValues.Any())
            {
                var setting = settingValues.First();
                if (IsAggregateSource(setting.Key, setting.Value))
                {
                    packageSource = AggregatePackageSource.Instance;
                }
                else
                {
                    packageSource = new PackageSource(setting.Value, setting.Key);
                }
            }

            if (packageSource != null)
            {
                // guard against corrupted data if the active package source is not enabled
                packageSource.IsEnabled = true;
            }

            return packageSource;
        }

        private static void PersistPackageSources(IPackageSourceProvider packageSourceProvider, IVsShellInfo vsShellInfo, List<PackageSource> packageSources)
        {
            bool windows8SourceIsDisabled = false;

            // When running Visual Studio Express For Windows 8, we will have previously added a curated package source.
            // But we don't want to persist it, so remove it from the list.
            if (vsShellInfo.IsVisualStudioExpressForWindows8)
            {
                PackageSource windows8SourceInUse = packageSources.Find(p => p.Equals(Windows8Source));
                Debug.Assert(windows8SourceInUse != null);
                if (windows8SourceInUse != null)
                {
                    packageSources = packageSources.Where(ps => !ps.Equals(Windows8Source)).ToList();
                    windows8SourceIsDisabled = !windows8SourceInUse.IsEnabled;
                }
            }

            // Starting from version 1.3, we persist the package sources to the nuget.config file instead of VS registry.
            // assert that we are not saving aggregate source
            Debug.Assert(!packageSources.Any(p => IsAggregateSource(p.Name, p.Source)));
            
            packageSourceProvider.SavePackageSources(packageSources);

            if (windows8SourceIsDisabled)
            {
                packageSourceProvider.DisablePackageSource(Windows8Source);
            }
        }

        private static bool IsAggregateSource(string name, string source)
        {
            PackageSource aggregate = AggregatePackageSource.Instance;
            return aggregate.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) ||
                aggregate.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsAggregateSource(PackageSource packageSource)
        {
            return IsAggregateSource(packageSource.Name, packageSource.Source);
        }
    }
}