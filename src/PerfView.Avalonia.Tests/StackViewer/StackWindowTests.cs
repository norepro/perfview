using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Diagnostics.Tracing.StackSources;
using Microsoft.Diagnostics.Tracing.Stacks;
using PerfView;
using Xunit;

namespace PerfViewTests.StackViewer
{
    /// <summary>
    /// Avalonia-specific tests for the StackWindow.
    /// These tests run headlessly via <see cref="AvaloniaFactAttribute"/>.
    /// </summary>
    public class StackWindowTests
    {
        #region StackWindow construction and controls

        /// <summary>
        /// Verifies that the design-time (parameterless) constructor produces
        /// a valid StackWindow with all expected named controls present.
        /// </summary>
        [AvaloniaFact]
        public void DesignTimeConstructor_CreatesWindowWithExpectedControls()
        {
            var window = new StackWindow();

            // Filter text boxes
            Assert.NotNull(window.StartTextBox);
            Assert.NotNull(window.EndTextBox);
            Assert.NotNull(window.GroupRegExTextBox);
            Assert.NotNull(window.FoldPercentTextBox);
            Assert.NotNull(window.FoldRegExTextBox);
            Assert.NotNull(window.IncludeRegExTextBox);
            Assert.NotNull(window.ExcludeRegExTextBox);

            // Data grids
            Assert.NotNull(window.ByNameDataGrid);
            Assert.NotNull(window.CallTreeDataGrid);
            Assert.NotNull(window.CallersDataGrid);
            Assert.NotNull(window.CalleesDataGrid);

            // Other controls
            Assert.NotNull(window.CallerCalleeView);
            Assert.NotNull(window.StatusBar);
            Assert.NotNull(window.FindTextBox);
        }

        /// <summary>
        /// Verifies that the StackWindow has the expected tab structure.
        /// </summary>
        [AvaloniaFact]
        public void DesignTimeConstructor_HasExpectedTabs()
        {
            var window = new StackWindow();

            Assert.NotNull(window.ByNameTab);
            Assert.NotNull(window.CallTreeTab);
            Assert.NotNull(window.CallersTab);
            Assert.NotNull(window.CalleesTab);
            Assert.NotNull(window.CallerCalleeTab);
            Assert.NotNull(window.FlameGraphTab);
        }

        #endregion

        #region Filter property round-trip

        /// <summary>
        /// Setting the <see cref="StackWindow.Filter"/> property and reading
        /// it back should produce the same <see cref="FilterParams"/> values.
        /// </summary>
        [AvaloniaFact]
        public void FilterProperty_RoundTrips()
        {
            var window = new StackWindow();

            var input = new FilterParams
            {
                StartTimeRelativeMSec = "100.000",
                EndTimeRelativeMSec = "999.999",
                MinInclusiveTimePercent = "5",
                FoldRegExs = @"ntoskrnl!%",
                IncludeRegExs = @"^MyModule",
                ExcludeRegExs = @"^System\.GC",
                GroupRegExs = @"[group module entries]  {%}!=>module $1",
                TypePriority = "-clr.dll",
            };

            window.Filter = input;
            FilterParams output = window.Filter;

            Assert.Equal(input.StartTimeRelativeMSec, output.StartTimeRelativeMSec);
            Assert.Equal(input.EndTimeRelativeMSec, output.EndTimeRelativeMSec);
            Assert.Equal(input.MinInclusiveTimePercent, output.MinInclusiveTimePercent);
            Assert.Equal(input.FoldRegExs, output.FoldRegExs);
            Assert.Equal(input.IncludeRegExs, output.IncludeRegExs);
            Assert.Equal(input.ExcludeRegExs, output.ExcludeRegExs);
            Assert.Equal(input.GroupRegExs, output.GroupRegExs);
            Assert.Equal(input.TypePriority, output.TypePriority);
        }

        /// <summary>
        /// An empty <see cref="FilterParams"/> should round-trip cleanly
        /// (all string properties should be empty strings, not null).
        /// </summary>
        [AvaloniaFact]
        public void FilterProperty_EmptyRoundTrips()
        {
            var window = new StackWindow();

            window.Filter = new FilterParams();
            FilterParams output = window.Filter;

            Assert.Equal("", output.StartTimeRelativeMSec);
            Assert.Equal("", output.EndTimeRelativeMSec);
            Assert.Equal("", output.MinInclusiveTimePercent);
            Assert.Equal("", output.FoldRegExs);
            Assert.Equal("", output.IncludeRegExs);
            Assert.Equal("", output.ExcludeRegExs);
            Assert.Equal("", output.GroupRegExs);
            Assert.Equal("", output.TypePriority);
        }

        /// <summary>
        /// Setting the filter twice should overwrite previous values cleanly.
        /// </summary>
        [AvaloniaFact]
        public void FilterProperty_OverwritesPreviousValues()
        {
            var window = new StackWindow();

            window.Filter = new FilterParams
            {
                IncludeRegExs = "^First",
                ExcludeRegExs = "^OldExclude",
            };

            window.Filter = new FilterParams
            {
                IncludeRegExs = "^Second",
                ExcludeRegExs = "",
            };

            FilterParams output = window.Filter;
            Assert.Equal("^Second", output.IncludeRegExs);
            Assert.Equal("", output.ExcludeRegExs);
        }

        #endregion

        #region CallTree from synthetic StackSource

        /// <summary>
        /// A synthetic <see cref="StackSource"/> that produces four samples with
        /// three distinct frames, mirroring the WPF test's TimeRangeStackSource.
        /// </summary>
        private sealed class TestStackSource : StackSource
        {
            private const StackSourceCallStackIndex StackEntry = StackSourceCallStackIndex.Start + 0;
            private const StackSourceCallStackIndex StackMiddle = StackSourceCallStackIndex.Start + 1;
            private const StackSourceCallStackIndex StackTail = StackSourceCallStackIndex.Start + 2;

