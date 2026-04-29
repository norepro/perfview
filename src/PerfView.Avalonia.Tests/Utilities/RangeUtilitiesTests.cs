using Microsoft.Diagnostics.Utilities;
using System;
using System.Globalization;
using Xunit;

namespace PerfViewTests.Utilities
{
    /// <summary>
    /// Avalonia/.NET Core version of RangeUtilitiesTests.
    /// Replaces the AppDomain-based culture isolation with direct CultureInfo.CurrentCulture assignment.
    /// The custom culture test cases (custom1/custom2) from the WPF version are omitted because
    /// .NET Core's CultureInfo doesn't allow overriding TextInfo.ListSeparator on standard cultures.
    /// </summary>
    public static class RangeUtilitiesTests
    {
        [Theory]
        [InlineData("en-US", "", default(double), default(double), false)]
        [InlineData("en-US", "XXXXXXXXXXXXXXX 234,567,890.123", default(double), default(double), false)]
        [InlineData("en-US", "123,456,789.123 XXXXXXXXXXXXXXX", default(double), default(double), false)]
        // On .NET Core, en-US ListSeparator is "," which conflicts with number group separator,
        // so RangeUtilities uses "|" as delimiter instead of space.
        [InlineData("en-US", "123,456,789.123|234,567,890.123", 123456789.123, 234567890.123, true)]
        [InlineData("en-US", "|  1,395.251|2,626.358 |", 1395.251, 2626.358, true)]
        [InlineData("en-US", "| 123,456,789.123|234,567,890.123 |", 123456789.123, 234567890.123, true)]
        [InlineData("en-US", "|123,456,789.123|234,567,890.123|", 123456789.123, 234567890.123, true)]
        [InlineData("ru-RU", "", default(double), default(double), false)]
        [InlineData("ru-RU", "XXXXXXXXXXXXXXX|234 567\u00A0890,123", default(double), default(double), false)]
        [InlineData("ru-RU", "123\u00A0456 789,123|XXXXXXXXXXXXXXX", default(double), default(double), false)]
        [InlineData("ru-RU", "123\u00A0456 789,123|234\u00A0567\u00A0890,123", 123456789.123, 234567890.123, true)]
        [InlineData("ru-RU", "| 123\u00A0456 789,123|234\u00A0567\u00A0890,123 |", 123456789.123, 234567890.123, true)]
        [InlineData("pt-PT", "", default(double), default(double), false)]
        [InlineData("pt-PT", "XXXXXXXXXXXXXXX|234 567 890,123", default(double), default(double), false)]
        [InlineData("pt-PT", "123 456 789,123|XXXXXXXXXXXXXXX", default(double), default(double), false)]
        [InlineData("pt-PT", "123 456 789,123|234 567 890,123", 123456789.123, 234567890.123, true)]
        public static void TryParseTests(string culture, string text, double expectedStart, double expectedEnd, bool expectedResult)
        {
            var savedCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                var actual = RangeUtilities.TryParse(text, out var actualStart, out var actualEnd);
                Assert.Equal(expectedResult, actual);
                Assert.Equal(expectedStart, actualStart);
                Assert.Equal(expectedEnd, actualEnd);
            }
            finally
            {
                CultureInfo.CurrentCulture = savedCulture;
            }
        }

        [Theory]
        // On .NET Core, en-US ListSeparator is "," which conflicts with number group separator,
        // so RangeUtilities uses "|" as delimiter.
        [InlineData("en-US", default(double), default(double), "0.000|0.000")]
        [InlineData("en-US", 123456789.123456, 234567890.123456, "123,456,789.123|234,567,890.123")]
        [InlineData("ru-RU", default(double), default(double), "0,000|0,000")]
        [InlineData("ru-RU", 123456789.123456, 234567890.123456, "123\u00A0456\u00A0789,123|234\u00A0567\u00A0890,123")]
        [InlineData("pt-PT", default(double), default(double), "0,000|0,000")]
        [InlineData("pt-PT", 123456789.123456, 234567890.123456, "123\u00A0456\u00A0789,123|234\u00A0567\u00A0890,123")]
        public static void ToStringTests(string culture, double start, double end, string expected)
        {
            var savedCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);
                var actual = RangeUtilities.ToString(start, end);
                Assert.Equal(expected, actual);
            }
            finally
            {
                CultureInfo.CurrentCulture = savedCulture;
            }
        }
    }
}
