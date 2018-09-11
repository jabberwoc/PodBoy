using System.Data.Entity;
using System.Data.Entity.Core.Common;
using EFCache;

namespace PodBoy.Context
{
    public class Configuration : DbConfiguration
    {
        public Configuration()
        {
            var transactionHandler = new CacheTransactionHandler(PodboyRepository.Cache);

            AddInterceptor(transactionHandler);

            Loaded +=
                (sender, args) =>
                    args.ReplaceService<DbProviderServices>((s, _) => new CachingProviderServices(s, transactionHandler));
        }
    }
}