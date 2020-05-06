namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Add_UserItems : DbMigration
    {
        public override void Up()
        {
            AddColumn("Users", "IsBanned", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("Users", "IsBanned");
        }
    }
}
