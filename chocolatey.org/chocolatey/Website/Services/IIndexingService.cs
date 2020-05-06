using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebBackgrounder;

namespace NuGetGallery
{
    public interface IIndexingService
    {
        DateTime? GetLastWriteTime();
        void UpdateIndex();
        void UpdateIndex(bool forceRefresh);
        void UpdatePackage(Package package);
        int GetDocumentCount();
        long GetIndexSizeInBytes();
        void RegisterBackgroundJobs(IList<IJob> jobs);
        string IndexPath { get; }
        bool IsLocal { get; }

    }
}