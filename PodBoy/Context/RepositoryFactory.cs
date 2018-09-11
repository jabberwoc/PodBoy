using System;

namespace PodBoy.Context
{
    public class RepositoryFactory : IRepositoryFactory
    {
        static RepositoryFactory()
        {
            Provider = () => new PodboyRepository();
        }

        public static IPodboyRepository Create()
        {
            return Provider.Invoke();
        }

        public static void SetContextProvider(Func<IPodboyRepository> provider)
        {
            Provider = provider;
        }

        public static Func<IPodboyRepository> Provider { get; private set; }

        IPodboyRepository IRepositoryFactory.Create()
        {
            return Create();
        }
    }
}