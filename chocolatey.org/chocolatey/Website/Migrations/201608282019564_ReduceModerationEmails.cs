namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ReduceModerationEmails : DbMigration
    {
        public override void Up()
        {
            AddColumn("Users", "EmailAllModerationNotifications", c => c.Boolean(nullable: false, defaultValue:true));
        }
        
        public override void Down()
        {
            DropColumn("Users", "EmailAllModerationNotifications");
        }
    }
}
