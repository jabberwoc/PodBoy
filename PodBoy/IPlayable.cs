namespace PodBoy
{
    public interface IPlayable
    {
        bool IsActive { get; set; }
        bool IsPlaying { get; set; }

        bool IsPlayed { get; set; }
    }
}