namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PackageCleanupResultDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "PackageCleanupResultDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("Packages", "PackageCleanupResultDate");
        }
    }
}
