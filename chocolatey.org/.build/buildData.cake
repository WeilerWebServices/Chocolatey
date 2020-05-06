public class BuildData
{
  public FilePath SolutionFilePath { get; }
  public DirectoryPath BuildArtifactsDirectoryPath { get; }
  public DirectoryPath ChocolateyNuspecDirectory { get; }
  public DirectoryPath ChocolateyPackagesDirectory { get; }
  public string ChocolateyPushUrl { get; }
  public string ChocolateySourceName { get; }
  public ChocolateyCredentials ChocolateyCredentials { get; }
  public string Version { get; set;}
  public string SemVersion { get; set; }
  public string Milestone { get; set; }
  public string CakeVersion { get; set; }
  public string InformationalVersion { get; set; }
  public string FullSemVersion { get; set; }
  public string PackageVersion { get; set; }
  public IBuildProvider BuildProvider { get; }
  public bool IsTagged { get; }
  public string BuildCounter { get; }
  public FilePath PreReleaseLabelFilePath { get; }

	public BuildData(ICakeContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException(nameof(context));
		}

    SolutionFilePath = context.MakeAbsolute((FilePath)"ChocolateyGallery.sln");
    BuildArtifactsDirectoryPath = context.MakeAbsolute((DirectoryPath)"BuildArtifacts");
    ChocolateyNuspecDirectory = context.MakeAbsolute((DirectoryPath)"nuspec/chocolatey");
    ChocolateyPackagesDirectory = context.MakeAbsolute((DirectoryPath)"BuildArtifacts/Packages/Chocolatey");
    ChocolateyPushUrl = context.EnvironmentVariable("CCR_CHOCOLATEY_PUSH_URL");
    ChocolateySourceName = context.EnvironmentVariable("CCR_CHOCOLATEY_SOURCE_NAME");
    ChocolateyCredentials = new ChocolateyCredentials(context);
    BuildProvider = GetBuildProvider(context, context.BuildSystem());
    IsTagged = BuildProvider.Repository.Tag.IsTag;
    BuildCounter = context.Argument("buildCounter", BuildProvider.Build.Number);
    PreReleaseLabelFilePath = context.MakeAbsolute((FilePath)".build_pre_release_label");
	}
}

public class ChocolateyCredentials
{
  public string ApiKey { get; }
  public string User { get; }
  public string Password { get; }

  public ChocolateyCredentials(ICakeContext context)
  {
    ApiKey = context.EnvironmentVariable("CCR_CHOCOLATEY_API_KEY");
    User = context.EnvironmentVariable("CCR_CHOCOLATEY_USER");
    Password = context.EnvironmentVariable("CCR_CHOCOLATEY_PASSWORD");
  }
}