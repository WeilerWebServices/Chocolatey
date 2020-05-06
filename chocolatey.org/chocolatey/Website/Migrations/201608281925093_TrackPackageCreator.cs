namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackPackageCreator : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("Packages", "ReviewerAssignedId", "Users");
            DropIndex("Packages", new[] { "ReviewerAssignedId" });
            AddColumn("Packages", "CreatedByKey", c => c.Int());
            AddForeignKey("Packages", "CreatedByKey", "Users", "Key");
            CreateIndex("Packages", "CreatedByKey");
            DropColumn("Packages", "ReviewerAssignedId");
        }
        
        public override void Down()
        {
            AddColumn("Packages", "ReviewerAssignedId", c => c.Int());
            DropIndex("Packages", new[] { "CreatedByKey" });
            DropForeignKey("Packages", "CreatedByKey", "Users");
            DropColumn("Packages", "CreatedByKey");
            CreateIndex("Packages", "ReviewerAssignedId");
            AddForeignKey("Packages", "ReviewerAssignedId", "Users", "Key");
        }
    }
}
