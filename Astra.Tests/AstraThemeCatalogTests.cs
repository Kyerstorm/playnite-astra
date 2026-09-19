using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class AstraThemeCatalogTests
    {
        [Theory]
        [InlineData("Dark", "Themes/Dark.xaml")]
        [InlineData("Light", "Themes/Light.xaml")]
        [InlineData("Oled", "Themes/Oled.xaml")]
        [InlineData("Playnite", null)]
        [InlineData("SomeFutureTheme", null)]
        public void ResourcePathFor_ReturnsExpectedPathOrNullForFollowPlaynite(string theme, string expected)
        {
            Assert.Equal(expected, AstraThemeCatalog.ResourcePathFor(theme));
        }

        [Fact]
        public void HexFor_KnownAccentName_ReturnsItsHex()
        {
            Assert.Equal("#3EC6E0", AstraThemeCatalog.HexFor("Teal"));
        }

        [Fact]
        public void HexFor_UnknownAccentName_FallsBackToFirstAccent()
        {
            Assert.Equal(AstraThemeCatalog.Accents[0].Hex, AstraThemeCatalog.HexFor("NotARealAccent"));
        }
    }
}
