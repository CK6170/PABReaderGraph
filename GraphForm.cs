using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using Color = System.Drawing.Color;
using Label = System.Windows.Forms.Label;
using FontStyle = System.Drawing.FontStyle;
using System.Media;


namespace PABReaderGraph
{
    /// <summary>
    /// Main form for displaying live data from PAB (Portable Analysis Board) load cell measurements
    /// Provides real-time graphing, data logging, session management, and calibration functionality
    /// 
    /// Architecture:
    /// - UI construction delegated to GraphFormUIBuilder
    /// - Plot operations handled by PlotManager  
    /// - Session data management handled by SessionDataManager
    /// - Styling and configuration centralized in UIConstants
    /// 
    /// Features:
    /// - Real-time plotting of 4 load cells + total weight
    /// - Zero calibration with visual markers
    /// - Data filtering and line visibility controls
    /// - Session data export (JSON, CSV, PNG)
    /// - Keyboard shortcuts for common operations
    /// - Window state persistence between sessions
    /// </summary>
    public partial class GraphForm : Form
    {
        // ==== UI Components (exposed for UIBuilder) ====
        public ScottPlot.WinForms.FormsPlot formsPlot;
        public Panel panelTop;
        public Panel panelBottom;
        public Panel labelValuesPanel;
        public Button buttonZero;
        public Button buttonRestart;
        public Button buttonExit;
        public Button buttonExitAll;
        public Label labelStatus;
        public Label labelRecord;
        public Label labelElapsed;
        public Label labelLC1, labelLC2, labelLC3, labelLC4, labelTotal;
        private int portId;
        private Settings globalSettings;
        private WindowSettings localWindowSettings;
        private readonly List<(double time, double[] values)> dataPoints = new();
        private readonly double displaySeconds = UIConstants.Plot.DisplaySeconds;
        private readonly System.Windows.Forms.Timer updateTimer;
        private List<(double time, double[] values)> recordedData = new();
        private List<VerticalLine> zeroMarkers = new();
        private DateTime lastRateUpdate = DateTime.Now;
        private int recordCountSinceLastRate = 0;
        private int totalRecordCount = 0;
        private double currentRate = 0.0;
        private uint[] currentAdcValues = new uint[4];
        private const int rateUpdateIntervalSeconds = 10;
        private System.Windows.Forms.Timer? calibrationCountdownTimer;

        // Individual colored labels for ADC, Zero, and Factor values (exposed for UIBuilder)
        public Label labelAdcLC1, labelAdcLC2, labelAdcLC3, labelAdcLC4, labelAdcTotal;
        public Label labelZeroLC1, labelZeroLC2, labelZeroLC3, labelZeroLC4, labelZeroTotal, labelZeroHeaders, labelZeroElapsed;
        public Label labelFactorLC1, labelFactorLC2, labelFactorLC3, labelFactorLC4, labelFactorTotal;

        // Runtime filter control for toggling graph lines (exposed for UIBuilder)
        public CheckedListBox filterBox;

        // ==== Instance Data ====
        private bool[] lineVisibility = new bool[5] { true, true, true, true, true }; // LC1, LC2, LC3, LC4, Total

        // Performance optimization - cached plot data
        private CachedPlotData? _cachedArrays;
        private bool _needsArrayUpdate = true;
        private bool _needsPlotUpdate = true;

        // Plot management
        private PlotManager _plotManager;

        // Session data management
        private SessionDataManager _sessionManager;


        // ==== Summary window management ====
        private static List<Form> openSummaryForms = new();

        // ==== Constructor & Setup ====
        public GraphForm(int id, Settings settings)
        { 
            InitializeComponent();
            this.KeyPreview = true;

            FormBorderStyle = FormBorderStyle.Sizable;
            portId = id;
            globalSettings = settings;
            localWindowSettings = settings.WindowSettingsPerId.TryGetValue(portId, out var ws)
                ? new WindowSettings { Width = ws.Width, Height = ws.Height, Left = ws.Left, Top = ws.Top, Maximized = ws.Maximized }
                : new WindowSettings { Width = 800, Height = 600, Left = 100 + portId * 50, Top = 100 + portId * 50, Maximized = false };

            StartPosition = FormStartPosition.Manual;
            Size = new Size(localWindowSettings.Width, localWindowSettings.Height);
            Location = new Point(localWindowSettings.Left, localWindowSettings.Top);
            WindowState = localWindowSettings.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            Text = $"PAB Port {id + 1} Monitor";

            SetupLayout();

            // Initialize plot manager after UI setup
            _plotManager = new PlotManager(formsPlot, portId, zeroMarkers);

            // Initialize session data manager
            _sessionManager = new SessionDataManager();

            updateTimer = new System.Windows.Forms.Timer { Interval = UIConstants.Plot.UpdateInterval };
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            FormClosing += GraphForm_FormClosing;
            SerialManager.Instance.OnDataReceived += Instance_OnDataReceived;
            SerialManager.Instance.CalibrationCompleted += Instance_CalibrationCompleted;

            // Register this form with SerialManager so it can receive zero calibration updates
            SerialManager.Instance.RegisterGraphForm((ushort)portId, this);

            // Setup event handlers for UI components (includes calibration countdown setup)
            SetupEventHandlers();

            // Display calibration factors
            UpdateFactorsDisplay();
        }

