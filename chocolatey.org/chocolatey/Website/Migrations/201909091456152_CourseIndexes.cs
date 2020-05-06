namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CourseIndexes : DbMigration
    {
        public override void Up()
        {
            // add the constraints 
            CreateIndex(table: "Courses", columns: new[] { "CourseNameType" }, unique: true, name: "UX_CourseNameType");
            CreateIndex(table: "CourseModules", columns: new[] { "CourseModuleNameType" }, unique: true, name: "UX_CourseModuleNameType");
            CreateIndex(table: "CourseModules", columns: new[] { "CourseKey", "Order" }, unique: true, name: "UX_Course_Order");
        }
        
        public override void Down()
        {
            DropIndex(table: "Courses", name: "UX_CourseNameType");
            DropIndex(table: "CourseModules", name: "UX_CourseModuleNameType");
            DropIndex(table: "CourseModules", name: "UX_Course_Order");
        }
    }
}
