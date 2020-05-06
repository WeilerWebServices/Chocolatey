namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TestResults : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "PackageTestResultStatus", c => c.String(maxLength: 50));
            AddColumn("Packages", "PackageTestResultUrl", c => c.String(maxLength: 400));
        }
        
        public override void Down()
        {
            DropColumn("Packages", "PackageTestResultUrl");
            DropColumn("Packages", "PackageTestResultStatus");
        }
    }
}
