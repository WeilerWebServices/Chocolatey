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

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace NuGetGallery
{
    internal class ScanService : IScanService
    {
        private readonly IPackageService _packageService;
        private readonly IEntityRepository<ScanResult> _scanRepository;

        public ScanService(IEntityRepository<ScanResult> scanRepository, IPackageService packageService)
        {
            _packageService = packageService;
            _scanRepository = scanRepository;
        }

        public void SaveOrUpdateResults(PackageScanResult result, Package package)
        {
            var scanResult = _scanRepository.GetAll()
                .Include(pr => pr.Packages)
                .SingleOrDefault(s => s.Sha256Checksum == result.Sha256Checksum);

            if (scanResult == null)
            {
                scanResult = new ScanResult();
                _scanRepository.InsertOnCommit(scanResult);
            }

            scanResult.Sha256Checksum = result.Sha256Checksum.to_string();
            scanResult.FileName = result.FileName.to_string();
            scanResult.ScanData = result.ScanData.to_string();
            scanResult.ScanDetailsUrl = result.ScanDetailsUrl.to_string();

            int positives = 0;
            int.TryParse(result.Positives.to_string(), out positives);
            scanResult.Positives = positives;

            int totalScans = 0;
            int.TryParse(result.TotalScans.to_string(), out totalScans);
            scanResult.TotalScans = totalScans;

            var scanDate = DateTime.MinValue;
            DateTime.TryParse(result.ScanDate.to_string(), out scanDate);
            if (scanDate != DateTime.MinValue)
            {
                scanResult.ScanDate = scanDate;
            }

            var packageLinked = false;
            foreach (var existingPackage in scanResult.Packages.OrEmptyListIfNull())
            {
                if (package.Key == existingPackage.Key) packageLinked = true;
            }

            if (!packageLinked)
            {
                scanResult.Packages.Add(package);
            }

            _scanRepository.CommitChanges();
        }

        public IEnumerable<ScanResult> GetResults(string id, string version, string sha256Checksum)
        {
            //todo cache
            //todo fix this up later
            if (!string.IsNullOrWhiteSpace(sha256Checksum))
            {
                return _scanRepository.GetAll().Where(s => s.Sha256Checksum == sha256Checksum).ToList();
            }

            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
            {
                var package = _packageService.FindPackageByIdAndVersion(id, version, allowPrerelease: true, useCache: false);
                if (package == null) return new List<ScanResult>();
                var exitingResults = _scanRepository.GetAll()
                    .Include(s => s.Packages);

                return exitingResults.Where(s => s.Packages.Contains(package)).ToList();
            }

            return new List<ScanResult>();
        }
    }
}