        /// <summary>
        /// Setup UI layout using the dedicated UI builder
        /// </summary>
        private void SetupLayout()
        {
            var uiBuilder = new GraphFormUIBuilder(this, portId);
            uiBuilder.SetupLayout();
        }

        /// <summary>
        /// Setup event handlers for UI components
        /// </summary>
        private void SetupEventHandlers()
        {
            buttonZero.Click += BtnZero_Click;
            buttonRestart.Click += BtnRestart_Click;
            buttonExit.Click += BtnExit_Click;
            buttonExitAll.Click += BtnExitAll_Click;

            filterBox.ItemCheck += (s, e) =>
            {
                // Update visibility state - ItemCheck fires BEFORE the state changes
                lineVisibility[e.Index] = (e.NewValue == CheckState.Checked);
                _needsPlotUpdate = true; // Trigger plot update on next timer tick
            };

            // Initialize status and calibration countdown
            InitializeStatusAndCalibration();
        }

        /// <summary>
        /// Initialize the status label and start calibration countdown if needed
        /// </summary>
        private void InitializeStatusAndCalibration()
        {
            if (!IsCalibrationComplete())
            {
                labelStatus.Text = "Status: Calibrating...";
                labelStatus.ForeColor = UIConstants.Colors.StatusCalibrating;
                StartCalibrationCountdown();
                Debug.WriteLine($"Port {portId + 1}: Started calibration countdown");
            }
            else
            {
                labelStatus.Text = "Status: Ready";
                labelStatus.ForeColor = UIConstants.Colors.StatusReady;
                Debug.WriteLine($"Port {portId + 1}: Calibration already complete");
            }
        }

            // ==== Helper Methods for Label Creation ====
            /// <summary>
            /// Creates a data display label with specified styling and positioning
            /// Helper method for consistent label creation across the UI
            /// </summary>
            /// <param name="left">X position of the label</param>
            /// <param name="color">Text color for the label</param>
            /// <param name="initialText">Initial text content (default: "0")</param>
            /// <param name="alignment">Text alignment within the label (default: MiddleRight)</param>
            /// <param name="fontStyle">Font style to apply (default: Regular)</param>
            /// <returns>Configured Label control ready for use</returns>
            private Label CreateDataLabel(int left, Color color, string initialText = "0", ContentAlignment alignment = ContentAlignment.MiddleRight, FontStyle fontStyle = FontStyle.Regular)
            {
                return new Label
                {
                    AutoSize = false,
                    Width = UIConstants.Layout.LabelWidth,
                    Left = left,
                    Font = new Font(UIConstants.Fonts.DefaultFontFamily, UIConstants.Fonts.DefaultFontSize, fontStyle),
                    Text = $"{initialText,10}",
                    TextAlign = alignment,
                    ForeColor = color,
                    Top = 0
                };
            }

            /// <summary>
            /// Creates a header label with bold styling for column headers
            /// Provides consistent formatting for table-style data display headers
            /// </summary>
            /// <param name="left">X position of the header label</param>
            /// <param name="text">Header text to display</param>
            /// <returns>Configured header Label with bold styling</returns>
            private Label CreateHeaderLabel(int left, string text)
            {
                return new Label
                {
                    AutoSize = false,
                    Width = UIConstants.Layout.LabelWidth,
                    Left = left,
                    Font = new Font(UIConstants.Fonts.DefaultFontFamily, UIConstants.Fonts.DefaultFontSize, FontStyle.Bold),
                    Text = text,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Top = 0
                };
            }
        /// <summary>
        /// Updates the calibration factor display labels with current values from SerialManager
        /// </summary>
        private void UpdateFactorsDisplay()
        {
            var (totalFactor, factors) = SerialManager.Instance.GetFactors((ushort)portId);
            var zeroOffsets = SerialManager.Instance.GetZeroOffsets((ushort)portId);

            // Update individual colored factor labels
            labelFactorLC1.Text = factors[0].ToString("F7");
            labelFactorLC2.Text = factors[1].ToString("F7");
            labelFactorLC3.Text = factors[2].ToString("F7");
            labelFactorLC4.Text = factors[3].ToString("F7");
            labelFactorTotal.Text = totalFactor.ToString("F7");

            // Update individual colored zero offset labels
            labelZeroLC1.Text = zeroOffsets[0].ToString();
            labelZeroLC2.Text = zeroOffsets[1].ToString();
            labelZeroLC3.Text = zeroOffsets[2].ToString();
            labelZeroLC4.Text = zeroOffsets[3].ToString();
            labelZeroTotal.Text = "";
        }

