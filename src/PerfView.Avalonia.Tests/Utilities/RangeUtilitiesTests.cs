using Microsoft.Diagnostics.Utilities;
using System;
using System.Globalization;
using Xunit;

namespace PerfViewTests.Utilities
{
    /// <summary>
    /// Avalonia/.NET Core version of RangeUtilitiesTests.
    /// 
    /// Note: RangeUtilities resolves its separator ONCE at static init time based
    /// on the process's default culture. Changing CultureInfo.CurrentCulture per-test
    /// doesn't affect the separator (unlike the WPF tests which used AppDomain isolation).
    /// These tests therefore focus on the actual runtime behavior: formatting numbers
    /// with the per-test culture but using the process-default separator.
    /// </summary>
    public static class RangeUtilitiesTests
    {
        /// <summary>
        /// Determine the separator that RangeUtilities resolved at startup
        /// by round-tripping a known value.
        /// </summary>
        private static string GetRuntimeSeparator()
        {
            // Use en-US culture for formatting so we get "1.000" format
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                var output = RangeUtilities.ToString(1.0, 2.0);
                // output is "1.000{sep}2.000" — extract the separator
                var idx1 = output.IndexOf("1.000") + 5;
                var idx2 = output.IndexOf("2.000");
                return output.Substring(idx1, idx2 - idx1);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Fact]
        public static void RoundTrip_EnUS()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var text = RangeUtilities.ToString(123456789.123, 234567890.123);
                Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
                Assert.Equal(123456789.123, start, precision: 3);
                Assert.Equal(234567890.123, end, precision: 3);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Fact]
        public static void RoundTrip_RuRU()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
                var text = RangeUtilities.ToString(123456789.123, 234567890.123);
                Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
                Assert.Equal(123456789.123, start, precision: 3);
                Assert.Equal(234567890.123, end, precision: 3);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Fact]
        public static void RoundTrip_PtPT()
        {
            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("pt-PT");
                var text = RangeUtilities.ToString(123456789.123, 234567890.123);
                Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
                Assert.Equal(123456789.123, start, precision: 3);
                Assert.Equal(234567890.123, end, precision: 3);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Fact]
        public static void RoundTrip_Zeros()
        {
            var text = RangeUtilities.ToString(0.0, 0.0);
            Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
            Assert.Equal(0.0, start);
            Assert.Equal(0.0, end);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a range")]
        [InlineData("123.456")]
        public static void TryParse_InvalidInput_ReturnsFalse(string text)
        {
            Assert.False(RangeUtilities.TryParse(text, out _, out _));
        }

        [Fact]
        public static void TryParse_PipeEnclosed_Works()
        {
            // The pipe-enclosed format (from markdown tables) should be supported
            var sep = GetRuntimeSeparator();
            var inner = "1,395.251" + sep + "2,626.358";
            var text = "| " + inner + " |";

            var saved = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
                Assert.Equal(1395.251, start, precision: 3);
                Assert.Equal(2626.358, end, precision: 3);
            }
            finally
            {
                CultureInfo.CurrentCulture = saved;
            }
        }

        [Fact]
        public static void ToString_ProducesParsableOutput()
        {
            // Whatever ToString produces should be parsable by TryParse
            var text = RangeUtilities.ToString(42.5, 100.75);
            Assert.True(RangeUtilities.TryParse(text, out var start, out var end));
            Assert.Equal(42.5, start, precision: 3);
            Assert.Equal(100.75, end, precision: 3);
        }
    }
}
