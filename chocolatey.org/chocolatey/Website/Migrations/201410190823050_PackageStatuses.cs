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
    public partial class PackageStatuses : DbMigration
    {
        public override void Up()
        {
            AddColumn("Packages", "Status", c => c.String(maxLength: 100));
            AddColumn("Packages", "ReviewedDate", c => c.DateTime());
            AddColumn("Packages", "ApprovedDate", c => c.DateTime());
            AddColumn("Packages", "ReviewedById", c => c.Int());
            AddForeignKey("Packages", "ReviewedById", "Users", "Key");
            CreateIndex("Packages", "ReviewedById");
        }

        public override void Down()
        {
            DropIndex("Packages", new[] { "ReviewedById" });
            DropForeignKey("Packages", "ReviewedById", "Users");
            DropColumn("Packages", "ReviewedById");
            DropColumn("Packages", "ApprovedDate");
            DropColumn("Packages", "ReviewedDate");
            DropColumn("Packages", "Status");
        }
    }
}
