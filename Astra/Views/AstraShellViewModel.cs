using System.Windows.Input;

namespace Astra.Views
{
    /// <summary>
    /// Owns Astra's single Playnite sidebar entry's content: an internal nav panel
    /// (Home / Most Played / Recap) plus a content area that swaps between them,
    /// styled after GameScrobbler's in-plugin sidebar rather than three separate
    /// Playnite sidebar icons (the latter rendered as broken/untinted icon
    /// placeholders in Playnite's own rail - see CLAUDE.md "UI redesign").
    /// </summary>
    public class AstraShellViewModel : ViewModelBase
    {
        private readonly AstraSettings settings;

        public HomeViewModel Home { get; }
        public MostPlayedViewModel MostPlayed { get; }
        public RecapViewModel Recap { get; }

        private object currentPage;
        public object CurrentPage
        {
            get => currentPage;
            set
            {
                if (SetValue(ref currentPage, value))
                {
                    // Unlike the old three-separate-sidebar-icon design, all three page
                    // view-models stay alive for the shell's lifetime instead of being
                    // reconstructed on every navigation, so a year change made on one page
                    // (e.g. Home) wouldn't otherwise be reflected when switching to another
                    // (e.g. Most Played) without this push-on-navigate sync.
                    if (value is IYearScoped yearScoped && yearScoped.Year != settings.LastSelectedYear)
                    {
                        yearScoped.Year = settings.LastSelectedYear;
                    }

                    NotifyPropertyChanged(nameof(IsHomeActive));
                    NotifyPropertyChanged(nameof(IsMostPlayedActive));
                    NotifyPropertyChanged(nameof(IsRecapActive));
                }
            }
        }

        public bool IsHomeActive => ReferenceEquals(CurrentPage, Home);
        public bool IsMostPlayedActive => ReferenceEquals(CurrentPage, MostPlayed);
        public bool IsRecapActive => ReferenceEquals(CurrentPage, Recap);

        public ICommand ShowHomeCommand { get; }
        public ICommand ShowMostPlayedCommand { get; }
        public ICommand ShowRecapCommand { get; }

        public AstraShellViewModel(Astra plugin, AstraSettings settings)
        {
            this.settings = settings;

            Home = new HomeViewModel(plugin, settings);
            MostPlayed = new MostPlayedViewModel(plugin, settings);
            Recap = new RecapViewModel(plugin, settings);

            ShowHomeCommand = new RelayCommand(_ => CurrentPage = Home);
            ShowMostPlayedCommand = new RelayCommand(_ => CurrentPage = MostPlayed);
            ShowRecapCommand = new RelayCommand(_ => CurrentPage = Recap);

            currentPage = Home;
        }
    }
}