        /// <summary>
        /// Updates the zero offset display labels with current values from SerialManager
        /// </summary>
        private void UpdateZeroOffsetsDisplay()
        {
            var zeroOffsets = SerialManager.Instance.GetZeroOffsets((ushort)portId);

            // Update individual colored zero offset labels
            labelZeroLC1.Text = zeroOffsets[0].ToString();
            labelZeroLC2.Text = zeroOffsets[1].ToString();
            labelZeroLC3.Text = zeroOffsets[2].ToString();
            labelZeroLC4.Text = zeroOffsets[3].ToString();
            labelZeroTotal.Text = "";
        }

        // ==== Live Data & Plot Update ====
        /// <summary>
        /// Handles incoming data from SerialManager, updates internal data collections and triggers plot updates
        /// </summary>
        /// <param name="id">Port ID of the incoming data</param>
        /// <param name="time">Timestamp of the data point</param>
        /// <param name="values">Array of weight values from load cells</param>
        /// <param name="recordNumber">Sequential record number from SerialManager</param>
        /// <param name="adcValues">Raw ADC values from hardware</param>
        private void Instance_OnDataReceived(int id, double time, double[] values, int recordNumber, uint[] adcValues)
        {
            if (id != portId) return;

            this.Invoke((MethodInvoker)delegate
            {
                recordedData.Add((time, values));
                dataPoints.Add((time, values));

                // More efficient data point removal - avoid RemoveAll with delegate
                var cutoffTime = time - displaySeconds;
                int removeCount = 0;
                for (int i = 0; i < dataPoints.Count; i++)
                {
                    if (dataPoints[i].time >= cutoffTime) break;
                    removeCount++;
                }
                if (removeCount > 0)
                {
                    dataPoints.RemoveRange(0, removeCount);
                }

                recordCountSinceLastRate++;
                totalRecordCount = recordNumber; // Use the record number from SerialManager
                currentAdcValues = adcValues; // Store current ADC values
                _needsArrayUpdate = true;

                // Calculate rate every 10 seconds
                if ((DateTime.Now - lastRateUpdate).TotalSeconds >= rateUpdateIntervalSeconds)
                {
                    currentRate = recordCountSinceLastRate / (DateTime.Now - lastRateUpdate).TotalSeconds;
                    recordCountSinceLastRate = 0;
                    lastRateUpdate = DateTime.Now;

                    Debug.WriteLine($"Port {portId + 1}: Rate updated to {currentRate:F1} rec/s");
                }

                // Log first few data points for debugging
                if (recordedData.Count <= 5 || recordedData.Count % 100 == 0)
                {
                    Debug.WriteLine($"Port {portId + 1} received data point #{recordedData.Count}: time={time:F1}, values=[{string.Join(",", values.Select(v => v.ToString("F1")))}], rate={currentRate:F1} rec/s");
                }
            });
        }
        /// <summary>
        /// Timer tick handler that updates plots and labels at regular intervals
        /// Optimizes performance by caching data arrays and only updating when necessary
        /// </summary>
        /// <param name="sender">Timer object that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (dataPoints.Count == 0) return;

            // Cache arrays once and reuse - avoid multiple LINQ operations
            if (_cachedArrays == null || _needsArrayUpdate)
            {
                _cachedArrays = new CachedPlotData
                {
                    Times = dataPoints.Select(dp => dp.time).ToArray(),
                    LC1 = dataPoints.Select(dp => dp.values[0]).ToArray(),
                    LC2 = dataPoints.Select(dp => dp.values[1]).ToArray(),
                    LC3 = dataPoints.Select(dp => dp.values[2]).ToArray(),
                    LC4 = dataPoints.Select(dp => dp.values[3]).ToArray(),
                    Total = dataPoints.Select(dp => dp.values.Sum()).ToArray()
                };
                _needsArrayUpdate = false;
                _needsPlotUpdate = true;
            }

            // Only update plots when visibility changes or new data
            if (_needsPlotUpdate)
            {
                _plotManager.UpdatePlots(_cachedArrays, lineVisibility);
                _needsPlotUpdate = false;
            }

