using System.Windows;

namespace PodBoy
{
    public static class UserSettings
    {
        public static void SaveColumnSettings(GridLength mainColumnWidth, GridLength detailColumnWidth)
        {
            Properties.Settings.Default.EpisodeColumnWidth = mainColumnWidth;
            Properties.Settings.Default.DetailColumnWidth = detailColumnWidth;
            Properties.Settings.Default.Save();
        }
    }
}