using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Astra.Models;

namespace Astra.Views
{
    /// <summary>
    /// Hand-rolled area/line chart over WPF primitives (Polyline for the line, Polygon for the
    /// translucent fill) - no charting library is referenced anywhere in this project, and net472
    /// rules out LiveCharts2 (needs net6+); adding LiveCharts v1 would be a new dependency risk given
    /// this plugin's documented history of assembly-version collisions inside Playnite's shared
    /// process (see CLAUDE.md). This control only knows about TrendResult/TrendPoint - never
    /// AstraDatabase/SessionTracker/IPlayniteAPI - so the same instance serves Home's mini-chart and
    /// the Trends page's full chart.
    /// </summary>
    public partial class TrendChartControl : UserControl
    {
        public static readonly DependencyProperty TrendProperty = DependencyProperty.Register(
            nameof(Trend), typeof(TrendResult), typeof(TrendChartControl),
            new PropertyMetadata(null, OnRedrawPropertyChanged));

        public TrendResult Trend
        {
            get => (TrendResult)GetValue(TrendProperty);
            set => SetValue(TrendProperty, value);
        }

        public static readonly DependencyProperty CompactProperty = DependencyProperty.Register(
            nameof(Compact), typeof(bool), typeof(TrendChartControl),
            new PropertyMetadata(false, OnRedrawPropertyChanged));

        /// <summary>Smaller/quieter styling for Home's mini-chart: no axis labels, thinner line.</summary>
        public bool Compact
        {
            get => (bool)GetValue(CompactProperty);
            set => SetValue(CompactProperty, value);
        }

