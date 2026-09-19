using Astra;

namespace Astra.Models
{
    /// <summary>One selectable theme swatch-card on the Settings page. Mirrors FilterOption's
    /// shape so a click can two-way bind IsSelected directly, but is used single-select
    /// (radio-button-like) rather than FilterOption's multi-select chip usage.</summary>
    public class ThemeOption : ViewModelBase
    {
        public string Name { get; }
        public string Label { get; }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetValue(ref isSelected, value);
        }

        public ThemeOption(string name, string label)
        {
            Name = name;
            Label = label;
        }
    }
}
