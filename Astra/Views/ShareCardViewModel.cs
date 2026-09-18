using System.Collections.Generic;
using System.Linq;
using Astra.Models;

namespace Astra.Views
{
    /// <summary>
    /// One-shot snapshot handed to ShareCardRenderer: constructed once, bound once, rendered
    /// once, then discarded. No ViewModelBase/INotifyPropertyChanged - nothing here is ever
    /// edited or re-bound after construction.
    /// </summary>
    public class ShareCardViewModel
    {
        public RecapData Recap { get; set; }
        public string DisplayName { get; set; }

        public List<GameRecapEntry> TopFive => Recap?.TopGames?.Take(5).ToList() ?? new List<GameRecapEntry>();
    }
}
