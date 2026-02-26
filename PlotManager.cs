using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System.Drawing;
using System.Linq;
using Color = System.Drawing.Color;

namespace PABReaderGraph
{
    /// <summary>
    /// Manages all ScottPlot operations for GraphForm to separate plotting concerns
    /// Handles real-time plot updates, session summaries, and zero marker management
    /// Provides thread-safe plot operations and performance-optimized rendering
    /// 
    /// Responsibilities:
    /// - Real-time plot creation and updates for live data visualization
    /// - Session summary plot generation for post-analysis review
    /// - Zero calibration marker placement and styling
    /// - Axis scaling and visibility management
    /// - Plot styling and appearance configuration
    /// </summary>
    public class PlotManager
    {
        private readonly FormsPlot _formsPlot;
        private readonly int _portId;
        private readonly object _plotLock = new();
        private readonly List<VerticalLine> _zeroMarkers;

        /// <summary>
        /// Initializes a new PlotManager instance for a specific port
        /// Sets up plot styling and prepares for real-time data visualization
        /// </summary>
        /// <param name="formsPlot">ScottPlot FormsPlot control for rendering</param>
        /// <param name="portId">Zero-based port identifier for labeling and context</param>
        /// <param name="zeroMarkers">Reference to zero markers collection for marker management</param>
        public PlotManager(FormsPlot formsPlot, int portId, List<VerticalLine> zeroMarkers)
        {
            _formsPlot = formsPlot;
            _portId = portId;
            _zeroMarkers = zeroMarkers;
            
            SetupPlotStyling();
        }