            private const StackSourceFrameIndex FrameEntry = StackSourceFrameIndex.Start + 0;
            private const StackSourceFrameIndex FrameMiddle = StackSourceFrameIndex.Start + 1;
            private const StackSourceFrameIndex FrameTail = StackSourceFrameIndex.Start + 2;

            private readonly List<StackSourceSample> m_samples;

            public TestStackSource()
            {
                m_samples = new List<StackSourceSample>
                {
                    new StackSourceSample(this) { SampleIndex = 0, StackIndex = StackEntry, Metric = 1, TimeRelativeMSec = 0 },
                    new StackSourceSample(this) { SampleIndex = (StackSourceSampleIndex)1, StackIndex = StackMiddle, Metric = 1, TimeRelativeMSec = 1000.25 },
                    new StackSourceSample(this) { SampleIndex = (StackSourceSampleIndex)2, StackIndex = StackMiddle, Metric = 1, TimeRelativeMSec = 2000.5 },
                    new StackSourceSample(this) { SampleIndex = (StackSourceSampleIndex)3, StackIndex = StackTail, Metric = 1, TimeRelativeMSec = 3000.75 },
                };
            }

            public override int CallStackIndexLimit => (int)StackTail + 1;
            public override int CallFrameIndexLimit => (int)FrameTail + 1;
            public override int SampleIndexLimit => m_samples.Count;
            public override double SampleTimeRelativeMSecLimit => m_samples.Max(s => s.TimeRelativeMSec);

            public override void ForEach(Action<StackSourceSample> callback)
            {
                foreach (var sample in m_samples)
                {
                    callback(sample);
                }
            }

            public override StackSourceSample GetSampleByIndex(StackSourceSampleIndex sampleIndex)
                => m_samples[(int)sampleIndex];

            public override StackSourceCallStackIndex GetCallerIndex(StackSourceCallStackIndex callStackIndex)
                => StackSourceCallStackIndex.Invalid;

            public override StackSourceFrameIndex GetFrameIndex(StackSourceCallStackIndex callStackIndex)
            {
                return callStackIndex switch
                {
                    StackEntry => FrameEntry,
                    StackMiddle => FrameMiddle,
                    StackTail => FrameTail,
                    _ => StackSourceFrameIndex.Root,
                };
            }

            public override string GetFrameName(StackSourceFrameIndex frameIndex, bool verboseName)
            {
                if (frameIndex >= StackSourceFrameIndex.Start)
                {
                    return "Frame" + (frameIndex - StackSourceFrameIndex.Start);
                }

                return frameIndex.ToString();
            }
        }

        /// <summary>
        /// Building a <see cref="CallTree"/> from a synthetic stack source should
        /// yield a root with the expected inclusive metric and callees.
        /// </summary>
        [Fact]
        public void CallTree_FromSyntheticSource_HasExpectedStructure()
        {
            var source = new TestStackSource();
            var callTree = new CallTree(ScalingPolicyKind.ScaleToData);
            callTree.StackSource = source;

            // The root should aggregate all 4 samples (metric = 4).
            Assert.Equal(4, callTree.Root.InclusiveMetric);

            // Each sample has a single-frame stack, so 3 distinct callees under ROOT.
            Assert.Equal(3, callTree.Root.Callees.Count);
        }

        /// <summary>
        /// A <see cref="FilterStackSource"/> filtering by time range should
        /// exclude samples outside the window.
        /// </summary>
        [Fact]
        public void FilterStackSource_ByTimeRange_ExcludesSamplesOutsideRange()
        {
            var source = new TestStackSource();
            var filter = new FilterParams
            {
                StartTimeRelativeMSec = "500",
                EndTimeRelativeMSec = "2500",
            };

            var filtered = new FilterStackSource(filter, source, ScalingPolicyKind.ScaleToData);
            var callTree = new CallTree(ScalingPolicyKind.ScaleToData);
            callTree.StackSource = filtered;

            // Only samples at 1000.25 and 2000.5 are within [500, 2500].
            Assert.Equal(2, callTree.Root.InclusiveMetric);
        }

        /// <summary>
        /// ByID aggregation should produce one entry per distinct frame name.
        /// </summary>
        [Fact]
        public void CallTree_ByID_ProducesExpectedEntries()
        {
            var source = new TestStackSource();
            var callTree = new CallTree(ScalingPolicyKind.ScaleToData);
            callTree.StackSource = source;

            var byId = callTree.ByID.ToList();

            // 3 distinct frames + ROOT
            Assert.True(byId.Count >= 3, $"Expected at least 3 ByID entries but got {byId.Count}");

            // Frame1 (StackMiddle) has metric 2 (two samples)
            var middleNode = byId.FirstOrDefault(n => n.Name == "Frame1");
            Assert.NotNull(middleNode);
            Assert.Equal(2, middleNode.InclusiveMetric);
        }

        /// <summary>
        /// CallerCallee aggregation for a known node name should work.
        /// </summary>
        [Fact]
        public void CallTree_CallerCallee_ProducesExpectedNode()
        {
            var source = new TestStackSource();
            var callTree = new CallTree(ScalingPolicyKind.ScaleToData);
            callTree.StackSource = source;

            var callerCallee = callTree.CallerCallee("Frame1");
            Assert.NotNull(callerCallee);
            Assert.Equal("Frame1", callerCallee.Name);
            Assert.Equal(2, callerCallee.InclusiveMetric);
        }

        #endregion
    }
}
