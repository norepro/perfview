using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Microsoft.Diagnostics.Tracing.Stacks;
using PerfView.Utilities;
using static PerfView.FlameGraph;

#if !AVALONIA
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
#else
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
#endif

namespace PerfView
{
    public class FlameGraphDrawingCanvas :
#if AVALONIA
        Control
#else
        Canvas
#endif
    {
        private static readonly Typeface Typeface = new Typeface("Consolas");

        private static readonly Brush[][] Brushes = GenerateBrushes();

        public event EventHandler<string> CurrentFlameBoxChanged;

        private List<Visual> visuals = new List<Visual>();
        private FlameBoxesMap flameBoxesMap = new FlameBoxesMap();
        private ToolTip tooltip = new ToolTip() { FontSize = 20.0 };
        private CallTreeNode selectedNode;
#if !AVALONIA
        private ScaleTransform scaleTransform = new ScaleTransform(1.0f, 1.0f, 0.0f, 0.0f);
#else
        private ScaleTransform scaleTransform = new ScaleTransform(1.0f, 1.0f);
        private double _scaleCenterX, _scaleCenterY;
#endif
        private Cursor cursor;

        public FlameGraphDrawingCanvas()
        {
#if AVALONIA
            PointerMoved += OnMouseMove;
            PointerExited += OnMouseLeave;
#else
            MouseMove += OnMouseMove;
            MouseLeave += OnMouseLeave;
            MouseRightButtonDown += (s, e) => selectedNode = flameBoxesMap.Find(e.MouseDevice.GetPosition(this)).Node;
#endif
#if !AVALONIA
            PreviewMouseWheel += OnPreviewMouseWheel;
            MouseLeftButtonDown += OnMouseLeftButtonDown;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
            PreviewKeyDown += OnPreviewKeyDown;
#else
            PointerWheelChanged += OnPreviewMouseWheel;
            PointerPressed += OnMouseLeftButtonDown;
            PointerReleased += OnMouseLeftButtonUp;
            KeyDown += OnPreviewKeyDown;
#endif
            Focusable = true;
        }

        public bool IsEmpty => visuals.Count == 0;

        public CallTreeNodeBase SelectedNode => selectedNode;

#if !AVALONIA
        protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) => visuals[index];
#endif

        private bool IsZoomed => scaleTransform.ScaleX != 1.0;

#if AVALONIA
        private static readonly IBrush BlackBrush = new SolidColorBrush(Color.FromRgb(12, 12, 12));
        private static readonly IBrush TextBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));

        public void Draw(IEnumerable<FlameBox> boxes)
        {
            Clear();

            foreach (var box in boxes)
            {
                flameBoxesMap.Add(box);
            }

            flameBoxesMap.Sort();
            InvalidateVisual();
        }

        public override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);

            // Draw background to enable hit testing for pointer events
            drawingContext.FillRectangle(global::Avalonia.Media.Brushes.Transparent, new Rect(Bounds.Size));

            if (flameBoxesMap.EnumerateBoxes().Any() == false)
                return;

            // Apply scale transform around center point
            using (drawingContext.PushTransform(
                Matrix.CreateTranslation(-_scaleCenterX, -_scaleCenterY) *
                Matrix.CreateScale(scaleTransform.ScaleX, scaleTransform.ScaleY) *
                Matrix.CreateTranslation(_scaleCenterX, _scaleCenterY)))
            {
                // Draw borders around flame boxes using rectangles rather than Pen for performance
                double maxBorder = 0.5 / scaleTransform.ScaleX;
                const double MaxBorderPercent = 0.2;
                foreach (var box in flameBoxesMap.EnumerateBoxes())
                {
                    var node = box.Node;

                    // Draw root border box
                    if (node.Caller == null)
                    {
                        var rootBorderBox = new Rect(box.X - maxBorder, box.Y - maxBorder, box.Width + 2 * maxBorder, box.Height);
                        drawingContext.DrawRectangle(BlackBrush, null, rootBorderBox);
                    }

                    // Draw a single border box around all children
                    if (node.Callees != null)
                    {
                        double childrenRatio = node.Callees.Sum(child => Math.Abs(child.InclusiveMetric)) / Math.Abs(node.InclusiveMetric);
                        double childrenWidth = box.Width * childrenRatio;
                        double childrenX = box.X + (box.Width - childrenWidth) / 2.0;
                        double childrenY = box.Y - box.Height;
                        var borderSize = Math.Min(maxBorder, childrenWidth * MaxBorderPercent);
                        var borderBox = new Rect(childrenX - borderSize, childrenY - borderSize, childrenWidth + 2 * borderSize, box.Height);
                        drawingContext.DrawRectangle(BlackBrush, null, borderBox);
                    }
                }

                int index = 0;
                foreach (var box in flameBoxesMap.EnumerateBoxes())
                {
                    var brushSet = Brushes[box.Node.InclusiveMetric < 0 ? 1 : 0];
                    var brush = brushSet[index++ % brushSet.Length];

                    var boxRectangle = new Rect(box.X, box.Y, box.Width, box.Height);
                    drawingContext.DrawRectangle(brush, null, boxRectangle);

                    if (box.Width > 50 && box.Height >= 6)
                    {
                        // Compute font size: approximate SizeInPoints from pixel height
                        double fontSize = Math.Min(box.Height * 0.75, 20);

                        var text = new FormattedText(
                                box.Node.DisplayName,
                                CultureInfo.InvariantCulture,
                                FlowDirection.LeftToRight,
                                Typeface,
                                fontSize,
                                TextBrush);

                        text.MaxTextWidth = box.Width;
                        text.MaxTextHeight = box.Height;

                        drawingContext.DrawText(text, new Point(box.X, box.Y));
                    }
                }
            }
        }
