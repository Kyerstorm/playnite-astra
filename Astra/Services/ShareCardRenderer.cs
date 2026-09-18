using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Astra.Views;

namespace Astra.Services
{
    /// <summary>
    /// Captures ShareCardView (see that file - CardWidth/CardHeight must match its root
    /// Border's Width/Height exactly) to a PNG via RenderTargetBitmap. Must run on the WPF
    /// UI (STA) thread - only ever invoked from a button click, already on that thread.
    /// </summary>
    public class ShareCardRenderer
    {
        public const int CardWidth = 1080;
        public const int CardHeight = 1350;

        public void RenderToFile(ShareCardViewModel data, string filePath)
        {
            var view = new ShareCardView { DataContext = data };
            var size = new Size(CardWidth, CardHeight);
            view.Measure(size);
            view.Arrange(new Rect(size));
            view.UpdateLayout();

            var bitmap = new RenderTargetBitmap(CardWidth, CardHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
    }
}
