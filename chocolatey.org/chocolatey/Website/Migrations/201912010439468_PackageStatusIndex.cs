namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PackageStatusIndex : DbMigration
    {
        public override void Up()
        {
            var statement = @"
CREATE NONCLUSTERED INDEX [IX_PackageStatus] ON [dbo].[Packages]
(
	[Created] ASC,
	[Status] ASC,
	[ReviewedById] ASC,
	[SubmittedStatus] ASC
)
INCLUDE(
    [DownloadCount],
    [IconUrl],
    [Published],
    [Tags],
    [Title],
    [ReviewedDate],
    [ApprovedDate]
)";

            Sql(statement);
        }
        
        public override void Down()
        {
            DropIndex(table: "Packages", name: "IX_PackageStatus");
        }
    }
}
