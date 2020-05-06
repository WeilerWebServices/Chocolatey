namespace NuGetGallery.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AdjustVarCharMaxColumnsPartTwo : DbMigration
    {
        public override void Up()
        {
            // There are existing indexes that prevents altering these columns. We'll drop and recreate those.
            DropIndex(table: "Packages", name: "IX_Package_Search");
            DropIndex(table: "Packages", name: "IX_Packages_PackageRegistrationKey");
            DropIndex(table: "PackageAuthors", name: "IX_PackageAuthors_PackageKey");
            
            // remove the default value constraint even though we don't know the name
            Sql(@"
DECLARE @ConstraintName VarChar(90)
SET @ConstraintName = (SELECT TOP 1
    default_constraints.name
FROM sys.all_columns
INNER JOIN sys.tables
    ON all_columns.object_id = tables.object_id
INNER JOIN sys.schemas
    ON tables.schema_id = schemas.schema_id
INNER JOIN sys.default_constraints
	ON all_columns.default_object_id = default_constraints.object_id
WHERE 
	schemas.name = 'dbo'
	AND tables.name = 'Users'
	AND all_columns.name = 'PasswordHashAlgorithm'
)

PRINT 'Dropping ' + @ConstraintName
EXEC('ALTER TABLE [dbo].[Users] DROP CONSTRAINT ' + @ConstraintName)");
   

            AlterColumn("Users", "EmailAddress", c => c.String(maxLength: 150));
            AlterColumn("Users", "UnconfirmedEmailAddress", c => c.String(maxLength: 150));
            AlterColumn("Users", "HashedPassword", c => c.String(maxLength: 256));
            AlterColumn("Users", "PasswordHashAlgorithm", c => c.String(maxLength: 20, nullable: false)); // has a defaultValue: "SHA1", so we need to remove that and re-add it separately.
            AlterColumn("Users", "Username", c => c.String(maxLength: 64));
            AlterColumn("Users", "EmailConfirmationToken", c => c.String(maxLength: 256));
            AlterColumn("Users", "PasswordResetToken", c => c.String(maxLength: 256));
            AlterColumn("Roles", "Name", c => c.String(maxLength: 50));
            AlterColumn("Packages", "Description", c => c.String(maxLength: 4000));
            AlterColumn("PackageAuthors", "Name", c => c.String(maxLength: 1000));
            AlterColumn("PackageOwnerRequests", "ConfirmationCode", c => c.String(maxLength: 256));
            AlterColumn("UserSiteProfiles", "Username", c => c.String(maxLength: 64));
            AlterColumn("ScanResults", "ScanData", c => c.String(maxLength: 2500));
            AlterColumn("CourseModules", "Description", c => c.String(maxLength: 200));
            DropColumn("Packages", "ExternalPackageUrl");

            // add back in the defaultValue of SHA1
            Sql(@"ALTER TABLE [dbo].[Users] ADD  DEFAULT ('SHA1') FOR [PasswordHashAlgorithm]");

            // CreateIndex does not support INCLUDE
            Sql(@"CREATE NONCLUSTERED INDEX [IX_PackageAuthors_PackageKey] ON [dbo].[PackageAuthors] 
                (
                    [PackageKey]
                ) 
                INCLUDE (
                    [Key],
                    [Name]
                )");
   
            Sql(@"CREATE NONCLUSTERED INDEX [IX_Packages_PackageRegistrationKey] ON [dbo].[Packages] 
                (
                    [PackageRegistrationKey] ASC
                )
                INCLUDE ( 
                    [Key],
                    [Copyright],
                    [Created],
                    [Description],
                    [DownloadCount],
                    [HashAlgorithm],
                    [Hash],
                    [IconUrl],
                    [IsLatest],
                    [LastUpdated],
                    [LicenseUrl],
                    [Published],
                    [PackageFileSize],
                    [ProjectUrl],
                    [RequiresLicenseAcceptance],
                    [Summary],
                    [Tags],
                    [Title],
                    [Version],
                    [FlattenedAuthors],
                    [FlattenedDependencies],
                    [IsLatestStable],
                    [Listed],
                    [IsPrerelease],
                    [ReleaseNotes]
                )");

            Sql(@"CREATE NONCLUSTERED INDEX IX_Package_Search On [dbo].[Packages] 
                (
                    [IsLatestStable],
                    [IsLatest],
                    [Listed],
                    [IsPrerelease]
                ) 
                INCLUDE (
                    [Key],
                    [PackageRegistrationKey],
                    [Description],
                    [Summary],
                    [Tags]
                )");
        }
        
        public override void Down()
        {
            // There are existing indexes that prevents altering these columns. We'll drop and recreate those.
            DropIndex(table: "Packages", name: "IX_Package_Search");
            DropIndex(table: "Packages", name: "IX_Packages_PackageRegistrationKey");
            DropIndex(table: "PackageAuthors", name: "IX_PackageAuthors_PackageKey");

            // remove the default value constraint even though we don't know the name
            Sql(@"
DECLARE @ConstraintName VarChar(90)
SET @ConstraintName = (SELECT TOP 1
    default_constraints.name
FROM sys.all_columns
INNER JOIN sys.tables
    ON all_columns.object_id = tables.object_id
INNER JOIN sys.schemas
    ON tables.schema_id = schemas.schema_id
INNER JOIN sys.default_constraints
	ON all_columns.default_object_id = default_constraints.object_id
WHERE 
	schemas.name = 'dbo'
	AND tables.name = 'Users'
	AND all_columns.name = 'PasswordHashAlgorithm'
)

PRINT 'Dropping ' + @ConstraintName
EXEC('ALTER TABLE [dbo].[Users] DROP CONSTRAINT ' + @ConstraintName)");

            AddColumn("Packages", "ExternalPackageUrl", c => c.String());
            AlterColumn("CourseModules", "Description", c => c.String());
            AlterColumn("ScanResults", "ScanData", c => c.String());
            AlterColumn("UserSiteProfiles", "Username", c => c.String());
            AlterColumn("PackageOwnerRequests", "ConfirmationCode", c => c.String());
            AlterColumn("PackageAuthors", "Name", c => c.String());
            AlterColumn("Packages", "Description", c => c.String());
            AlterColumn("Roles", "Name", c => c.String());
            AlterColumn("Users", "PasswordResetToken", c => c.String());
            AlterColumn("Users", "EmailConfirmationToken", c => c.String());
            AlterColumn("Users", "Username", c => c.String());
            AlterColumn("Users", "PasswordHashAlgorithm", c => c.String(nullable: false)); // has a defaultValue: "SHA1", so we need to remove that and re-add it separately.
            AlterColumn("Users", "HashedPassword", c => c.String());
            AlterColumn("Users", "UnconfirmedEmailAddress", c => c.String());
            AlterColumn("Users", "EmailAddress", c => c.String());

            // add back in the defaultValue of SHA1
            Sql(@"ALTER TABLE [dbo].[Users] ADD  DEFAULT ('SHA1') FOR [PasswordHashAlgorithm]");

            // CreateIndex does not support INCLUDE
            Sql(@"CREATE NONCLUSTERED INDEX [IX_PackageAuthors_PackageKey] ON [dbo].[PackageAuthors] 
                (
                    [PackageKey]
                ) 
                INCLUDE (
                    [Key],
                    [Name]
                )");

            Sql(@"CREATE NONCLUSTERED INDEX [IX_Packages_PackageRegistrationKey] ON [dbo].[Packages] 
                (
                    [PackageRegistrationKey] ASC
                )
                INCLUDE ( 
                    [Key],
                    [Copyright],
                    [Created],
                    [Description],
                    [DownloadCount],
                    [ExternalPackageUrl],
                    [HashAlgorithm],
                    [Hash],
                    [IconUrl],
                    [IsLatest],
                    [LastUpdated],
                    [LicenseUrl],
                    [Published],
                    [PackageFileSize],
                    [ProjectUrl],
                    [RequiresLicenseAcceptance],
                    [Summary],
                    [Tags],
                    [Title],
                    [Version],
                    [FlattenedAuthors],
                    [FlattenedDependencies],
                    [IsLatestStable],
                    [Listed],
                    [IsPrerelease],
                    [ReleaseNotes]
                )");

            Sql(@"CREATE NONCLUSTERED INDEX IX_Package_Search On [dbo].[Packages] 
                (
                    [IsLatestStable],
                    [IsLatest],
                    [Listed],
                    [IsPrerelease]
                ) 
                INCLUDE (
                    [Key],
                    [PackageRegistrationKey],
                    [Description],
                    [Summary],
                    [Tags]
                )");
        }
    }
}
