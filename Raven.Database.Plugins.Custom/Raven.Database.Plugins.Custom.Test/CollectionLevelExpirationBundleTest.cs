using System;
using System.ComponentModel.Composition.Hosting;
using Raven.Abstractions.Data;
using Raven.Client.Embedded;
using Raven.Database.Config;

namespace Raven.Database.Plugins.Custom.Test
{
    class CollectionLevelExpirationBundleTest
    {
        static void Main()
        {
            using (var store = new EmbeddableDocumentStore())
            {
                store.DataDirectory = "Data" + DateTime.Now.Ticks;
                store.UseEmbeddedHttpServer = true;
                ModifyConfiguration(store.Configuration);

                store.Initialize();

                Animal animal = new Animal { Name = "Rhino" };

                using (var session = store.OpenSession())
                {
                    session.Store(animal);
                    session.SaveChanges();
                }

                Console.WriteLine("Single \"Animal\" document created.");
                Console.WriteLine("Browse the studio and see it dropped once it gets old");
                Console.WriteLine(store.HttpServer.Configuration.ServerUrl);
                Console.WriteLine("Hit Enter to quit");
                Console.Read();
            }
        }

        private static void ModifyConfiguration(InMemoryRavenConfiguration configuration)
        {
            configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
            configuration.Catalog.Catalogs.Add(new AssemblyCatalog(typeof(CollectionLevelExpirationBundle).Assembly));
            configuration.Settings[Constants.ActiveBundles] = "CollectionLevelExpirationBundle";
        }
    }

    public class Animal
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
