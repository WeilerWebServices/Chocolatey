namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPackageOperationsUserKey : DbMigration
    {
        public override void Up()
        {
            AddColumn("GallerySettings", "PackageOperationsUserKey", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("GallerySettings", "PackageOperationsUserKey");
        }
    }
}
