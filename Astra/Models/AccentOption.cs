using Astra;

namespace Astra.Models
{
    /// <summary>One selectable accent-color swatch dot on the Settings page. Same single-select
    /// shape as ThemeOption, plus the Hex value the swatch dot's Fill binds to (via the existing
    /// ColorHexToBrushConverter - no new converter needed).</summary>
    public class AccentOption : ViewModelBase
    {
        public string Name { get; }
        public string Label { get; }
        public string Hex { get; }

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetValue(ref isSelected, value);
        }

        public AccentOption(string name, string label, string hex)
        {
            Name = name;
            Label = label;
            Hex = hex;
        }
    }
}
