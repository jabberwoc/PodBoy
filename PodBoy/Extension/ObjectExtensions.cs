namespace PodBoy.Extension
{
    public static class ObjectExtensions
    {
        public static T[] InArray<T>(this T obj)
        {
            return new[]
            {
                obj
            };
        }
    }
}