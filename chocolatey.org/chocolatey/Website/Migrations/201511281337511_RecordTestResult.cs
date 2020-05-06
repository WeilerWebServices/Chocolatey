namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RecordTestResult : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "PackageTestResultDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("Packages", "PackageTestResultDate");
        }
    }
}
