namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AdjustPackageVarcharMaxColumns : DbMigration
    {
        public override void Up()
        {
            // There's an existing index that prevents altering these columns. We'll drop the index and recreate it.
            DropIndex(table: "Packages", name: "IX_Packages_PackageRegistrationKey");
            DropIndex(table: "Packages", name: "IX_Package_Search");
            //IX_Package_Search

            AlterColumn("Packages", "Copyright", c => c.String(maxLength: 1500));
            AlterColumn("Packages", "IconUrl", c => c.String(maxLength: 1500));
            AlterColumn("Packages", "LicenseUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "ProjectUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "ProjectSourceUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "PackageSourceUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "DocsUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "MailingListUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "BugTrackerUrl", c => c.String(maxLength: 500));
            AlterColumn("Packages", "Summary", c => c.String(maxLength: 1500));
            AlterColumn("Packages", "Tags", c => c.String(maxLength: 1000));
            AlterColumn("Packages", "FlattenedAuthors", c => c.String(maxLength: 1500));
            AlterColumn("Packages", "FlattenedDependencies", c => c.String(maxLength: 1500));

            // CreateIndex does not support INCLUDE
            Sql(@"CREATE NONCLUSTERED INDEX [IX_Packages_PackageRegistrationKey] ON [dbo].[Packages] 
                (
                    [PackageRegistrationKey] ASC
                )
                INCLUDE ( 
                    [Key],
                    [Copyright],
                    [Created],
                    [Description],
                    [DownloadCount],
                    [ExternalPackageUrl],
                    [HashAlgorithm],
                    [Hash],
                    [IconUrl],
                    [IsLatest],
                    [LastUpdated],
                    [LicenseUrl],
                    [Published],
                    [PackageFileSize],
                    [ProjectUrl],
                    [RequiresLicenseAcceptance],
                    [Summary],
                    [Tags],
                    [Title],
                    [Version],
                    [FlattenedAuthors],
                    [FlattenedDependencies],
                    [IsLatestStable],
                    [Listed],
                    [IsPrerelease],
                    [ReleaseNotes]
                )");

            Sql(@"CREATE NONCLUSTERED INDEX IX_Package_Search On [dbo].[Packages] 
                (
                    [IsLatestStable],
                    [IsLatest],
                    [Listed],
                    [IsPrerelease]
                ) 
                INCLUDE (
                    [Key],
                    [PackageRegistrationKey],
                    [Description],
                    [Summary],
                    [Tags]
                )");
        }
        
        public override void Down()
        {
            AlterColumn("Packages", "FlattenedDependencies", c => c.String());
            AlterColumn("Packages", "FlattenedAuthors", c => c.String());
            AlterColumn("Packages", "Tags", c => c.String());
            AlterColumn("Packages", "Summary", c => c.String());
            AlterColumn("Packages", "BugTrackerUrl", c => c.String(maxLength: 400));
            AlterColumn("Packages", "MailingListUrl", c => c.String(maxLength: 400));
            AlterColumn("Packages", "DocsUrl", c => c.String(maxLength: 400));
            AlterColumn("Packages", "PackageSourceUrl", c => c.String(maxLength: 400));
            AlterColumn("Packages", "ProjectSourceUrl", c => c.String(maxLength: 400));
            AlterColumn("Packages", "ProjectUrl", c => c.String());
            AlterColumn("Packages", "LicenseUrl", c => c.String());
            AlterColumn("Packages", "IconUrl", c => c.String());
            AlterColumn("Packages", "Copyright", c => c.String());
        }
    }
}