        /// <summary>
        /// Configures initial plot styling and appearance for live data display
        /// Sets up legend, grid, colors, and title with consistent visual theme
        /// Called once during PlotManager initialization for baseline styling
        /// </summary>
        private void SetupPlotStyling()
        {
            _formsPlot.Plot.ShowLegend();
            _formsPlot.Plot.Legend.Alignment = Alignment.UpperRight;
            _formsPlot.Plot.Title($"Port {_portId + 1} Live Data");
            _formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromSDColor(Color.LightGray);
            _formsPlot.Plot.Grid.MajorLinePattern = ScottPlot.LinePattern.Dotted;
            _formsPlot.Plot.FigureBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };
            _formsPlot.Plot.DataBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };
        }

        /// <summary>
        /// Updates live plots with new data in a thread-safe manner
        /// Clears existing plots and recreates them with current data and visibility settings
        /// Optimized for real-time performance with minimal UI thread blocking
        /// </summary>
        /// <param name="data">Pre-cached plot data arrays for efficient rendering</param>
        /// <param name="lineVisibility">Boolean array indicating which data series should be visible</param>
        public void UpdatePlots(CachedPlotData data, bool[] lineVisibility)
        {
            lock (_plotLock)
            {
                _formsPlot.Plot.Clear<Scatter>();

                var plots = CreateScatterPlots(data);
                ApplyVisibility(plots, lineVisibility);
                UpdateAxisLimits(data, lineVisibility);

                _formsPlot.Refresh();
            }
        }

        /// <summary>
        /// Creates scatter plot objects for all data series (LC1-LC4 + Total)
        /// Applies consistent styling and color schemes from UIConstants
        /// Optimized for performance with direct array access and minimal object creation
        /// </summary>
        /// <param name="data">Cached plot data containing all time series arrays</param>
        /// <returns>Array of 5 configured Scatter plot objects ready for rendering</returns>
        private Scatter[] CreateScatterPlots(CachedPlotData data)
        {
            var liveColors = UIConstants.Colors.GetLiveColors();
            var plots = new Scatter[5];

            // Create LC plots
            for (int i = 0; i < 4; i++)
            {
                double[] lcData = data.GetLCData((LineIndex)i);

                plots[i] = _formsPlot.Plot.Add.Scatter(data.Times, lcData);
                plots[i].LegendText = $"LC{i + 1}";
                plots[i].MarkerSize = UIConstants.Plot.MarkerSize;
                plots[i].Color = ScottPlot.Color.FromSDColor(liveColors[i]);
            }

            // Create Total plot
            plots[4] = _formsPlot.Plot.Add.Scatter(data.Times, data.Total);
            plots[4].LegendText = "Total";
            plots[4].Color = ScottPlot.Color.FromSDColor(liveColors[4]);
            plots[4].LineWidth = UIConstants.Plot.TotalLineWidth;
            plots[4].MarkerSize = UIConstants.Plot.TotalMarkerSize;

            return plots;
        }

        /// <summary>
        /// Applies user-controlled visibility settings to plot series
        /// Enables dynamic showing/hiding of individual load cell data and total weight
        /// Supports real-time filtering without recreating plot objects
        /// </summary>
        /// <param name="plots">Array of scatter plot objects to modify</param>
        /// <param name="lineVisibility">Boolean array controlling visibility for each series</param>
        private void ApplyVisibility(Scatter[] plots, bool[] lineVisibility)
        {
            for (int i = 0; i < plots.Length && i < lineVisibility.Length; i++)
            {
                plots[i].IsVisible = lineVisibility[i];
            }
        }

        /// <summary>
        /// Updates axis limits based on visible data for optimal viewing
        /// X-axis follows a sliding time window, Y-axis scales to visible data range
        /// Includes intelligent padding and handles edge cases like flat lines or no visible data
        /// </summary>
        /// <param name="data">Cached plot data for range calculations</param>
        /// <param name="lineVisibility">Visibility settings to determine which data affects scaling</param>
        private void UpdateAxisLimits(CachedPlotData data, bool[] lineVisibility)
        {
            double latestTime = data.Times.Last();
            _formsPlot.Plot.Axes.SetLimitsX(latestTime - UIConstants.Plot.DisplaySeconds, latestTime);

            // Manually calculate Y limits based only on visible lines
            var visibleValues = new List<double>();
            
            if (lineVisibility[(int)LineIndex.LC1]) visibleValues.AddRange(data.LC1);
            if (lineVisibility[(int)LineIndex.LC2]) visibleValues.AddRange(data.LC2);
            if (lineVisibility[(int)LineIndex.LC3]) visibleValues.AddRange(data.LC3);
            if (lineVisibility[(int)LineIndex.LC4]) visibleValues.AddRange(data.LC4);
            if (lineVisibility[(int)LineIndex.Total]) visibleValues.AddRange(data.Total);

            if (visibleValues.Count > 0)
            {
                double minY = visibleValues.Min();
                double maxY = visibleValues.Max();
                double padding = (maxY - minY) * UIConstants.Plot.YPaddingPercent;
                if (padding == 0) padding = Math.Abs(maxY) * UIConstants.Plot.YPaddingPercent + 1; // Handle flat lines
                _formsPlot.Plot.Axes.SetLimitsY(minY - padding, maxY + padding);
            }
            else
            {
                _formsPlot.Plot.Axes.AutoScaleY(); // Fallback if no lines visible
            }
        }

        /// <summary>
        /// Adds a zero calibration marker to the plot at the specified time
        /// Creates a red dashed vertical line with time label for calibration reference
        /// Thread-safe operation that updates both plot and marker collection
        /// </summary>
        /// <param name="time">Time value where the zero calibration marker should be placed</param>
        public void AddZeroMarker(double time)
        {
            lock (_plotLock)
            {
                var vline = _formsPlot.Plot.Add.VerticalLine(time);
                vline.Color = ScottPlot.Color.FromSDColor(UIConstants.Colors.ZeroMarker);
                vline.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                vline.LabelText = $"{time:F2}";
                _zeroMarkers.Add(vline);
            }
        }

        /// <summary>
        /// Creates a comprehensive session summary plot with complete data visualization
        /// Static method for generating post-session analysis plots with statistics
        /// Includes all data series, zero markers, and session metadata annotations
        /// Used for session review dialogs and exported graph files
        /// </summary>
        /// <param name="plot">ScottPlot.Plot instance to configure</param>
        /// <param name="recordedData">Complete session data collection</param>
        /// <param name="zeroMarkers">Zero calibration markers to display</param>
        /// <param name="portId">Port identifier for plot labeling</param>
        public static void AddSessionPlot(
            ScottPlot.Plot plot,
            List<(double time, double[] values)> recordedData,
            List<VerticalLine> zeroMarkers,
            int portId)
        {
            var times = recordedData.Select(dp => dp.time).ToArray();
            var lc1 = recordedData.Select(dp => dp.values[0]).ToArray();
            var lc2 = recordedData.Select(dp => dp.values[1]).ToArray();
            var lc3 = recordedData.Select(dp => dp.values[2]).ToArray();
            var lc4 = recordedData.Select(dp => dp.values[3]).ToArray();
            var total = recordedData.Select(dp => dp.values.Sum()).ToArray();

            plot.Clear();

            var liveColors = UIConstants.Colors.GetLiveColors();

            // Create all scatter plots
            var plots = new[]
            {
                CreateSessionScatterPlot(plot, times, lc1, "LC1", liveColors[0]),
                CreateSessionScatterPlot(plot, times, lc2, "LC2", liveColors[1]),
                CreateSessionScatterPlot(plot, times, lc3, "LC3", liveColors[2]),
                CreateSessionScatterPlot(plot, times, lc4, "LC4", liveColors[3]),
                CreateSessionScatterPlot(plot, times, total, "Total", liveColors[4], UIConstants.Plot.TotalLineWidth, UIConstants.Plot.TotalMarkerSize)
            };

            // Add zero markers
            foreach (var marker in zeroMarkers)
            {
                var vline = plot.Add.VerticalLine(marker.X);
                vline.Color = ScottPlot.Color.FromSDColor(UIConstants.Colors.ZeroMarker);
                vline.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                vline.LabelText = $"{marker.X:F2}";
            }

            // Configure plot appearance
            ConfigureSessionPlotAppearance(plot, portId, recordedData);
        }

        /// <summary>
        /// Creates a single scatter plot for session summary with customizable styling
        /// Helper method for consistent plot creation across all data series
        /// Supports variable line width and marker size for emphasis (e.g., Total weight)
        /// </summary>
        /// <param name="plot">Plot instance to add the scatter plot to</param>
        /// <param name="times">X-axis time values for the data series</param>
        /// <param name="values">Y-axis measurement values for the data series</param>
        /// <param name="legendText">Display name for the legend entry</param>
        /// <param name="color">Color for the plot line and markers</param>
        /// <param name="lineWidth">Line thickness (default: 1)</param>
        /// <param name="markerSize">Marker size (default: from UIConstants)</param>
        /// <returns>Configured Scatter plot object</returns>
        private static Scatter CreateSessionScatterPlot(
            ScottPlot.Plot plot, 
            double[] times, 
            double[] values, 
            string legendText, 
            Color color,
            float lineWidth = 1,
            float markerSize = UIConstants.Plot.MarkerSize)
        {
            var scatter = plot.Add.Scatter(times, values);
            scatter.LegendText = legendText;
            scatter.MarkerSize = markerSize;
            scatter.Color = ScottPlot.Color.FromSDColor(color);
            if (lineWidth > 1) scatter.LineWidth = lineWidth;
            return scatter;
        }

        /// <summary>
        /// Configures session plot appearance with styling and statistical annotations
        /// Applies consistent visual theme and adds session summary information
        /// Includes calculated statistics like duration, record count, and average rate
        /// </summary>
        /// <param name="plot">Plot instance to configure</param>
        /// <param name="portId">Port identifier for title generation</param>
        /// <param name="recordedData">Session data for statistical calculations</param>
        private static void ConfigureSessionPlotAppearance(
            ScottPlot.Plot plot, 
            int portId, 
            List<(double time, double[] values)> recordedData)
        {
            plot.Title($"Port {portId + 1} Data");
            plot.ShowLegend();
            plot.Legend.Alignment = Alignment.UpperRight;
            plot.Grid.MajorLineColor = ScottPlot.Color.FromSDColor(Color.LightGray);
            plot.Grid.MajorLinePattern = ScottPlot.LinePattern.Dotted;
            plot.FigureBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };
            plot.DataBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };

            // Add session statistics
            int totalRecords = recordedData.Count;
            double durationSeconds = totalRecords > 1 ? (recordedData.Last().time - recordedData.First().time) : 0.0;
            double avgRate = durationSeconds > 0 ? totalRecords / durationSeconds : 0.0;

            var summaryText = $"Records: {totalRecords}\nDuration: {durationSeconds:F1} sec\nRate: {avgRate:F1} rec/s";
            var annotation = plot.Add.Annotation(summaryText, ScottPlot.Alignment.UpperCenter);
            annotation.LabelFontSize = 14;
            annotation.LabelFontColor = ScottPlot.Color.FromSDColor(Color.DarkSlateGray);
            annotation.LabelBackgroundColor = ScottPlot.Color.FromSDColor(Color.FromArgb(230, Color.White));
        }
    }
}