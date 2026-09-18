using System.Windows.Controls;
using System.Windows.Input;

namespace Astra.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        /// <summary>Redirects a mouse-wheel gesture over the "New This Year" carousel to its own
        /// horizontal offset instead of letting it bubble to the page-level ScrollViewer added
        /// around this whole view - without this, WPF's default wheel handling (which only moves
        /// a ScrollViewer's vertical offset) finds this carousel's vertical scrolling disabled and
        /// passes the event up to the nearest ancestor that CAN scroll vertically, hijacking the
        /// whole page instead of the strip the user is actually hovering.</summary>
        private void OnCarouselPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
