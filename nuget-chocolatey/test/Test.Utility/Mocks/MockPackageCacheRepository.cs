﻿using System;
using System.IO;

namespace NuGet.Test.Mocks
{
    public class MockPackageCacheRepository : MockPackageRepository, IPackageCacheRepository
    {
        private readonly bool _doDownload;

        public MockPackageCacheRepository(bool doDownload)
        {
            _doDownload = doDownload;
        }

        public bool InvokeOnPackage(string packageId, SemanticVersion version, Action<Stream> action)
        {
            if (_doDownload)
            {
                action(Stream.Null);
                return true;
            }
            return false;
        }
    }
}
