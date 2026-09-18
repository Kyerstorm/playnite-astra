using System.Windows.Input;

namespace Astra.Views
{
    /// <summary>
    /// Owns Astra's single Playnite sidebar entry's content: an internal nav panel
    /// (Home / Trends / Most Played / Recap) plus a content area that swaps between them,
    /// styled after GameScrobbler's in-plugin sidebar rather than three separate
    /// Playnite sidebar icons (the latter rendered as broken/untinted icon
    /// placeholders in Playnite's own rail - see CLAUDE.md "UI redesign").
    /// </summary>
    public class AstraShellViewModel : ViewModelBase
    {
        private readonly AstraSettings settings;

        public HomeViewModel Home { get; }
        public TrendsViewModel Trends { get; }
        public MostPlayedViewModel MostPlayed { get; }
        public RecapViewModel Recap { get; }
        public BacklogViewModel Backlog { get; }

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
                    NotifyPropertyChanged(nameof(IsTrendsActive));
                    NotifyPropertyChanged(nameof(IsMostPlayedActive));
                    NotifyPropertyChanged(nameof(IsRecapActive));
                    NotifyPropertyChanged(nameof(IsBacklogActive));
                }
            }
        }

        public bool IsHomeActive => ReferenceEquals(CurrentPage, Home);
        public bool IsTrendsActive => ReferenceEquals(CurrentPage, Trends);
        public bool IsMostPlayedActive => ReferenceEquals(CurrentPage, MostPlayed);
        public bool IsRecapActive => ReferenceEquals(CurrentPage, Recap);
        public bool IsBacklogActive => ReferenceEquals(CurrentPage, Backlog);

        public ICommand ShowHomeCommand { get; }
        public ICommand ShowTrendsCommand { get; }
        public ICommand ShowMostPlayedCommand { get; }
        public ICommand ShowRecapCommand { get; }
        public ICommand ShowBacklogCommand { get; }

        public AstraShellViewModel(Astra plugin, AstraSettings settings)
        {
            this.settings = settings;

            Home = new HomeViewModel(plugin, settings);
            Trends = new TrendsViewModel(plugin, settings);
            MostPlayed = new MostPlayedViewModel(plugin, settings);
            Recap = new RecapViewModel(plugin, settings);
            Backlog = new BacklogViewModel(plugin, settings);

            // Preserves Home's year context when navigating via "View Trends ->": Home / 2025 ->
            // Trends / Year / 2025. Lands on the Year tab (not Month) since only Year can reach an
            // arbitrary past year - Month's dropdown only ever lists the most recent 12 months.
            Home.ViewTrendsRequested += targetYear =>
            {
                Trends.ShowYear(targetYear);
                CurrentPage = Trends;
            };

            ShowHomeCommand = new RelayCommand(_ => CurrentPage = Home);
            ShowTrendsCommand = new RelayCommand(_ => CurrentPage = Trends);
            ShowMostPlayedCommand = new RelayCommand(_ => CurrentPage = MostPlayed);
            ShowRecapCommand = new RelayCommand(_ => CurrentPage = Recap);
            ShowBacklogCommand = new RelayCommand(_ => CurrentPage = Backlog);

            currentPage = Home;
        }
    }
}
