using Astra;

namespace Astra.Models
{
    /// <summary>One selectable genre/platform chip in the Backlog page's filter rows. Extends
    /// ViewModelBase (like AstraSettings does) so a ToggleButton can two-way bind IsSelected
    /// directly and the page view-model can react to any chip's PropertyChanged.</summary>
    public class FilterOption : ViewModelBase
    {
        public string Label { get; }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetValue(ref isSelected, value);
        }

        public FilterOption(string label)
        {
            Label = label;
        }
    }
}
