using System.Windows;
using System.Windows.Controls;

namespace Astra.Views
{
    public partial class MostPlayedView : UserControl
    {
        public MostPlayedView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the row's own ContextMenu (Tag holds a reference to the row's root Grid, set via
        /// ElementName in the row's DataTemplate) on a left-click of the "..." button, in addition
        /// to the normal right-click-anywhere-on-the-row behavior the menu already supports.
        /// Deliberately does NOT assign the ContextMenu to the button's own ContextMenu property -
        /// a ContextMenu can only be owned by one FrameworkElement at a time, and it's already
        /// owned by the row's Grid.
        /// </summary>
        private void MoreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            if (button.Tag is FrameworkElement row && row.ContextMenu != null)
            {
                row.ContextMenu.IsOpen = true;
            }
        }
    }
}
