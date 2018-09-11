namespace PodBoy.Entity
{
    public class Media
    {
        public Media() {}

        public Media(string url, string mediaType, long length)
        {
            Url = url;
            MediaType = mediaType;
            Length = length;
        }

        public long Length { get; set; }

        public string MediaType { get; set; }

        public string Url { get; set; }
    }
}