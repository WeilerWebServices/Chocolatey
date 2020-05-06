namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class DownloadCacheInformation : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "DownloadCacheStatus", c => c.String(maxLength: 50));
            AddColumn("Packages", "DownloadCacheDate", c => c.DateTime());
            AddColumn("Packages", "DownloadCache", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("Packages", "DownloadCache");
            DropColumn("Packages", "DownloadCacheDate");
            DropColumn("Packages", "DownloadCacheStatus");
        }
    }
}
