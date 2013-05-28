using System;
using System.ComponentModel.Composition;
using System.Configuration;
using Raven.Abstractions.Data;
using Raven.Abstractions.Indexing;
using Raven.Abstractions.Logging;
using Raven.Json.Linq;

namespace Raven.Database.Plugins.Custom
{
    [InheritedExport(typeof(AbstractBackgroundTask))]
    [ExportMetadata("Bundle", "CollectionLevelExpirationBundle")]
    public class CollectionLevelExpirationBundle : AbstractBackgroundTask
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();
        private string _indexName;
        private string _collection;
        private TimeSpan _ttl;

        public CollectionLevelExpirationBundle()
        {
            Logger.Debug(() => "CollectionLevelExpirationBundle initialization");
        }

        protected override void Initialize()
        {
            SetConfiguration();
            EnsureIndex();
        }

        protected override bool HandleWork()
        {
            // find any document, from the begining of time up to the oldest document age allowed
            var oldestDocumentAgeAllowed = DateTime.UtcNow.Subtract(_ttl);
            var findOldDocuments = string.Format("Timestamp: [NULL TO {0}]", oldestDocumentAgeAllowed.ToString("o"));

            Logger.Debug(() => string.Format("CollectionLevelExpirationBundle.HandleWork criteria {0}", findOldDocuments));

            try
            {
                var databaseQuery = Database.Query(_indexName, new IndexQuery { Query = findOldDocuments });

                foreach (var document in databaseQuery.Results)
                {
                    var id = ((RavenJObject)document["@metadata"]).Value<string>("@id");
                    Database.Delete(id, null, null);
                }

                Logger.Debug(() => string.Format("CollectionLevelExpirationBundle.HandleWork {0} documents were deleted", databaseQuery.Results.Count));
            }
            catch (Exception ex)
            {
                Logger.ErrorException("CollectionLevelExpirationBundle.HandleWork failed while trying to find and remove old documents", ex);
            }

            return true;
        }

        protected override TimeSpan TimeoutForNextWork()
        {
            return TimeSpan.FromHours(1);
        }

        private void SetConfiguration()
        {
            _collection = ConfigurationManager.AppSettings["CollectionLevelExpirationBundle/Collection"];
            _indexName = string.Format("{0}ByLastModificationDate", _collection);

            double hoursToLeave;
            double.TryParse(ConfigurationManager.AppSettings["CollectionLevelExpirationBundle/HoursToLeave"], out hoursToLeave);
            _ttl = TimeSpan.FromHours(hoursToLeave);

            if (_collection == null || hoursToLeave.Equals(0))
            {
                Logger.Error("CollectionLevelExpirationBundle is expect the following configuration keys at the app settings: CollectionLevelExpirationBundle/Collection, CollectionLevelExpirationBundle/HoursToLeave");
            }
        }

        private void EnsureIndex()
        {
            if (Database.GetIndexDefinition(_indexName) == null)
            {
                Database.PutIndex(_indexName, new IndexDefinition
                {
                    Map = string.Format("from doc in docs.{0} select new {{ Timestamp = doc[\"@metadata\"][\"Last-Modified\"]}}", _collection)
                });

                Logger.Debug(() => string.Format("CollectionLevelExpirationBundle created the index {0}", _indexName));
            }
        }
    }
}