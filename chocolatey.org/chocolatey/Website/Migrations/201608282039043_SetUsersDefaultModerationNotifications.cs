namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class SetUsersDefaultModerationNotifications : DbMigration
    {
        public override void Up()
        {
            Sql("UPDATE dbo.Users SET EmailAllModerationNotifications = 1");
        }
        
        public override void Down()
        {
        }
    }
}