#else
        public void Draw(IEnumerable<FlameBox> boxes)
        {
            var blackBrush = new SolidColorBrush(Color.FromRgb(12, 12, 12));
            blackBrush.Freeze();

            Clear();

            var visual = new DrawingVisual { Transform = scaleTransform }; // we have only one visual to provide best possible perf

            using (DrawingContext drawingContext = visual.RenderOpen())
            {
                // Draw borders around flame boxes using rectangles rather than Pen for performance
                double maxBorder = 0.5 / scaleTransform.ScaleX; // Border thickness of 0.5 at all zoom levels
                const double MaxBorderPercent = 0.2; // For thin boxes, ensure border is not thicker than 20% of the box width
                foreach (var box in boxes)
                {
                    var node = box.Node;

                    // Draw root border box
                    if (node.Caller == null)
                    {
                        var rootBorderBox = new Rect(box.X - maxBorder, box.Y - maxBorder, box.Width + 2 * maxBorder, box.Height);
                        drawingContext.DrawRectangle(blackBrush, null, rootBorderBox);
                    }

                    // Draw a single border box around all children - assumes that all children are adjacent which is true in FlameGraph.Calculate
                    if (node.Callees != null)
                    {
                        double childrenRatio = node.Callees.Sum(child => Math.Abs(child.InclusiveMetric)) / Math.Abs(node.InclusiveMetric);
                        double childrenWidth = box.Width * childrenRatio;
                        double childrenX = box.X + (box.Width - childrenWidth) / 2.0;
                        double childrenY = box.Y - box.Height;
                        var borderSize = Math.Min(maxBorder, childrenWidth * MaxBorderPercent);
                        var borderBox = new Rect(childrenX - borderSize, childrenY - borderSize, childrenWidth + 2 * borderSize, box.Height);
                        drawingContext.DrawRectangle(blackBrush, null, borderBox);
                    }

                    // store boxes in boxesMap to avoid multiple enumeration
                    flameBoxesMap.Add(box);
                }

                int index = 0;
                System.Drawing.Font forSize = null;
                foreach (var box in flameBoxesMap.EnumerateBoxes())
                {
                    var brushSet = Brushes[box.Node.InclusiveMetric < 0 ? 1 : 0]; // use second brush set (aqua theme) for negative metrics
                    var brush = brushSet[index++ % brushSet.Length];

                    var boxRectangle = new Rect(box.X, box.Y, box.Width, box.Height);
                    drawingContext.DrawRectangle(brush, null, boxRectangle);

                    if (box.Width > 50 && box.Height >= 6) // we draw the text only if humans can see something
                    {
                        if (forSize == null)
                        {
                            forSize = new System.Drawing.Font("Consolas", (float)box.Height, System.Drawing.GraphicsUnit.Pixel);
                        }

                        var text = new FormattedText(
                                box.Node.DisplayName,
                                CultureInfo.InvariantCulture,
                                FlowDirection.LeftToRight,
                                Typeface,
                                Math.Min(forSize.SizeInPoints, 20),
                                System.Windows.Media.Brushes.Black);

                        text.MaxTextWidth = box.Width;
                        text.MaxTextHeight = box.Height;

                        drawingContext.DrawText(text, new Point(box.X, box.Y));
                    }

                }

                AddVisual(visual);

                flameBoxesMap.Sort();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
#endif

        /// <summary>
        /// DrawingVisual provides no tooltip support, so I had to implement it myself.. I feel bad for it.
        /// </summary>
#if AVALONIA
        private void OnMouseMove(object sender, PointerEventArgs e)
#else
        private void OnMouseMove(object sender, MouseEventArgs e)
#endif
        {
#if AVALONIA
            if (!IsEmpty && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var position = InverseTransformPoint(e.GetPosition(this));
                var tooltipText = flameBoxesMap.Find(position).TooltipText;
                if (tooltipText != null)
                {
                    ShowTooltip(tooltipText);
                    CurrentFlameBoxChanged(this, tooltipText);
                    return;
                }
            }
            else if (!IsEmpty && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && IsZoomed)
            {
                var relativeMousePosition = InverseTransformPoint(e.GetPosition(this));
                MoveZoomingCenterPoint(relativeMousePosition.X, relativeMousePosition.Y);
            }
#else
            if (!IsEmpty && e.LeftButton == MouseButtonState.Released)
            {
                var position = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));
                var tooltipText = flameBoxesMap.Find(position).TooltipText;
                if (tooltipText != null)
                {
                    ShowTooltip(tooltipText);
                    CurrentFlameBoxChanged(this, tooltipText);
                    return;
                }
            }
            else if (!IsEmpty && e.LeftButton == MouseButtonState.Pressed && IsZoomed)
            {
                var relativeMousePosition = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));
                MoveZoomingCenterPoint(relativeMousePosition.X, relativeMousePosition.Y);
            }
