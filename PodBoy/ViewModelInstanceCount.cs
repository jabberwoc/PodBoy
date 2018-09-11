using System.Globalization;

namespace PodBoy
{
    public class ViewModelInstanceCount
    {
        private static int instanceCount;

        public static string InstanceCount => instanceCount.ToString(CultureInfo.InvariantCulture);

        public static void Increment()
        {
            instanceCount++;
        }
    }
}