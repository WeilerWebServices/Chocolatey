namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddScanInformation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "ScanResults",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Sha256Checksum = c.String(maxLength: 400),
                        FileName = c.String(maxLength: 400),
                        ScanDetailsUrl = c.String(maxLength: 400),
                        Positives = c.Int(nullable: false),
                        TotalScans = c.Int(nullable: false),
                        ScanData = c.String(),
                        ScanDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Key);
            
            CreateTable(
                "PackageScanResults",
                c => new
                    {
                        ScanResultKey = c.Int(nullable: false),
                        PackageKey = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.ScanResultKey, t.PackageKey })
                .ForeignKey("ScanResults", t => t.ScanResultKey, cascadeDelete: true)
                .ForeignKey("Packages", t => t.PackageKey, cascadeDelete: true)
                .Index(t => t.ScanResultKey)
                .Index(t => t.PackageKey);
            
            AddColumn("Packages", "PackageScanStatus", c => c.String(maxLength: 50));
            AddColumn("Packages", "PackageScanResultDate", c => c.DateTime());
            AddColumn("GallerySettings", "ScanResultsKey", c => c.String());
        }
        
        public override void Down()
        {
            DropIndex("PackageScanResults", new[] { "PackageKey" });
            DropIndex("PackageScanResults", new[] { "ScanResultKey" });
            DropForeignKey("PackageScanResults", "PackageKey", "Packages");
            DropForeignKey("PackageScanResults", "ScanResultKey", "ScanResults");
            DropColumn("GallerySettings", "ScanResultsKey");
            DropColumn("Packages", "PackageScanResultDate");
            DropColumn("Packages", "PackageScanStatus");
            DropTable("PackageScanResults");
            DropTable("ScanResults");
        }
    }
}