#endif

            HideTooltip();
        }

#if AVALONIA
        private void OnMouseLeave(object sender, PointerEventArgs e)
#else
        private void OnMouseLeave(object sender, MouseEventArgs e)
#endif
        {
            HideTooltip();
            ResetCursor(); // leaving the control while still zooming and OnMouseLeftButtonUp won't fire
        }

#if AVALONIA
        private void OnPreviewMouseWheel(object sender, PointerWheelEventArgs e)
#else
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
#endif
        {
#if AVALONIA
            float modifier = e.Delta.Y > 0 ? 1.1f : 0.9f;

            var relativeMousePosition = InverseTransformPoint(e.GetPosition(this));

            scaleTransform.ScaleX = Math.Max(1.0, scaleTransform.ScaleX * modifier);
            scaleTransform.ScaleY = Math.Max(1.0, scaleTransform.ScaleY * modifier);
            _scaleCenterX = relativeMousePosition.X;
            _scaleCenterY = relativeMousePosition.Y;

            InvalidateVisual();
            Focus();
#else
            float modifier = e.Delta > 0 ? 1.1f : 0.9f;

            var relativeMousePosition = scaleTransform.Inverse.Transform(Mouse.GetPosition(this));

            scaleTransform.ScaleX = Math.Max(1.0, scaleTransform.ScaleX * modifier);
            scaleTransform.ScaleY = Math.Max(1.0, scaleTransform.ScaleY * modifier);
            scaleTransform.CenterX = relativeMousePosition.X;
            scaleTransform.CenterY = relativeMousePosition.Y;

            this.Draw(flameBoxesMap.EnumerateBoxes().ToList()); // redraw canvas with new scale
            Keyboard.Focus(this); // make it possible to handle Arrow keys and move CenterX & Y scaling points
#endif
        }

