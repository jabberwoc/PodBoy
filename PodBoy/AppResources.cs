using Splat;

namespace PodBoy
{
    public class AppResources
    {
        private AppResources() {}

        private static IResourceService instance;

        public static IResourceService Instance
            => instance ?? (instance = Locator.Current.GetService<IResourceService>());
    }
}