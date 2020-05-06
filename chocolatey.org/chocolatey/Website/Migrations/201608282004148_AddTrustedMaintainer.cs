namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddTrustedMaintainer : DbMigration
    {
        public override void Up()
        {
            AddColumn("Users", "IsTrusted", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("Users", "IsTrusted");
        }
    }
}