#if AVALONIA
        private void OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e)
#else
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
#endif
        {
            if (IsZoomed)
            {
                cursor = Mouse.OverrideCursor;
#if !AVALONIA
                Mouse.OverrideCursor = Cursors.Hand; // emulate drag&drop cursor style
#else
                Mouse.OverrideCursor = new Cursor(StandardCursorType.Hand);
#endif
            }
        }

#if AVALONIA
        private void OnMouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
#else
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
#endif
        {
            if (IsZoomed)
            {
                ResetCursor();
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsZoomed)
            {
                return;
            }

#if AVALONIA
            switch (e.Key)
            {
                case Key.Left:
                    MoveZoomingCenterPoint(_scaleCenterX * 0.9, _scaleCenterY);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveZoomingCenterPoint(_scaleCenterX * 1.1, _scaleCenterY);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveZoomingCenterPoint(_scaleCenterX, _scaleCenterY * 0.9);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveZoomingCenterPoint(_scaleCenterX, _scaleCenterY * 1.1);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
#else
            switch (e.Key)
            {
                case Key.Left:
                    MoveZoomingCenterPoint(scaleTransform.CenterX * 0.9, scaleTransform.CenterY);
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveZoomingCenterPoint(scaleTransform.CenterX * 1.1, scaleTransform.CenterY);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveZoomingCenterPoint(scaleTransform.CenterX, scaleTransform.CenterY * 0.9);
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveZoomingCenterPoint(scaleTransform.CenterX, scaleTransform.CenterY * 1.1);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
#endif
        }

        private void ShowTooltip(string text)
        {
#if AVALONIA
            ToolTip.SetTip(this, text);
            ToolTip.SetIsOpen(this, true);
#else
            if (object.ReferenceEquals(tooltip.Content, text) && tooltip.IsOpen)
            {
                return;
            }

            tooltip.IsOpen = false; // by closing and opening it again we restart it's position to the current mouse position..
            tooltip.Content = text;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            tooltip.IsOpen = true;
            tooltip.PlacementTarget = this;
#endif
        }

#if AVALONIA
        private void HideTooltip() => ToolTip.SetIsOpen(this, false);
#else
        private void HideTooltip() => tooltip.IsOpen = false;
#endif

        private void Clear()
        {
#if !AVALONIA
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                DeleteVisual(visuals[i]);
                visuals.RemoveAt(i);
            }
#else
            visuals.Clear();
#endif

            flameBoxesMap.Clear();
        }

#if !AVALONIA
        private void AddVisual(Visual visual)
        {
            visuals.Add(visual);

            base.AddVisualChild(visual);
            base.AddLogicalChild(visual);
        }

        private void DeleteVisual(Visual visual)
        {
            base.RemoveVisualChild(visual);
            base.RemoveLogicalChild(visual);
        }
#endif

        private void MoveZoomingCenterPoint(double x, double y)
        {
            if (IsZoomed)
            {
#if AVALONIA
                _scaleCenterX = Math.Min(x, Bounds.Width);
                _scaleCenterY = Math.Min(y, Bounds.Height);
#else
                scaleTransform.CenterX = Math.Min(x, ActualWidth);
                scaleTransform.CenterY = Math.Min(y, ActualHeight);
#endif
            }
        }

        private void ResetCursor() => Mouse.OverrideCursor = cursor;

#if AVALONIA
        private Point InverseTransformPoint(Point point)
        {
            return new Point(
                _scaleCenterX + (point.X - _scaleCenterX) / scaleTransform.ScaleX,
                _scaleCenterY + (point.Y - _scaleCenterY) / scaleTransform.ScaleY);
        }
#endif

        private static Brush[][] GenerateBrushes()
        {
            var brushes = new Brush[][]
            {
                Enumerable.Range(0, 100)
                    .Select(_ => (Brush)new SolidColorBrush(
                        CreateRandomColor(205, 255, 0, 230, 0, 55)))
                    .ToArray(),
                Enumerable.Range(0, 100)
                    .Select(_ => (Brush)new SolidColorBrush(
                        CreateRandomColor(50, 110, 165, 220, 165, 220)))
                    .ToArray()
            };

            foreach (var brushArray in brushes)
            {
                foreach (var brush in brushArray)
                {
                    brush.Freeze(); // this is crucial for performance
                }
            }

            return brushes;
        }

        private static Color CreateRandomColor(byte r1, byte r2, byte g1, byte g2, byte b1, byte b2, double contrastThreshold = 4.5)
        {
            while (true)
            {
                var r = (byte)(r1 + RandomNumberGenerator.GetDouble() * (r2 - r1 + 1));
                var g = (byte)(g1 + RandomNumberGenerator.GetDouble() * (g2 - g1 + 1));
                var b = (byte)(b1 + RandomNumberGenerator.GetDouble() * (b2 - b1 + 1));

                var color = Color.FromRgb(r, g, b);
                var contrastWithBlack = CalculateContrastWithBlack(color);
                if (contrastWithBlack > contrastThreshold)
                {
                    return color;
                }
            }
        }

        private static double CalculateContrastWithBlack(Color color)
        {
            var luminance = CalculateLuminance(color);
            return (luminance + 0.05) / 0.05;
        }

        private static double CalculateLuminance(Color color)
        {
            // Normalize RGB values to [0,1]
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            // Apply gamma correction
            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            // Calculate luminance
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private class FlameBoxesMap
        {
            private SortedDictionary<Range, List<FlameBox>> boxesMap = new SortedDictionary<Range, List<FlameBox>>();

            internal IEnumerable<FlameBox> EnumerateBoxes() => boxesMap.Values.SelectMany(x => x);

            internal void Clear() => boxesMap.Clear();

            internal void Add(FlameBox flameBox)
            {
                var row = new Range(flameBox.Y, flameBox.Y + flameBox.Height);

                if (!boxesMap.TryGetValue(row, out var list))
                {
                    boxesMap.Add(row, list = new List<FlameBox>());
                }

                list.Add(flameBox);
            }

            internal void Sort()
            {
                foreach (var row in boxesMap.Values)
                {
                    row.Sort(CompareByX); // sort the boxes from left to the right
                }
            }

            internal FlameBox Find(Point point)
            {
                foreach (var rowData in boxesMap)
                {
                    if (rowData.Key.Contains(point.Y))
                    {
                        int low = 0, high = rowData.Value.Count - 1, mid = 0;

                        while (low <= high)
                        {
                            mid = (low + high) / 2;

                            if (rowData.Value[mid].X > point.X)
                            {
                                high = mid - 1;
                            }
                            else if ((rowData.Value[mid].X + rowData.Value[mid].Width) < point.X)
                            {
                                low = mid + 1;
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (rowData.Value[mid].X <= point.X && point.X <= (rowData.Value[mid].X + rowData.Value[mid].Width))
                        {
                            return rowData.Value[mid];
                        }

                        return default(FlameBox);
                    }
                }

                return default(FlameBox);
            }

            private static int CompareByX(FlameBox left, FlameBox right) => left.X.CompareTo(right.X);

            private struct Range : IEquatable<Range>, IComparable<Range>
            {
                private readonly double Start, End;

                internal Range(double start, double end)
                {
                    Start = start;
                    End = end;
                }

                internal bool Contains(double y) => Start <= y && y <= End;

                public override bool Equals(object obj) => throw new InvalidOperationException("No boxing");

                public bool Equals(Range other) => other.Start == Start && other.End == End;

                public int CompareTo(Range other) => other.Start.CompareTo(Start);

                public override int GetHashCode() => (Start * End).GetHashCode();
            }
        }
    }
}
