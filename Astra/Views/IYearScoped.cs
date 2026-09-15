namespace Astra.Views
{
    /// <summary>
    /// Implemented by every page view-model that has its own year selector (Home,
    /// Most Played, Recap). Unlike the old three-separate-sidebar-icon design (where
    /// each Opened() call re-read AstraSettings.LastSelectedYear from scratch), the
    /// shell now keeps all three view-models alive at once and swaps between them,
    /// so a year change on one page needs to be pushed into whichever page is
    /// navigated to next - see AstraShellViewModel.CurrentPage.
    /// </summary>
    public interface IYearScoped
    {
        int Year { get; set; }
    }
}