            UpdateLabels(_cachedArrays);
        }

        /// <summary>
        /// Updates all data display labels with the latest values from cached plot data
        /// Also handles status label updates based on calibration and data reception state
        /// </summary>
        /// <param name="data">Cached plot data containing current values</param>
        private void UpdateLabels(CachedPlotData data)
        {
            // Cache latest values to avoid repeated array access
            var lastTime = data.Times.Last();
            var lastLc1 = data.LC1.Last();
            var lastLc2 = data.LC2.Last();
            var lastLc3 = data.LC3.Last();
            var lastLc4 = data.LC4.Last();
            var lastTotal = data.Total.Last();

            // Batch update labels with cached values
            labelRecord.Text = totalRecordCount.ToString("N0");
            labelElapsed.Text = lastTime.ToString("F1");
            labelLC1.Text = lastLc1.ToString("F0");
            labelLC2.Text = lastLc2.ToString("F0");
            labelLC3.Text = lastLc3.ToString("F0");
            labelLC4.Text = lastLc4.ToString("F0");
            labelTotal.Text = lastTotal.ToString("F0");

            // Update individual colored ADC value labels
            labelAdcLC1.Text = currentAdcValues[0].ToString();
            labelAdcLC2.Text = currentAdcValues[1].ToString();
            labelAdcLC3.Text = currentAdcValues[2].ToString();
            labelAdcLC4.Text = currentAdcValues[3].ToString();
            labelAdcTotal.Text = "";

            // Update status - but only if not currently calibrating (don't override countdown)
            if (calibrationCountdownTimer == null && IsCalibrationComplete())
            {
                if (currentRate > 0)
                {
                    labelStatus.Text = $"Status: Running — {currentRate:F1} rec/s";
                    labelStatus.ForeColor = UIConstants.Colors.StatusReady;
                }
                else
                {
                    labelStatus.Text = "Status: Running — measuring...";
                    labelStatus.ForeColor = UIConstants.Colors.StatusReady;
                }
            }
        }
        /// <summary>
        /// Freezes the current session by stopping data collection and plot updates
        /// Used during exit operations to ensure clean data capture
        /// </summary>
        public void FreezeSession()
        {
            updateTimer.Stop();
            SerialManager.Instance.OnDataReceived -= Instance_OnDataReceived;
        }
        // ==== Mark Zero helpers ====
        /// <summary>
        /// Adds a zero calibration marker to the plot at the specified time
        /// Updates zero offset display and plot visual indicators
        /// </summary>
        /// <param name="now">Time value where the zero marker should be placed</param>
        public void MarkZero(double now)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double>(MarkZero), now);
                return;
            }

            _plotManager.AddZeroMarker(now);

            // Update zero line with current values when zero calculation finishes
            labelZeroHeaders.Text = totalRecordCount.ToString();
            labelZeroElapsed.Text = now.ToString("F1");

            // Update zero offsets display after manual zero calibration
            UpdateZeroOffsetsDisplay();
        }
        /// <summary>
        /// Adds a zero calibration marker at the current time (last data point)
        /// Convenience overload that automatically determines the current time
        /// </summary>
        public void MarkZero()
        {
            double now = dataPoints.Count > 0 ? dataPoints.Last().time : 0;
            MarkZero(now);
        }
        // ==== User Actions & Buttons ====
        /// <summary>
        /// Handles Zero button click - starts manual zero calibration for this port
        /// </summary>
        /// <param name="sender">Button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnZero_Click(object? sender, EventArgs e)
        {
            SerialManager.Instance.StartManualZeroCalibration((ushort)portId);
            AutoClosingMessage.Show($"Port {portId + 1} re-zeroing started...", SerialManager.Instance.CalibrationSeconds * 1000);
        }
        /// <summary>
        /// Handles Restart button click - restarts the entire session
        /// </summary>
        /// <param name="sender">Button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            AutoClosingMessage.Show("Restarting...", 1000);
            Program.AppContext?.RestartSession();
        }
        /// <summary>
        /// Handles Exit button click - saves data, shows session summary, and closes this form
        /// </summary>
        /// <param name="sender">Button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnExit_Click(object? sender, EventArgs e)
        {
            FreezeSession();

            // Ensure data is saved even if user exits early
            if (recordedData.Count > 0)
            {
                SaveSessionData();
            }

            ShowFullSessionGraphModal();
            AutoClosingMessage.Show("Closing current graphs...", 1000);
            Close();
        }
        /// <summary>
        /// Handles Exit All button click - saves data for all forms and shows session summaries
        /// </summary>
        /// <param name="sender">Button that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void BtnExitAll_Click(object? sender, EventArgs e)
        {
            AutoClosingMessage.Show("Closing all graphs and showing session summaries...", 1000);
            ShowAllSessionGraphsAndThenExit();
        }
        /// <summary>
        /// Handles keyboard shortcuts for quick access to common functions
        /// Z=Zero, R=Restart, S=Save, X=Exit, A=Exit All
        /// </summary>
        /// <param name="e">Key event arguments containing the pressed key</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) 
                return;

            if (e.KeyCode == Keys.Z)
            {
                BtnZero_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.R)
            {
                BtnRestart_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.S)
            {
                SaveSessionData();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.X)
            {
                BtnExit_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.A)
            {
                BtnExitAll_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        // ==== Graph/Summary Helpers ====
        /// <summary>
        /// Static method to save data from all open GraphForm instances and show session summaries
        /// Used by Exit All functionality to coordinate shutdown of multiple forms
        /// </summary>
        public static void ShowAllSessionGraphsAndThenExit()
        {
            var liveForms = Application.OpenForms.OfType<GraphForm>().ToList();

            // IMPORTANT: Save data from all forms BEFORE hiding/freezing them
            foreach (var gf in liveForms)
            {
                if (gf.recordedData.Count > 0)
                {
                    gf.SaveSessionData(true); // Use skipIfExists=true for Exit All to prevent duplicates
                    Debug.WriteLine($"Saved data for Port {gf.portId + 1} during Exit All");
                }
            }

            var formSnapshots = liveForms
                .Select(gf => new
                {
                    gf.portId,
                    gf.Size,
                    gf.Location,
                    gf.WindowState,
                    Data = gf.recordedData.ToList(),
                    ZeroMarkers = gf.zeroMarkers,
                    Title = gf.Text
                }).ToList();

            foreach (var gf in liveForms)
                gf.FreezeSession();
            foreach (var gf in liveForms)
                gf.Hide();

            int openSummaries = formSnapshots.Count;
            foreach (var snap in formSnapshots)
            {
                var summaryForm = CreateFullSessionGraphForm(
                    snap.Data, snap.ZeroMarkers, snap.portId, snap.Size, snap.Location, snap.WindowState, snap.Title);
                summaryForm.FormClosed += (fs, fe) =>
                {
                    openSummaries--;
                    if (openSummaries == 0)
                        foreach (var gf in liveForms)
                            gf.Close();
                };
                summaryForm.Show();
            }
        }
        /// <summary>
        /// Creates a session summary form displaying the complete data set with filtering capabilities
        /// Used for post-session data review and analysis
        /// </summary>
        /// <param name="recordedData">Complete session data to display</param>
        /// <param name="zeroMarkers">Zero calibration markers to show</param>
        /// <param name="portId">Port ID for labeling</param>
        /// <param name="size">Window size to use</param>
        /// <param name="location">Window location to use</param>
        /// <param name="windowState">Window state (normal/maximized)</param>
        /// <param name="title">Window title</param>
        /// <returns>Configured summary form ready to display</returns>
        public static Form CreateFullSessionGraphForm(
            List<(double time, double[] values)> recordedData,
            List<VerticalLine> zeroMarkers,
            int portId,
            Size size,
            Point location,
            FormWindowState windowState,
            string title)
        {
            var plotControl = new ScottPlot.WinForms.FormsPlot();
            var plot = plotControl.Plot;
            AddSessionPlot(plot, recordedData, zeroMarkers, portId);

            Form dlg = new Form
            {
                Text = title,
                Size = size,
                StartPosition = FormStartPosition.Manual,
                Location = location,
                WindowState = windowState,
                Icon = Application.OpenForms.OfType<GraphForm>().FirstOrDefault()?.Icon
            };
            plotControl.Dock = DockStyle.Fill;
            dlg.Controls.Add(plotControl);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120
            };
            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 120,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            // LC filter on the left
            var filterBox = new CheckedListBox
            {
                Width = 100,
                Font = new Font("Segoe UI", 9F),
                Height = 100,
                CheckOnClick = true
            };
            filterBox.Items.AddRange(new object[] { "LC1", "LC2", "LC3", "LC4", "Total" });
            for (int i = 0; i < filterBox.Items.Count; i++)
                filterBox.SetItemChecked(i, true); // All visible by default

            var plotLines = plot.PlottableList;
            filterBox.ItemCheck += (s, e) =>
            {
                // ItemCheck fires BEFORE the state changes, so manually set according to NewValue
                plotLines[0].IsVisible = (e.Index == 0 && e.NewValue == CheckState.Checked) || (e.Index != 0 && filterBox.GetItemChecked(0));
                plotLines[1].IsVisible = (e.Index == 1 && e.NewValue == CheckState.Checked) || (e.Index != 1 && filterBox.GetItemChecked(1));
                plotLines[2].IsVisible = (e.Index == 2 && e.NewValue == CheckState.Checked) || (e.Index != 2 && filterBox.GetItemChecked(2));
                plotLines[3].IsVisible = (e.Index == 3 && e.NewValue == CheckState.Checked) || (e.Index != 3 && filterBox.GetItemChecked(3));
                plotLines[4].IsVisible = (e.Index == 4 && e.NewValue == CheckState.Checked) || (e.Index != 4 && filterBox.GetItemChecked(4));
                plot.Axes.AutoScale();
                plotControl.Refresh();
            };
            dlg.Controls.Add(filterBox);

            var spacer = new Panel { Width = 1, Height = 1, AutoSize = true };
            // Exit All button on the right
            var exitBtn = new Button
            {
                Text = "Exit All",
                Height = 40,
                Width = 110,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            exitBtn.Click += (s, e) =>
            {
                foreach (var f in openSummaryForms.ToList())
                    if (!f.IsDisposed) f.Close();
                openSummaryForms.Clear();
            };
            flowPanel.Controls.Add(filterBox);
            flowPanel.Controls.Add(spacer);
            flowPanel.SetFlowBreak(filterBox, false);
            flowPanel.Controls.Add(new Label { AutoSize = true, Width = 9999 }); // flexible filler
            flowPanel.Controls.Add(exitBtn);

            bottomPanel.Controls.Add(flowPanel);
            dlg.Controls.Add(bottomPanel);
            dlg.AcceptButton = exitBtn; // Set Exit All as default button

            openSummaryForms.Add(dlg);
            dlg.FormClosed += (s, e) => openSummaryForms.Remove(dlg);
            return dlg;
        }
        /// <summary>
        /// Static helper method that delegates session plot creation to PlotManager
        /// Maintains compatibility with existing code while using the new architecture
        /// </summary>
        /// <param name="plot">ScottPlot.Plot instance to configure</param>
        /// <param name="recordedData">Session data to plot</param>
        /// <param name="zeroMarkers">Zero markers to display</param>
        /// <param name="portId">Port ID for labeling</param>
        private static void AddSessionPlot(
            ScottPlot.Plot plot,
            List<(double time, double[] values)> recordedData,
            List<VerticalLine> zeroMarkers,
            int portId)
        {
            PlotManager.AddSessionPlot(plot, recordedData, zeroMarkers, portId);
        }
        /// <summary>
        /// Shows a modal dialog with the complete session graph for detailed review
        /// Includes filtering capabilities and statistical information
        /// </summary>
        private void ShowFullSessionGraphModal()
        {
            if (recordedData.Count == 0)
            {
                AutoClosingMessage.Show("No session data to display.");
                return;
            }
            var plotControl = new FormsPlot();
            var plot = plotControl.Plot;
            AddSessionPlot(plot, recordedData, zeroMarkers, portId);

            Form dlg = new Form
            {
                Text = $"Full Session Graph — Port {portId + 1}",
                Size = this.Size,
                StartPosition = FormStartPosition.Manual,
                Location = this.Location,
                Icon = Application.OpenForms.OfType<GraphForm>().FirstOrDefault()?.Icon
            };

            plotControl.Dock = DockStyle.Fill;
            dlg.Controls.Add(plotControl);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120
            };
            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 120,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            // LC filter on the left
            var filterBox = new CheckedListBox
            {
                Width = 100,
                Font = new Font("Segoe UI", 9F),
                Height = 100,
                CheckOnClick = true // Enable single-click toggle like live mode
            };
            filterBox.Items.AddRange(new object[] { "LC1", "LC2", "LC3", "LC4", "Total" });
            for (int i = 0; i < filterBox.Items.Count; i++)
                filterBox.SetItemChecked(i, true); // All visible by default

            var plotLines = plot.PlottableList;
            filterBox.ItemCheck += (s, e) =>
            {
                // ItemCheck fires BEFORE the state changes, so manually set according to NewValue
                plotLines[0].IsVisible = (e.Index == 0 && e.NewValue == CheckState.Checked) || (e.Index != 0 && filterBox.GetItemChecked(0));
                plotLines[1].IsVisible = (e.Index == 1 && e.NewValue == CheckState.Checked) || (e.Index != 1 && filterBox.GetItemChecked(1));
                plotLines[2].IsVisible = (e.Index == 2 && e.NewValue == CheckState.Checked) || (e.Index != 2 && filterBox.GetItemChecked(2));
                plotLines[3].IsVisible = (e.Index == 3 && e.NewValue == CheckState.Checked) || (e.Index != 3 && filterBox.GetItemChecked(3));
                plotLines[4].IsVisible = (e.Index == 4 && e.NewValue == CheckState.Checked) || (e.Index != 4 && filterBox.GetItemChecked(4));
                plot.Axes.AutoScale();
                plotControl.Refresh();
            };
            dlg.Controls.Add(filterBox);

            var spacer = new Panel { Width = 1, Height = 1, AutoSize = true };

            // Exit All button on the right
            var exitBtn = new Button
            {
                Text = "Exit",
                Height = 40,
                Width = 110,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            exitBtn.Click += (s, e) => dlg.Close();
            flowPanel.Controls.Add(filterBox);
            flowPanel.Controls.Add(spacer);
            flowPanel.SetFlowBreak(filterBox, false);
            flowPanel.Controls.Add(new Label { AutoSize = true, Width = 9999 }); // flexible filler
            flowPanel.Controls.Add(exitBtn);

            bottomPanel.Controls.Add(flowPanel);
            dlg.Controls.Add(bottomPanel);

            dlg.ShowDialog();
        }
        // ==== Session Data Management ====
        /// <summary>
        /// Saves session data synchronously with default settings
        /// Convenience method that delegates to SaveSessionData(false) for normal save operations
        /// </summary>
        public void SaveSessionData()
        {
            SaveSessionData(false); // Normal save - always save current data
        }

        /// <summary>
        /// Saves session data with option to skip if already saved (for Exit All scenario)
        /// Provides control over duplicate file handling during coordinated multi-form exits
        /// Uses SessionDataManager for structured save operations with comprehensive error handling
        /// </summary>
        /// <param name="skipIfExists">If true, skips saving when files already exist (Exit All coordination)</param>
        public void SaveSessionData(bool skipIfExists)
        {
            var sessionData = CreateSessionData();
            var result = _sessionManager.SaveSessionData(sessionData, skipIfExists);

            HandleSaveResult(result);
        }

        /// <summary>
        /// Saves session data asynchronously for improved UI responsiveness
        /// Offloads file I/O operations to background thread to prevent UI blocking
        /// Ideal for large datasets or when non-blocking save operations are preferred
        /// </summary>
        /// <param name="skipIfExists">If true, skips saving when files already exist</param>
        /// <returns>Task that completes when save operation finishes</returns>
        public async Task SaveSessionDataAsync(bool skipIfExists = false)
        {
            var sessionData = CreateSessionData();
            var result = await _sessionManager.SaveSessionDataAsync(sessionData, skipIfExists);

            HandleSaveResult(result);
        }

        /// <summary>
        /// Creates a SessionData transfer object from current form state
        /// Packages all session information for save operations including data, markers, and metadata
        /// Creates defensive copies to prevent modification during async save operations
        /// </summary>
        /// <returns>SessionData object containing complete session information</returns>
        private SessionData CreateSessionData()
        {
            return new SessionData
            {
                PortId = portId,
                RecordedData = recordedData.ToList(), // Create copy to avoid modification during save
                ZeroMarkers = zeroMarkers.ToList(),
                TotalRecordCount = totalRecordCount
            };
        }

        /// <summary>
        /// Processes save operation results and provides appropriate user feedback
        /// Handles success, error, and skip scenarios with contextual messaging
        /// Coordinates debug logging and user notifications based on operation outcome
        /// </summary>
        /// <param name="result">SaveSessionResult containing operation status and details</param>
        private void HandleSaveResult(SaveSessionResult result)
        {
            var message = _sessionManager.CreateSaveMessage(result, portId);

            if (result.Success && !result.WasSkipped)
            {
                Debug.WriteLine($"Port {portId + 1}: {message}");
                AutoClosingMessage.Show(message, 3000);
            }
            else if (result.WasSkipped)
            {
                Debug.WriteLine($"Port {portId + 1}: {message}");
                // Don't show user message for skipped saves during Exit All
            }
            else
            {
                Debug.WriteLine($"Port {portId + 1}: {message}");
                AutoClosingMessage.Show(message, 3000);
            }
        }
        // ==== Form Closing, Misc UI ====
        /// <summary>
        /// Handles form closing event - saves data, preserves window settings, and cleans up resources
        /// </summary>
        /// <param name="sender">Form that is closing</param>
        /// <param name="e">Form closing event arguments</param>
        private void GraphForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Only save if we have data and haven't already saved (avoid double-save during Exit All)
            if (recordedData.Count > 0)
            {
                SaveSessionData();
            }

            if (WindowState == FormWindowState.Normal)
            {
                localWindowSettings.Width = Width;
                localWindowSettings.Height = Height;
                localWindowSettings.Left = Left;
                localWindowSettings.Top = Top;
                localWindowSettings.Maximized = false;
            }
            else if (WindowState == FormWindowState.Maximized)
            {
                localWindowSettings.Width = RestoreBounds.Width;
                localWindowSettings.Height = RestoreBounds.Height;
                localWindowSettings.Left = RestoreBounds.Left;
                localWindowSettings.Top = RestoreBounds.Top;
                localWindowSettings.Maximized = true;
            }
            globalSettings.WindowSettingsPerId[portId] = localWindowSettings;
            SerialManager.Instance.SaveSettings();
        }
        /// <summary>
        /// Override of Form.OnFormClosing to ensure proper cleanup of resources and event handlers
        /// Called after GraphForm_FormClosing to handle final cleanup
        /// </summary>
        /// <param name="e">Form closing event arguments</param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Cleanup resources and events
            updateTimer?.Dispose();
            calibrationCountdownTimer?.Dispose();

            // Unsubscribe from events
            if (SerialManager.Instance != null)
            {
                SerialManager.Instance.OnDataReceived -= Instance_OnDataReceived;
                SerialManager.Instance.CalibrationCompleted -= Instance_CalibrationCompleted;
            }

            // Clear collections
            dataPoints?.Clear();
            recordedData?.Clear();
            zeroMarkers?.Clear();
            _cachedArrays = null;

            base.OnFormClosing(e);
        }
        /// <summary>
        /// Checks if the initial calibration process has completed
        /// Uses reflection to access private SerialManager field until a proper API is available
        /// </summary>
        /// <returns>True if calibration is complete, false if still in progress</returns>
        private bool IsCalibrationComplete()
        {
            // You may want to expose a property in SerialManager for this, but for now:
            var sm = SerialManager.Instance;
            var end = sm.GetType().GetField("calibrationEndTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(sm);
            if (end is DateTime endTime)
                return endTime > DateTime.MinValue;
            return false;
        }
        /// <summary>
        /// Starts a timer that displays the remaining calibration time in the status label
        /// Provides visual feedback to the user during the calibration process
        /// </summary>
        private void StartCalibrationCountdown()
        {
            calibrationCountdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };
            calibrationCountdownTimer.Tick += CalibrationCountdownTimer_Tick;
            calibrationCountdownTimer.Start();
            CalibrationCountdownTimer_Tick(this, EventArgs.Empty); // update immediately

            Debug.WriteLine($"Port {portId + 1}: Calibration countdown timer started");
        }
        /// <summary>
        /// Timer tick handler for calibration countdown display
        /// Updates status label with remaining calibration time and handles completion
        /// </summary>
        /// <param name="sender">Timer that triggered the event</param>
        /// <param name="e">Event arguments</param>
        private void CalibrationCountdownTimer_Tick(object? sender, EventArgs e)
        {
            var sm = SerialManager.Instance;
            int elapsed = (int)(DateTime.Now - sm.CalibrationStartTime).TotalSeconds;
            int remain = sm.CalibrationSeconds - elapsed;

            if (remain <= 0)
            {
                labelStatus.Text = "Status: Calibrating... 0s left";
                labelStatus.ForeColor = UIConstants.Colors.StatusCalibrating;
                calibrationCountdownTimer?.Stop();
                calibrationCountdownTimer?.Dispose();
                calibrationCountdownTimer = null;
                Debug.WriteLine($"Port {portId + 1}: Calibration countdown finished");
            }
            else
            {
                labelStatus.Text = $"Status: Calibrating... {remain}s left";
                labelStatus.ForeColor = UIConstants.Colors.StatusCalibrating;

                // Log countdown progress every 5 seconds
                if (remain % 5 == 0)
                {
                    Debug.WriteLine($"Port {portId + 1}: Calibration countdown - {remain}s remaining");
                }
            }
        }
        /// <summary>
        /// Handles the calibration completed event from SerialManager
        /// Updates UI state and plays notification sound when calibration finishes
        /// </summary>
        private void Instance_CalibrationCompleted()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Instance_CalibrationCompleted));
                return;
            }

            calibrationCountdownTimer?.Stop();
            calibrationCountdownTimer?.Dispose();
            calibrationCountdownTimer = null;

            SystemSounds.Exclamation.Play(); 

            labelStatus.Text = "Status: Ready";
            labelStatus.ForeColor = UIConstants.Colors.StatusReady;

            Debug.WriteLine($"Port {portId + 1}: Calibration completed successfully");

            // Update zero offsets display after initial calibration completion
            UpdateZeroOffsetsDisplay();
        }


    }
}
