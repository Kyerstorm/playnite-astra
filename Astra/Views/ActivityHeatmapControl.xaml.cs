using System;
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
    /// GitHub-contributions-style calendar heatmap: one cell per calendar day, columns are Monday-start
    /// ISO weeks, rows are Monday(0)-Sunday(6) - same week convention as everywhere else in Astra (see
    /// TrendAggregationService.StartOfWeek). Hand-drawn over a Canvas like TrendChartControl, for the
    /// same reason: no charting library is referenced anywhere in this net472 project. Only depends on
    /// TrendResult/TrendPoint, so it never needs to know about games or the database directly.
    /// </summary>
    public partial class ActivityHeatmapControl : UserControl
    {
        public static readonly DependencyProperty TrendProperty = DependencyProperty.Register(
            nameof(Trend), typeof(TrendResult), typeof(ActivityHeatmapControl),
            new PropertyMetadata(null, OnRedrawPropertyChanged));

        /// <summary>Expected to be a Day-granularity TrendResult spanning one full calendar year
        /// (TrendsViewModel.HeatmapTrend) - the control doesn't enforce this, but the week/row math
        /// assumes every point is exactly one calendar day.</summary>
        public TrendResult Trend
        {
            get => (TrendResult)GetValue(TrendProperty);
            set => SetValue(TrendProperty, value);
        }

        private static void OnRedrawPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ActivityHeatmapControl)d).Redraw();
        }

        // Five discrete intensity steps (like GitHub's) rather than continuous opacity, so the
        // difference between a light day and a heavy day stays visually obvious - a continuous
        // scale washes out into indistinguishable pale cells for most real playtime distributions.
        private static readonly double[] IntensitySteps = { 0.10, 0.30, 0.50, 0.70, 0.90, 1.0 };

        public ActivityHeatmapControl()
        {
            InitializeComponent();
            Loaded += (s, e) => Redraw();
            SizeChanged += (s, e) => Redraw();
        }

        private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
        {
            TooltipBorder.Visibility = Visibility.Collapsed;
        }

        private void Redraw()
        {
            HeatmapCanvas.Children.Clear();
            TooltipBorder.Visibility = Visibility.Collapsed;

            var trend = Trend;
            var width = HeatmapCanvas.ActualWidth;
            var height = HeatmapCanvas.ActualHeight;

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
            var firstDay = points[0].PeriodStart;
            var gridStart = TrendAggregationServiceWeekStart(firstDay);
            var lastDay = points[points.Count - 1].PeriodStart;
            var totalWeeks = ((TrendAggregationServiceWeekStart(lastDay) - gridStart).Days / 7) + 1;

            const double gap = 3;
            var cellSize = Math.Max(4, Math.Min((width - (totalWeeks - 1) * gap) / totalWeeks, (height - 6 * gap) / 7));

            var maxSeconds = Math.Max(1L, points.Max(p => p.PlaytimeSeconds));
            var accent = TryFindResource("GlyphBrush") as Brush ?? new SolidColorBrush(Color.FromRgb(0x3E, 0xC6, 0xE0));
            var emptyBrush = new SolidColorBrush(Color.FromArgb(0x28, 0x80, 0x80, 0x80));

            foreach (var point in points)
            {
                var dayIndex = (point.PeriodStart - gridStart).Days;
                var column = dayIndex / 7;
                var row = dayIndex % 7; // Monday-aligned gridStart guarantees row 0 = Monday

                var x = column * (cellSize + gap);
                var y = row * (cellSize + gap);

                var brush = point.PlaytimeSeconds > 0 ? IntensityBrush(accent, point.PlaytimeSeconds, maxSeconds) : emptyBrush;

                var cell = new Rectangle
                {
                    Width = cellSize,
                    Height = cellSize,
                    Fill = brush,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(cell, x);
                Canvas.SetTop(cell, y);
                cell.MouseMove += (s, e) => ShowTooltip(point, e.GetPosition(this));
                HeatmapCanvas.Children.Add(cell);
            }
        }

        private static Brush IntensityBrush(Brush accent, long seconds, long maxSeconds)
        {
            var fraction = seconds / (double)maxSeconds;
            var step = IntensitySteps.FirstOrDefault(s => fraction <= s);
            if (step <= 0)
            {
                step = IntensitySteps[IntensitySteps.Length - 1];
            }

            if (accent is SolidColorBrush solid)
            {
                var color = solid.Color;
                return new SolidColorBrush(Color.FromArgb((byte)(step * 255), color.R, color.G, color.B));
            }

            return accent;
        }

        /// <summary>Same Monday-start convention as TrendAggregationService.StartOfWeek, duplicated
        /// here (rather than made internal-and-shared) since this control intentionally has zero
        /// dependency on the Services namespace - it only ever touches Models types.</summary>
        private static DateTime TrendAggregationServiceWeekStart(DateTime date)
        {
            var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.AddDays(-diff);
        }

        private void ShowTooltip(TrendPoint point, Point controlPosition)
        {
            TooltipTitle.Text = point.PeriodStart.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

            if (point.PlaytimeSeconds > 0)
            {
                TooltipPlaytime.Text = FormatPlaytime(point.PlaytimeSeconds) + " played";
                TooltipDetail.Text = $"{point.SessionCount} session{(point.SessionCount == 1 ? "" : "s")} - " +
                                      $"{point.ActiveGameCount} game{(point.ActiveGameCount == 1 ? "" : "s")}";
            }
            else
            {
                TooltipPlaytime.Text = "0h";
                TooltipDetail.Text = "No sessions";
            }

            TooltipBorder.Visibility = Visibility.Visible;
            var left = Math.Min(controlPosition.X + 12, Math.Max(0, ActualWidth - 170));
            var top = Math.Min(controlPosition.Y + 12, Math.Max(0, ActualHeight - 60));
            TooltipBorder.Margin = new Thickness(left, top, 0, 0);
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
