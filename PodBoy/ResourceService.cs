using System.Windows;
using System.Windows.Media;

namespace PodBoy
{
    public class ResourceService : IResourceService
    {
        public ImageSource DefaultImage => Application.Current.FindResource("DefaultImage") as DrawingImage;
    }
}