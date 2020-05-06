namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PackageValidationResults : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "PackageValidationResultStatus", c => c.String(maxLength: 50));
            AddColumn("Packages", "PackageValidationResultDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("Packages", "PackageValidationResultDate");
            DropColumn("Packages", "PackageValidationResultStatus");
        }
    }
}
