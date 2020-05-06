using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Elmah;
using WebBackgrounder;

namespace NuGetGallery
{
    public class LuceneIndexingJob : Job
    {
        private readonly LuceneIndexingService _indexingService;

        public LuceneIndexingJob(TimeSpan frequence, TimeSpan timeout, LuceneIndexingService indexingService)
            : base("Lucene", frequence, timeout)
        {
            _indexingService = indexingService;

            try
            {
                _indexingService.UpdateIndex();
            }
            catch (SqlException e)
            {
                // Log but swallow the exception
                ErrorSignal.FromCurrentContext().Raise(e);
            }
            catch (DataException e)
            {
                // Log but swallow the exception
                ErrorSignal.FromCurrentContext().Raise(e);
            }
        }
        
        public override Task Execute()
        {
            return new Task(_indexingService.UpdateIndex);
        }
    }
}