using Astra.Services;
using Xunit;

namespace Astra.Tests
{
    public class CoverPlaceholderTests
    {
        [Fact]
        public void ColorHexFor_SameName_IsDeterministic()
        {
            Assert.Equal(CoverPlaceholder.ColorHexFor("Hollow Knight"), CoverPlaceholder.ColorHexFor("Hollow Knight"));
        }

        [Fact]
        public void ColorHexFor_DifferentNames_UsuallyDiffer()
        {
            Assert.NotEqual(CoverPlaceholder.ColorHexFor("Hollow Knight"), CoverPlaceholder.ColorHexFor("Celeste"));
        }

        [Fact]
        public void ColorHexFor_ReturnsWellFormedHex()
        {
            var hex = CoverPlaceholder.ColorHexFor("Baldur's Gate 3");
            Assert.Matches("^#[0-9A-F]{6}$", hex);
        }

        [Theory]
        [InlineData("Hollow Knight", "HK")]
        [InlineData("Celeste", "CE")]
        [InlineData("Baldur's Gate 3", "BG")]
        public void InitialsFor_MultiWordOrSingleWordNames_ProducesExpectedInitials(string name, string expected)
        {
            Assert.Equal(expected, CoverPlaceholder.InitialsFor(name));
        }

        [Fact]
        public void InitialsFor_EmptyName_DoesNotThrow()
        {
            Assert.Equal("?", CoverPlaceholder.InitialsFor(""));
        }
    }
}
