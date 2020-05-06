// Copyright 2011 - Present RealDimensions Software, LLC, the original 
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery 
// at https://github.com/NuGet/NuGetGallery
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Data.Entity.Migrations;

namespace NuGetGallery.Migrations
{
    public partial class AddPackageFiles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "PackageFiles", c => new
                {
                    Key = c.Int(nullable: false, identity: true),
                    PackageKey = c.Int(nullable: false),
                    FilePath = c.String(nullable: false, maxLength: 500),
                    FileContent = c.String(nullable: false),
                }).PrimaryKey(t => t.Key).ForeignKey("Packages", t => t.PackageKey, cascadeDelete: true).Index(t => t.PackageKey);
        }

        public override void Down()
        {
            DropIndex("PackageFiles", new[] { "PackageKey" });
            DropForeignKey("PackageFiles", "PackageKey", "Packages");
            DropTable("PackageFiles");
        }
    }
}
