using System.Windows;
using System.Windows.Controls;

namespace Astra.Views
{
    public partial class AstraShellView : UserControl
    {
        public AstraShellView()
        {
            InitializeComponent();
            // Wired here, not as a XAML Loaded="..." attribute on this root element - the markup
            // compiler would emit a fully-qualified cast whose leading "Astra" token resolves to
            // the plugin's own GenericPlugin subclass instead of the Views namespace (CS0426).
            // See CLAUDE.md's TrendChartControl note for the first time this was hit.
            Loaded += AstraShellView_Loaded;
        }

        private void AstraShellView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is AstraShellViewModel vm))
            {
                return;
            }

            ApplyTheme(vm);
            vm.Settings.PropertyChanged += (s, args) =>
            {
                if (args.PropertyName == nameof(AstraSettings.ThemeName) ||
                    args.PropertyName == nameof(AstraSettings.AccentColor))
                {
                    ApplyTheme(vm);
                }
            };
        }

        private void ApplyTheme(AstraShellViewModel vm)
        {
            AstraThemeApplier.Apply(this, vm.Plugin, vm.Settings.ThemeName, vm.Settings.AccentColor);
        }
    }
}
