using System.Globalization;

namespace PodBoy
{
    public class ViewInstanceCount
    {
        private static int instanceCount;

        public static string InstanceCount => instanceCount.ToString(CultureInfo.InvariantCulture);

        public static void Increment()
        {
            instanceCount++;
        }
    }
}