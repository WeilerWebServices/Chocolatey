namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Courses : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "Courses",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255),
                        CourseNameType = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Key);
            
            CreateTable(
                "CourseModules",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        CourseKey = c.Int(nullable: false),
                        Name = c.String(maxLength: 255),
                        Description = c.String(),
                        ModuleLength = c.String(maxLength: 10),
                        ModuleQuestionCount = c.Int(nullable: false),
                        Order = c.Int(nullable: false),
                        CourseModuleNameType = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("Courses", t => t.CourseKey, cascadeDelete: true)
                .Index(t => t.CourseKey);
            
            CreateTable(
                "UserCourseAchievements",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        UserKey = c.Int(nullable: false),
                        CourseKey = c.Int(nullable: false),
                        Completed = c.Boolean(nullable: false),
                        CompletedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("Users", t => t.UserKey)
                .Index(t => t.UserKey);
            
            CreateTable(
                "UserCourseModuleAchievements",
                c => new
                    {
                        Key = c.Int(nullable: false, identity: true),
                        UserCourseAchievementKey = c.Int(nullable: false),
                        CourseModuleKey = c.Int(nullable: false),
                        CompletedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Key)
                .ForeignKey("UserCourseAchievements", t => t.UserCourseAchievementKey, cascadeDelete: true)
                .Index(t => t.UserCourseAchievementKey);
            
        }
        
        public override void Down()
        {
            DropIndex("UserCourseModuleAchievements", new[] { "UserCourseAchievementKey" });
            DropIndex("UserCourseAchievements", new[] { "UserKey" });
            DropIndex("CourseModules", new[] { "CourseKey" });
            DropForeignKey("UserCourseModuleAchievements", "UserCourseAchievementKey", "UserCourseAchievements");
            DropForeignKey("UserCourseAchievements", "UserKey", "Users");
            DropForeignKey("CourseModules", "CourseKey", "Courses");
            DropTable("UserCourseModuleAchievements");
            DropTable("UserCourseAchievements");
            DropTable("CourseModules");
            DropTable("Courses");
        }
    }
}
