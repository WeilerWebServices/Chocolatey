///////////////////////////////////////////////////////////////////////////////
// BUILD PROVIDER
///////////////////////////////////////////////////////////////////////////////

public class AppVeyorTagInfo : ITagInfo
{
    public AppVeyorTagInfo(IAppVeyorProvider appVeyor)
    {
        IsTag = appVeyor.Environment.Repository.Tag.IsTag;
        Name = appVeyor.Environment.Repository.Tag.Name;
    }

    public bool IsTag { get; }

    public string Name { get; }
}

public class AppVeyorRepositoryInfo : IRepositoryInfo
{
    public AppVeyorRepositoryInfo(IAppVeyorProvider appVeyor)
    {
        Branch = appVeyor.Environment.Repository.Branch;
        Name = appVeyor.Environment.Repository.Name;
        Tag = new AppVeyorTagInfo(appVeyor);
    }

    public string Branch { get; }

    public string Name { get; }

    public ITagInfo Tag { get; }
}

public class AppVeyorPullRequestInfo : IPullRequestInfo
{
    public AppVeyorPullRequestInfo(IAppVeyorProvider appVeyor)
    {
        IsPullRequest = appVeyor.Environment.PullRequest.IsPullRequest;
    }

    public bool IsPullRequest { get; }
}

public class AppVeyorBuildInfo : IBuildInfo
{
    public AppVeyorBuildInfo(IAppVeyorProvider appVeyor)
    {
        Number = appVeyor.Environment.Build.Number.ToString();
    }

    public string Number { get; }
}

public class AppVeyorBuildProvider : IBuildProvider
{
    public AppVeyorBuildProvider(IAppVeyorProvider appVeyor)
    {
        Repository = new AppVeyorRepositoryInfo(appVeyor);
        PullRequest = new AppVeyorPullRequestInfo(appVeyor);
        Build = new AppVeyorBuildInfo(appVeyor);

        _appVeyor = appVeyor;
    }

    public IRepositoryInfo Repository { get; }

    public IPullRequestInfo PullRequest { get; }

    public IBuildInfo Build { get; }

    private readonly IAppVeyorProvider _appVeyor;

    public void UploadArtifact(FilePath file)
    {
        _appVeyor.UploadArtifact(file);
    }
}