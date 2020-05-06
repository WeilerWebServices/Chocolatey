///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// FILE INCLUDES
///////////////////////////////////////////////////////////////////////////////

#load ./.build/buildData.cake
#load ./.build/appveyor.cake
#load ./.build/buildProvider.cake
#load ./.build/localbuild.cake
#load ./.build/teamcity.cake

///////////////////////////////////////////////////////////////////////////////
// MODULES
///////////////////////////////////////////////////////////////////////////////
#module nuget:?package=Cake.BuildSystems.Module&version=0.3.2

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////
#tool nuget:?package=GitVersion.CommandLine&version=5.0.1

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildData>(context =>
{
    Information("Running tasks...");

    return new BuildData(context);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Get-Version-Information")
    .Does<BuildData>(buildData =>
{
    string majorMinorPatch = null;
    string version = null;
    string fileVersion = null;
    string semVersion = null;
    string milestone = null;
    string informationalVersion = null;
    string fullSemVersion = null;
    string packageVersion = null;
    string prerelease = null;
    string sha = null;
    GitVersion assertedVersions = null;

    if (Context.Environment.Platform.Family != PlatformFamily.Windows)
    {
        PatchGitLibConfigFiles(Context);
    }

    Information("Calculating Semantic Version...");

    if (!BuildSystem.IsLocalBuild)
    {
        Information("Running GitVersion with BuildServer flag...");
        GitVersion(new GitVersionSettings{
            OutputType = GitVersionOutput.BuildServer,
            NoFetch = true
        });

        assertedVersions = GitVersion(new GitVersionSettings{
            OutputType = GitVersionOutput.Json,
            NoFetch = true
        });
    }
    else
    {
        Information("Running GitVersion directly...");
        assertedVersions = GitVersion(new GitVersionSettings{
            OutputType = GitVersionOutput.Json
        });
    }

    majorMinorPatch = assertedVersions.MajorMinorPatch;
    fileVersion = assertedVersions.AssemblySemVer;
    semVersion = assertedVersions.LegacySemVerPadded;
    informationalVersion = assertedVersions.InformationalVersion;
    milestone = string.Concat(version);
    fullSemVersion = assertedVersions.FullSemVer;
    prerelease = assertedVersions.PreReleaseLabel;
    sha = assertedVersions.Sha.Substring(0,8);

    var preReleaseLabel = string.Empty;

    if (buildData.PreReleaseLabelFilePath != null && FileExists(buildData.PreReleaseLabelFilePath))
    {
        preReleaseLabel = System.IO.File.ReadAllText(buildData.PreReleaseLabelFilePath.FullPath);
    }

    var buildDate = DateTime.Now.ToString("yyyyMMdd");

    if (!buildData.IsTagged)
    {
        packageVersion = string.Format("{0}{1}{2}{3}", majorMinorPatch, string.IsNullOrWhiteSpace(preReleaseLabel) ? string.Format("-{0}", prerelease) : string.Format("-{0}", preReleaseLabel), string.Format("-{0}", buildDate), buildData.BuildCounter != "-1" ? string.Format("-{0}", buildData.BuildCounter) : string.Empty);
        informationalVersion = string.Format("{0}{1}{2}{3}", majorMinorPatch, string.IsNullOrWhiteSpace(preReleaseLabel) ? string.Format("-{0}", prerelease) : string.Format("-{0}", preReleaseLabel), string.Format("-{0}", buildDate), string.Format("-{0}", sha));
        Information("There is no tag.");
    }
    else
    {
        packageVersion = semVersion;
        informationalVersion = semVersion;
        Information("There is a tag.");
    }

    Information("Calculated File Version: {0}", fileVersion);
    Information("Calculated Package Version: {0}", packageVersion);
    Information("Calculated Informational Version: {0}", informationalVersion);

    var cakeVersion = typeof(ICakeContext).Assembly.GetName().Version.ToString();

    buildData.Version = majorMinorPatch;
    buildData.SemVersion = semVersion;
    buildData.Milestone = milestone;
    buildData.CakeVersion = cakeVersion;
    buildData.InformationalVersion = informationalVersion;
    buildData.FullSemVersion = fullSemVersion;
    buildData.PackageVersion = packageVersion;
});


Task("Clean")
    .IsDependentOn("Get-Version-Information")
    .Does<BuildData>(buildData =>
{
    MSBuild(buildData.SolutionFilePath, new MSBuildSettings {
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL,
        Targets = { "Clean" }
    });

    CleanDirectory(buildData.BuildArtifactsDirectoryPath);
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does<BuildData>(buildData =>
{
    NuGetRestore(buildData.SolutionFilePath);
});

Task("Build")
    .IsDependentOn("Restore")
    .Does<BuildData>(buildData =>
{
    MSBuild(buildData.SolutionFilePath, new MSBuildSettings {
        Configuration = configuration,
        PlatformTarget = PlatformTarget.MSIL,
        Targets = { "Build" }
    }
    .WithProperty("CodeAnalysis", "true")
    .WithProperty("OutDir", buildData.BuildArtifactsDirectoryPath.FullPath)
    );
});

Task("CleanBuildOutput")
    .IsDependentOn("Build")
    .Does<BuildData>(buildData =>
{
    var filesToRemove = GetFiles(buildData.BuildArtifactsDirectoryPath + "/*") - GetFiles(buildData.BuildArtifactsDirectoryPath + "/_PublishedWebsites/*");

    DeleteFiles(filesToRemove);
});

Task("CreateChocolateyPackages")
    .IsDependentOn("CleanBuildOutput")
    .Does<BuildData>(buildData =>
{
    var nuspecFiles = GetFiles(buildData.ChocolateyNuspecDirectory + "/**/*.nuspec");

    EnsureDirectoryExists(buildData.ChocolateyPackagesDirectory);

    foreach (var nuspecFile in nuspecFiles)
    {
        // Create package.
        ChocolateyPack(nuspecFile, new ChocolateyPackSettings {
            AllowUnofficial = true,
            Version = buildData.PackageVersion,
            OutputDirectory = buildData.ChocolateyPackagesDirectory
        });
    }
});

Task("PublishPackages")
    .IsDependentOn("CreateChocolateyPackages")
    .Does<BuildData>(buildData =>
{
    if (Context.Environment.Platform.Family == PlatformFamily.Windows && DirectoryExists(buildData.ChocolateyPackagesDirectory))
    {
        var nupkgFiles = GetFiles(buildData.ChocolateyPackagesDirectory + "/**/*.nupkg");

        if (string.IsNullOrEmpty(buildData.ChocolateyPushUrl))
        {
          Warning("Unable to push Chocolatey Packages, as no PushUrl has been provided.");
          return;
        }

        var chocolateyPushSettings = new ChocolateyPushSettings
            {
                AllowUnofficial = true,
                Source = buildData.ChocolateyPushUrl
            };

        var canPushToChocolateySource = false;
        if (!string.IsNullOrEmpty(buildData.ChocolateyCredentials.ApiKey))
        {
            Information("Setting ApiKey in Chocolatey Push Settings...");
            chocolateyPushSettings.ApiKey = buildData.ChocolateyCredentials.ApiKey;
            canPushToChocolateySource = true;
        }
        else
        {
            if (!string.IsNullOrEmpty(buildData.ChocolateyCredentials.User) && !string.IsNullOrEmpty(buildData.ChocolateyCredentials.Password))
            {
                var chocolateySourceSettings = new ChocolateySourcesSettings
                {
                    AllowUnofficial = true,
                    UserName = buildData.ChocolateyCredentials.User,
                    Password = buildData.ChocolateyCredentials.Password
                };

                Information("Adding Chocolatey source with user/pass...");

                var sourceName = buildData.ChocolateySourceName;
                if (string.IsNullOrEmpty(sourceName))
                {
                  sourceName = "TempChocolateySource";
                }

                ChocolateyAddSource(sourceName, buildData.ChocolateyPushUrl, chocolateySourceSettings);
                canPushToChocolateySource = true;
            }
            else
            {
                Warning("User and Password are missing for Chocolatey Source with Url {0}", buildData.ChocolateyPushUrl);
            }
        }

        if (canPushToChocolateySource)
        {
            foreach (var nupkgFile in nupkgFiles)
            {
                Information("Pushing {0} to Source with Url {1}...", nupkgFile, buildData.ChocolateyPushUrl);

                // Push the package.
                ChocolateyPush(nupkgFile, chocolateyPushSettings);
            }
        }
        else
        {
            Warning("Unable to push Chocolatey Packages to Source with Url {0} as necessary credentials haven't been provided.", buildData.ChocolateyPushUrl);
        }
    }
    else
    {
        Information("Unable to publish Chocolatey packages. IsRunningOnWindows: {0} Chocolatey Packages Directory Exists: {0}", Context.Environment.Platform.Family, DirectoryExists(buildData.ChocolateyPackagesDirectory));
    }
});

private static void PatchGitLibConfigFiles(ICakeContext context)
{
    var configFiles = context.GetFiles("./tools/**/LibGit2Sharp.dll.config");
    var libgitPath = GetLibGit2Path(context);
    if (string.IsNullOrEmpty(libgitPath)) { return; }

    foreach (var config in configFiles) {
        var xml = System.Xml.Linq.XDocument.Load(config.ToString());

        if (xml.Element("configuration").Elements("dllmap")
            .All(e => e.Attribute("target").Value != libgitPath)) {

            var dllName = xml.Element("configuration").Elements("dllmap").First(e => e.Attribute("os").Value == "linux").Attribute("dll").Value;
            xml.Element("configuration")
                .Add(new System.Xml.Linq.XElement("dllmap",
                    new System.Xml.Linq.XAttribute("os", "linux"),
                    new System.Xml.Linq.XAttribute("dll", dllName),
                    new System.Xml.Linq.XAttribute("target", libgitPath)));

            context.Information($"Patching '{config}' to use fallback system path on Linux...");
            xml.Save(config.ToString());
        }
    }
}

private static string GetLibGit2Path(ICakeContext context)
{
    var possiblePaths = new[] {
        "/usr/lib*/libgit2.so*",
        "/usr/lib/*/libgit2.so*"
    };

    foreach (var path in possiblePaths) {
        var file = context.GetFiles(path).FirstOrDefault();
        if (file != null && !string.IsNullOrEmpty(file.ToString())) {
            return file.ToString();
        }
    }

    return null;
}


Task("Default")
    .IsDependentOn("CreateChocolateyPackages");

RunTarget(target);