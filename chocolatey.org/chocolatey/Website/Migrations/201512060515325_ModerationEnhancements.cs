namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ModerationEnhancements : DbMigration
    {
        public override void Up()
        {
            AddColumn("PackageRegistrations", "ExemptedFromVerification", c => c.Boolean(nullable: false));
            AddColumn("PackageRegistrations", "ExemptedFromVerificationReason", c => c.String(maxLength: 500));
            AddColumn("PackageRegistrations", "ExemptedFromVerificationDate", c => c.DateTime());
            AddColumn("PackageRegistrations", "ExemptedFromVerificationById", c => c.Int());
            AddColumn("Packages", "ReviewerAssignedId", c => c.Int());
            AddForeignKey("PackageRegistrations", "ExemptedFromVerificationById", "Users", "Key");
            AddForeignKey("Packages", "ReviewerAssignedId", "Users", "Key");
            CreateIndex("PackageRegistrations", "ExemptedFromVerificationById");
            CreateIndex("Packages", "ReviewerAssignedId");
        }
        
        public override void Down()
        {
            DropIndex("Packages", new[] { "ReviewerAssignedId" });
            DropIndex("PackageRegistrations", new[] { "ExemptedFromVerificationById" });
            DropForeignKey("Packages", "ReviewerAssignedId", "Users");
            DropForeignKey("PackageRegistrations", "ExemptedFromVerificationById", "Users");
            DropColumn("Packages", "ReviewerAssignedId");
            DropColumn("PackageRegistrations", "ExemptedFromVerificationById");
            DropColumn("PackageRegistrations", "ExemptedFromVerificationDate");
            DropColumn("PackageRegistrations", "ExemptedFromVerificationReason");
            DropColumn("PackageRegistrations", "ExemptedFromVerification");
        }
    }
}
