using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PerfView
{
    /// <summary>
    /// WPF compatibility shim for ImageSource. In Avalonia, we use this as a
    /// base class that Bitmap-backed resources can be cast to. The Icon property
    /// returns object so AXAML Image.Source binding works directly with Bitmap.
    /// </summary>
    public class ImageSource
    {
    }
}