        private static void OnRedrawPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TrendChartControl)d).Redraw();
        }

        public TrendChartControl()
        {
            InitializeComponent();
            // Wired here rather than as a XAML SizeChanged="..." attribute on the root element: that
            // attribute form makes the XAML markup compiler generate a fully-qualified cast to
            // Astra.Views.TrendChartControl in Connect(), and "Astra" resolves to the plugin's own
            // GenericPlugin class (Astra.Astra) before the namespace, since both share the name -
            // CS0426 "'Views' does not exist in type 'Astra'". Attaching the handler here sidesteps
            // that resolution entirely.
            Loaded += (s, e) => Redraw();
            SizeChanged += (s, e) => Redraw();
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            TooltipBorder.Visibility = Visibility.Collapsed;
        }

        private void Redraw()
        {
            ChartCanvas.Children.Clear();
            TooltipBorder.Visibility = Visibility.Collapsed;

            var trend = Trend;
            var width = ChartCanvas.ActualWidth;
            var height = ChartCanvas.ActualHeight;

            if (trend == null || trend.Points.Count == 0 || width <= 0 || height <= 0)
            {
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (!trend.HasActivity)
            {
                EmptyStatePanel.Visibility = Visibility.Visible;
                return;
            }

            EmptyStatePanel.Visibility = Visibility.Collapsed;

            var points = trend.Points;
            var axisLabelHeight = Compact ? 0.0 : 18.0;
            var plotHeight = Math.Max(1.0, height - axisLabelHeight);
            var maxSeconds = Math.Max(1L, points.Max(p => p.PlaytimeSeconds));
            var stepX = points.Count > 1 ? width / (points.Count - 1) : width;

            var accent = TryFindResource("GlyphBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x3E, 0xC6, 0xE0));
            var gridlineBrush = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80));

            ChartCanvas.Children.Add(new Line
            {
                X1 = 0,
                Y1 = plotHeight - 0.5,
                X2 = width,
                Y2 = plotHeight - 0.5,
                Stroke = gridlineBrush,
                StrokeThickness = 1
            });

            var linePoints = new PointCollection();
            for (var i = 0; i < points.Count; i++)
            {
                var x = points.Count > 1 ? i * stepX : width / 2;
                var y = plotHeight - (points[i].PlaytimeSeconds / (double)maxSeconds * plotHeight);
                linePoints.Add(new Point(x, y));
            }

            var areaPoints = new PointCollection(linePoints);
            areaPoints.Add(new Point(width, plotHeight));
            areaPoints.Add(new Point(0, plotHeight));

            ChartCanvas.Children.Add(new Polygon
            {
                Points = areaPoints,
                Fill = accent,
                Opacity = 0.16
            });

            ChartCanvas.Children.Add(new Polyline
            {
                Points = linePoints,
                Stroke = accent,
                StrokeThickness = Compact ? 1.5 : 2,
                StrokeLineJoin = PenLineJoin.Round
            });

            if (!Compact)
            {
                DrawAxisLabels(points, width, plotHeight, stepX);
            }

            AddHoverStrips(points, width, plotHeight, stepX);
        }

        private void DrawAxisLabels(IList<TrendPoint> points, double width, double plotHeight, double stepX)
        {
            // Thin out labels on long ranges (e.g. 365 daily points) so they don't overlap -
            // roughly 10 evenly spaced labels regardless of point count.
            var labelEvery = Math.Max(1, points.Count / 10);

            for (var i = 0; i < points.Count; i += labelEvery)
            {
                var label = new TextBlock
                {
                    Text = points[i].Label,
                    FontSize = 10,
                    Opacity = 0.55
                };
                var x = points.Count > 1 ? i * stepX : width / 2;
                Canvas.SetLeft(label, Math.Max(0, Math.Min(Math.Max(0, width - 28), x - 14)));
                Canvas.SetTop(label, plotHeight + 2);
                ChartCanvas.Children.Add(label);
            }
        }

        private void AddHoverStrips(IList<TrendPoint> points, double width, double plotHeight, double stepX)
        {
            // One invisible hit-test rectangle per point so hovering anywhere across a point's slice
            // of width shows its exact tooltip, rather than requiring a pixel-perfect hover on the
            // line/area itself.
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                var x = points.Count > 1 ? i * stepX : width / 2;
                var hitWidth = points.Count > 1 ? stepX : width;

                var strip = new Rectangle
                {
                    Width = Math.Max(1, hitWidth),
                    Height = plotHeight,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(strip, x - hitWidth / 2);
                Canvas.SetTop(strip, 0);
                strip.MouseMove += (s, e) => ShowTooltip(point, e.GetPosition(this));
                ChartCanvas.Children.Add(strip);
            }
        }

        private void ShowTooltip(TrendPoint point, Point controlPosition)
        {
            TooltipTitle.Text = TooltipTitleFor(point);
            TooltipValue.Text = FormatPlaytime(point.PlaytimeSeconds) + " played";
            TooltipBorder.Visibility = Visibility.Visible;

            var left = Math.Min(controlPosition.X + 12, Math.Max(0, ActualWidth - 160));
            TooltipBorder.Margin = new Thickness(left, 4, 0, 0);
        }

        private string TooltipTitleFor(TrendPoint point)
        {
            switch (Trend?.Granularity)
            {
                case TrendGranularity.Hour:
                    return point.PeriodStart.ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
                case TrendGranularity.Day:
                    return point.PeriodStart.ToString("MMMM d", CultureInfo.InvariantCulture);
                case TrendGranularity.Week:
                    return "Week of " + point.PeriodStart.ToString("d MMM", CultureInfo.InvariantCulture);
                case TrendGranularity.Month:
                    return point.PeriodStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                case TrendGranularity.Year:
                    return point.PeriodStart.Year.ToString(CultureInfo.InvariantCulture);
                default:
                    return point.Label;
            }
        }

        private static string FormatPlaytime(long seconds)
        {
            var hours = seconds / 3600;
            var minutes = (seconds % 3600) / 60;
            if (hours <= 0)
            {
                return minutes > 0 ? $"{minutes}m" : "0m";
            }
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }
    }
}
