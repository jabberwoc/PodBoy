using System;

namespace PodBoy.Context
{
    [Flags]
    public enum EntityType
    {
        None = 0,
        Channel = 1 << 1,
        Episode = 1 << 2,
        Playlist = 1 << 3,
        PlaylistItem = 1 << 4
    }
}