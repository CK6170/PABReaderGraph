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
    public partial class GraphForm : Form
    {
        // ==== Instance Data ====
        private int portId;
        private Settings globalSettings;
        private WindowSettings localWindowSettings;
        private readonly List<(double time, double[] values)> dataPoints = new();
        private readonly double displaySeconds = 10;
        private readonly System.Windows.Forms.Timer updateTimer;
        private List<(double time, double[] values)> recordedData = new();
        private List<VerticalLine> zeroMarkers = new();
        private readonly object plotLock = new();
        private DateTime lastRateUpdate = DateTime.Now;
        private int recordCountSinceLastRate = 0;
        private int totalRecordCount = 0;
        private double currentRate = 0.0;
        private uint[] currentAdcValues = new uint[4];
        private const int rateUpdateIntervalSeconds = 10;
        private System.Windows.Forms.Timer? calibrationCountdownTimer;

        // Individual colored labels for ADC, Zero, and Factor values
        private Label labelAdcLC1, labelAdcLC2, labelAdcLC3, labelAdcLC4, labelAdcTotal;
        private Label labelZeroLC1, labelZeroLC2, labelZeroLC3, labelZeroLC4, labelZeroTotal, labelZeroHeaders, labelZeroElapsed;
        private Label labelFactorLC1, labelFactorLC2, labelFactorLC3, labelFactorLC4, labelFactorTotal;

        // Runtime filter control for toggling graph lines
        private CheckedListBox filterBox;
        private bool[] lineVisibility = new bool[5] { true, true, true, true, true }; // LC1, LC2, LC3, LC4, Total


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

            formsPlot.Plot.ShowLegend();
            formsPlot.Plot.Legend.Alignment = Alignment.UpperRight;
            formsPlot.Plot.Title($"Port {portId + 1} Live Data");
            formsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromSDColor(Color.LightGray);
            formsPlot.Plot.Grid.MajorLinePattern = ScottPlot.LinePattern.Dotted;
            formsPlot.Plot.FigureBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };
            formsPlot.Plot.DataBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };

            updateTimer = new System.Windows.Forms.Timer { Interval = 100 }; // Reduced from 50ms to 100ms
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            FormClosing += GraphForm_FormClosing;
            SerialManager.Instance.OnDataReceived += Instance_OnDataReceived;
            SerialManager.Instance.CalibrationCompleted += Instance_CalibrationCompleted;

            // Register this form with SerialManager so it can receive zero calibration updates
            SerialManager.Instance.RegisterGraphForm((ushort)portId, this);

            labelStatus.Text = "Status: Calibrating...";
            labelStatus.ForeColor = Color.Orange;

            // Display calibration factors
            UpdateFactorsDisplay();
        }
        private void SetupLayout()
        {
            formsPlot = new FormsPlot { Dock = DockStyle.Fill };
            Controls.Add(formsPlot);

            // Top status panel
            panelTop = new Panel { Dock = DockStyle.Bottom, Height = 120 }; // Reduced from 200 to 120
            Controls.Add(panelTop);

            // Create a panel to hold individual value labels
            labelValuesPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20
            };
            panelTop.Controls.Add(labelValuesPanel);

            // Create individual labels for each value with colors matching the graphs
            labelRecord = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 110,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
            labelElapsed = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 209,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0.0,10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black
            };
            var labelType = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 308,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = "Weight",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black // Weight uses live data colors (brightest)
            };
            labelLC1 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 407,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Blue
            };
            labelLC2 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 506,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Red
            };
            labelLC3 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 605,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Green
            };
            labelLC4 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 704,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Orange
            };
            labelTotal = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 803,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Black
            };

            labelValuesPanel.Controls.AddRange(new Control[] { labelRecord, labelElapsed, labelType, labelLC1, labelLC2, labelLC3, labelLC4, labelTotal });

            // ADC Values row - individual colored labels
            var labelAdcHeaders = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 110,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            var labelAdcElapsed = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 209,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            var labelAdcType = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 308,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = "ADC",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(50, 50, 80), // Dark shade to match ADC row
                Top = 0
            };

            labelAdcLC1 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 407,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(25, 25, 112), // Midnight Blue - now darker for ADC
                Top = 0
            };
            labelAdcLC2 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 506,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(128, 0, 0), // Maroon - now darker for ADC
                Top = 0
            };
            labelAdcLC3 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 605,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(0, 80, 0), // Dark Green - now darker for ADC
                Top = 0
            };
            labelAdcLC4 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 704,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(204, 85, 0), // Dark Orange - now darker for ADC
                Top = 0
            };
            labelAdcTotal = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 803,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{"",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.LightGray,
                Top = 0
            };

            var panelAdc = new Panel
            {
                Height = 20,
                Dock = DockStyle.Top
            };
            panelAdc.Controls.AddRange(new Control[] { labelAdcHeaders, labelAdcElapsed, labelAdcType, labelAdcLC1, labelAdcLC2, labelAdcLC3, labelAdcLC4, labelAdcTotal });
            panelTop.Controls.Add(panelAdc);

            // Zero Offsets row - individual colored labels  
            labelZeroHeaders = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 110,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            labelZeroElapsed = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 209,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            var labelZeroType = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 308,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = "Zero",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(80, 100, 120), // Medium shade to match Zero row
                Top = 0
            };

            labelZeroLC1 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 407,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(100, 149, 237), // Medium Blue
                Top = 0
            };
            labelZeroLC2 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 506,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(220, 20, 60), // Medium Red
                Top = 0
            };
            labelZeroLC3 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 605,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(60, 179, 113), // Medium Green
                Top = 0
            };
            labelZeroLC4 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 704,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{0,10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(255, 140, 0), // Medium Orange
                Top = 0
            };
            labelZeroTotal = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 803,
                Font = new Font("Consolas", 12F, FontStyle.Regular),
                Text = $"{"",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.Gray,
                Top = 0
            };

            var panelZero = new Panel
            {
                Height = 20,
                Dock = DockStyle.Top
            };
            panelZero.Controls.AddRange(new Control[] { labelZeroHeaders, labelZeroElapsed, labelZeroType, labelZeroLC1, labelZeroLC2, labelZeroLC3, labelZeroLC4, labelZeroTotal });
            panelTop.Controls.Add(panelZero);

            // Factors row - individual colored labels
            var labelFactorHeaders = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 110,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            var labelFactorElapsed = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 209,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{" ",10}",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray,
                Top = 0
            };
            var labelFactorType = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 308,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = "Factor",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(150, 170, 190), // Light shade to match Factor row
                Top = 0
            };

            labelFactorLC1 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 407,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{"0.000000",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(135, 206, 250), // Sky Blue - now lighter for Factor
                Top = 0
            };
            labelFactorLC2 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 506,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{"0.000000",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(255, 160, 160), // Light Coral - now lighter for Factor
                Top = 0
            };
            labelFactorLC3 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 605,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{"0.000000",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(152, 251, 152), // Pale Green - now lighter for Factor
                Top = 0
            };
            labelFactorLC4 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 704,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{"0.000000",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(255, 222, 173), // Navajo White - now lighter for Factor
                Top = 0
            };
            labelFactorTotal = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 803,
                Font = new Font("Consolas", 12F, FontStyle.Italic),
                Text = $"{"0.000000",10}",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DarkGray,
                Top = 0
            };

            var panelFactors = new Panel
            {
                Height = 20,
                Dock = DockStyle.Top
            };
            panelFactors.Controls.AddRange(new Control[] { labelFactorHeaders, labelFactorElapsed, labelFactorType, labelFactorLC1, labelFactorLC2, labelFactorLC3, labelFactorLC4, labelFactorTotal });
            panelTop.Controls.Add(panelFactors);

            // Create individual header labels with proper alignments and filter box
            var panelHeaders = new Panel
            {
                Height = 20, // Back to original size
                Dock = DockStyle.Top
            };

            // Add filter box to align with headers row
            filterBox = new CheckedListBox
            {
                Width = 100,
                Left = 5,
                Height = 100, // Reasonable height to show all 5 items
                Top = 10,// Position just below the 20px header panel
                Font = new Font("Segoe UI", 9F), // Appropriate font size
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(240, 240, 240),
                CheckOnClick = true,
                IntegralHeight = false, // Prevent automatic resizing
                Visible = true // Keep filter box visible
            };
            filterBox.Items.AddRange(new object[] { "LC1", "LC2", "LC3", "LC4", "Total" });
            for (int i = 0; i < filterBox.Items.Count; i++)
                filterBox.SetItemChecked(i, true); // All visible by default

            filterBox.ItemCheck += (s, e) =>
            {
                // Update visibility state - ItemCheck fires BEFORE the state changes
                lineVisibility[e.Index] = (e.NewValue == CheckState.Checked);
            };

            var labelHeaderRecords = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 110,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "Records",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderElapsed = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 209,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "Elapsed",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderType = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 308,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "Type",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderLC1 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 407,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "LC1",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderLC2 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 506,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "LC2",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderLC3 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 605,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "LC3",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderLC4 = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 704,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "LC4",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };
            var labelHeaderTotal = new Label
            {
                AutoSize = false,
                Width = 100,
                Left = 803,
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                Text = "Total",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0 // Back to original position
            };

            panelHeaders.Controls.AddRange(new Control[] { labelHeaderRecords, labelHeaderElapsed, labelHeaderType, labelHeaderLC1, labelHeaderLC2, labelHeaderLC3, labelHeaderLC4, labelHeaderTotal });
            panelTop.Controls.Add(panelHeaders);

            // Add filter box directly to panelTop so it's visible
            panelTop.Controls.Add(filterBox);
            filterBox.BringToFront(); // Bring to front so it's not hidden behind other panels

            // Bottom button panel
            panelBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            Controls.Add(panelBottom);

            buttonZero = new Button { Text = "Zero", Width = 100, Height = 30 };
            buttonZero.Click += BtnZero_Click;
            buttonRestart = new Button { Text = "Restart", Width = 100, Height = 30 };
            buttonRestart.Click += BtnRestart_Click;
            buttonExit = new Button { Text = "Exit", Width = 100, Height = 30 };
            buttonExit.Click += BtnExit_Click;
            buttonExitAll = new Button { Text = "Exit All", Width = 100, Height = 30 };
            buttonExitAll.Click += BtnExitAll_Click;
            labelStatus = new Label { Text = "Status: Calibrating...", AutoSize = true, Margin = new Padding(50, 10, 0, 0) };
            // In constructor, after labelStatus is set
            if (!IsCalibrationComplete())
            {
                StartCalibrationCountdown();
            }


            Font = new Font("Consolas", 12F, FontStyle.Regular);

            panelBottom.Controls.AddRange(new Control[] { buttonZero, buttonRestart, buttonExit, buttonExitAll, labelStatus });
            var shortcutLabel = new Label
            {
                Text = "Shortcuts: Z=Zero, R=Restart, X=Exit, A=Exit All",
                Dock = DockStyle.Bottom,
                Font = new Font("Consolas", 9F, FontStyle.Italic),
                ForeColor = Color.DarkGray
            };
            Controls.Add(shortcutLabel);
            //shortcutLabel.BringToFront();
        }

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
        private void Instance_OnDataReceived(int id, double time, double[] values, int recordNumber, uint[] adcValues)
        {
            if (id != portId) return;
            this.Invoke((MethodInvoker)delegate
            {
                recordedData.Add((time, values));
                dataPoints.Add((time, values));
                dataPoints.RemoveAll(dp => dp.time < time - displaySeconds);
                recordCountSinceLastRate++;
                totalRecordCount = recordNumber; // Use the record number from SerialManager
                currentAdcValues = adcValues; // Store current ADC values
                if ((DateTime.Now - lastRateUpdate).TotalSeconds >= rateUpdateIntervalSeconds)
                {
                    currentRate = recordCountSinceLastRate / (DateTime.Now - lastRateUpdate).TotalSeconds;
                    recordCountSinceLastRate = 0;
                    lastRateUpdate = DateTime.Now;
                }
            });
        }
        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (dataPoints.Count == 0) return;

            // Cache the data arrays once
            var times = dataPoints.Select(dp => dp.time).ToArray();
            var lc1 = dataPoints.Select(dp => dp.values[0]).ToArray();
            var lc2 = dataPoints.Select(dp => dp.values[1]).ToArray();
            var lc3 = dataPoints.Select(dp => dp.values[2]).ToArray();
            var lc4 = dataPoints.Select(dp => dp.values[3]).ToArray();
            var total = dataPoints.Select(dp => dp.values.Sum()).ToArray();

            // Update plot less frequently to improve performance
            lock (plotLock)
            {
                formsPlot.Plot.Clear<Scatter>();

                var plot1 = formsPlot.Plot.Add.Scatter(times, lc1); 
                plot1.LegendText = "LC1"; 
                plot1.MarkerSize = 2;
                plot1.Color = ScottPlot.Color.FromSDColor(Color.Blue);
                plot1.IsVisible = lineVisibility[0];

                var plot2 = formsPlot.Plot.Add.Scatter(times, lc2); 
                plot2.LegendText = "LC2"; 
                plot2.MarkerSize = 2;
                plot2.Color = ScottPlot.Color.FromSDColor(Color.Red);
                plot2.IsVisible = lineVisibility[1];

                var plot3 = formsPlot.Plot.Add.Scatter(times, lc3); 
                plot3.LegendText = "LC3"; 
                plot3.MarkerSize = 2;
                plot3.Color = ScottPlot.Color.FromSDColor(Color.Green);
                plot3.IsVisible = lineVisibility[2];

                var plot4 = formsPlot.Plot.Add.Scatter(times, lc4); 
                plot4.LegendText = "LC4"; 
                plot4.MarkerSize = 2;
                plot4.Color = ScottPlot.Color.FromSDColor(Color.Orange);
                plot4.IsVisible = lineVisibility[3];

                var plotTotal = formsPlot.Plot.Add.Scatter(times, total);
                plotTotal.LegendText = "Total";
                plotTotal.Color = ScottPlot.Color.FromSDColor(Color.Black);
                plotTotal.LineWidth = 2;
                plotTotal.MarkerSize = 3;
                plotTotal.IsVisible = lineVisibility[4];

                double latestTime = times.Last();
                formsPlot.Plot.Axes.SetLimitsX(latestTime - displaySeconds, latestTime);

                // Manually calculate Y limits based only on visible lines
                var visibleValues = new List<double>();
                if (lineVisibility[0]) visibleValues.AddRange(lc1);
                if (lineVisibility[1]) visibleValues.AddRange(lc2);
                if (lineVisibility[2]) visibleValues.AddRange(lc3);
                if (lineVisibility[3]) visibleValues.AddRange(lc4);
                if (lineVisibility[4]) visibleValues.AddRange(total);

                if (visibleValues.Count > 0)
                {
                    double minY = visibleValues.Min();
                    double maxY = visibleValues.Max();
                    double padding = (maxY - minY) * 0.1; // 10% padding
                    if (padding == 0) padding = Math.Abs(maxY) * 0.1 + 1; // Handle flat lines
                    formsPlot.Plot.Axes.SetLimitsY(minY - padding, maxY + padding);
                }
                else
                {
                    formsPlot.Plot.Axes.AutoScaleY(); // Fallback if no lines visible
                }

                formsPlot.Refresh();
            }

            // Cache latest values to avoid repeated array access
            var lastTime = times.Last();
            var lastLc1 = lc1.Last();
            var lastLc2 = lc2.Last();
            var lastLc3 = lc3.Last();
            var lastLc4 = lc4.Last();
            var lastTotal = total.Last();

            // Batch update labels with cached values
            labelRecord.Text = totalRecordCount.ToString();
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

            // Update status
            labelStatus.Text = currentRate > 0
                ? $"Status: Running — {currentRate:F1} rec/s"
                : "Status: Running — measuring...";
        }
        public void FreezeSession()
        {
            updateTimer.Stop();
            SerialManager.Instance.OnDataReceived -= Instance_OnDataReceived;
        }
        // ==== Mark Zero helpers ====
        public void MarkZero(double now)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double>(MarkZero), now);
                return;
            }

            lock (plotLock)
            {
                var vline = formsPlot.Plot.Add.VerticalLine(now);
                vline.Color = ScottPlot.Color.FromSDColor(Color.Red);
                vline.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                vline.LabelText = $"{now:F2}";
                zeroMarkers.Add(vline);
            }

            // Update zero line with current values when zero calculation finishes
            labelZeroHeaders.Text = totalRecordCount.ToString();
            labelZeroElapsed.Text = now.ToString("F1");

            // Update zero offsets display after manual zero calibration
            UpdateZeroOffsetsDisplay();
        }
        public void MarkZero()
        {
            double now = dataPoints.Count > 0 ? dataPoints.Last().time : 0;
            MarkZero(now);
        }
        // ==== User Actions & Buttons ====
        private void BtnZero_Click(object? sender, EventArgs e)
        {
            SerialManager.Instance.StartManualZeroCalibration((ushort)portId);
            AutoClosingMessage.Show($"Port {portId + 1} re-zeroing started...", SerialManager.Instance.CalibrationSeconds * 1000);
        }
        private void BtnRestart_Click(object? sender, EventArgs e)
        {
            AutoClosingMessage.Show("Restarting...", 1000);
            Program.AppContext?.RestartSession();
        }
        private void BtnExit_Click(object? sender, EventArgs e)
        {
            FreezeSession();
            ShowFullSessionGraphModal();
            AutoClosingMessage.Show("Closing current graphs...", 1000);
            Close();
        }
        private void BtnExitAll_Click(object? sender, EventArgs e)
        {
            AutoClosingMessage.Show("Closing all graphs and showing session summaries...", 1000);
            ShowAllSessionGraphsAndThenExit();
        }
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
        public static void ShowAllSessionGraphsAndThenExit()
        {
            var liveForms = Application.OpenForms.OfType<GraphForm>().ToList();
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
                Width = 110,
                Font = new Font("Segoe UI", 10),
                Height = 120
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
        private static void AddSessionPlot(
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

            var plot1 = plot.Add.Scatter(times, lc1);
            plot1.LegendText = "LC1";
            plot1.MarkerSize = 2;
            plot1.Color = ScottPlot.Color.FromSDColor(Color.Blue);

            var plot2 = plot.Add.Scatter(times, lc2);
            plot2.LegendText = "LC2";
            plot2.MarkerSize = 2;
            plot2.Color = ScottPlot.Color.FromSDColor(Color.Red);

            var plot3 = plot.Add.Scatter(times, lc3);
            plot3.LegendText = "LC3";
            plot3.MarkerSize = 2;
            plot3.Color = ScottPlot.Color.FromSDColor(Color.Green);

            var plot4 = plot.Add.Scatter(times, lc4);
            plot4.LegendText = "LC4";
            plot4.MarkerSize = 2;
            plot4.Color = ScottPlot.Color.FromSDColor(Color.Orange);

            var plotTotal = plot.Add.Scatter(times, total);
            plotTotal.LegendText = "Total";
            plotTotal.Color = ScottPlot.Color.FromSDColor(Color.Black);
            plotTotal.LineWidth = 2;
            plotTotal.MarkerSize = 3;

            foreach (var marker in zeroMarkers)
            {
                var vline = plot.Add.VerticalLine(marker.X);
                vline.Color = ScottPlot.Color.FromSDColor(Color.Red);
                vline.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
                vline.LabelText = $"{marker.X:F2}";
            }

            plot.Title($"Port {portId + 1} Data");
            plot.ShowLegend();
            plot.Legend.Alignment = Alignment.UpperRight;
            plot.Grid.MajorLineColor = ScottPlot.Color.FromSDColor(Color.LightGray);
            plot.Grid.MajorLinePattern = ScottPlot.LinePattern.Dotted;
            plot.FigureBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };
            plot.DataBackground = new ScottPlot.BackgroundStyle { Color = ScottPlot.Color.FromSDColor(Color.White) };

            int totalRecords = recordedData.Count;
            double durationSeconds = totalRecords > 1 ? (recordedData.Last().time - recordedData.First().time) : 0.0;
            double avgRate = durationSeconds > 0 ? totalRecords / durationSeconds : 0.0;

            var summaryText = $"Records: {totalRecords}\nDuration: {durationSeconds:F1} sec\nRate: {avgRate:F1} rec/s";
            var annotation = plot.Add.Annotation(summaryText, ScottPlot.Alignment.UpperCenter);
            annotation.LabelFontSize = 14;
            annotation.LabelFontColor = ScottPlot.Color.FromSDColor(Color.DarkSlateGray);
            annotation.LabelBackgroundColor = ScottPlot.Color.FromSDColor(Color.FromArgb(230, Color.White));
        }
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
                Width = 110,
                Font = new Font("Segoe UI", 9F),
                Height = 120,
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
        // ==== File Saving ====
        private void SaveSessionData()
        {
            string sessionFolder = SerialManager.Instance.SessionFolder;
            if (string.IsNullOrEmpty(sessionFolder)) return;

            string jsonPath = Path.Combine(sessionFolder, $"port_{portId + 1}.json");
            string csvPath = Path.Combine(sessionFolder, $"port_{portId + 1}.csv");
            var jsonData = recordedData.Select(d => new
            {
                time = d.time,
                id = portId,
                weights = d.values,
                totalWeight = d.values.Sum()
            });
            File.WriteAllText(jsonPath, System.Text.Json.JsonSerializer.Serialize(jsonData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            var csvLines = new List<string> { "Time,LC1,LC2,LC3,LC4,Total" };
            csvLines.AddRange(recordedData.Select(d =>
                $"{d.time:F3},{d.values[0]},{d.values[1]},{d.values[2]},{d.values[3]},{d.values.Sum()}"));
            File.WriteAllLines(csvPath, csvLines);

            string graphPath = Path.Combine(sessionFolder, $"port_{portId + 1}.png");
            var plot = new ScottPlot.Plot();
            AddSessionPlot(plot, recordedData, zeroMarkers, portId);
            plot.SavePng(graphPath, 1200, 800);

            AutoClosingMessage.Show($"Session data saved:\n{jsonPath}\n{csvPath}\n{graphPath}");
        }
        // ==== Form Closing, Misc UI ====
        private void GraphForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSessionData();
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
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SerialManager.Instance.OnDataReceived -= Instance_OnDataReceived;
            base.OnFormClosing(e);
        }
        private bool IsCalibrationComplete()
        {
            // You may want to expose a property in SerialManager for this, but for now:
            var sm = SerialManager.Instance;
            var end = sm.GetType().GetField("calibrationEndTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(sm);
            if (end is DateTime endTime)
                return endTime > DateTime.MinValue;
            return false;
        }
        private void StartCalibrationCountdown()
        {
            calibrationCountdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };
            calibrationCountdownTimer.Tick += CalibrationCountdownTimer_Tick;
            calibrationCountdownTimer.Start();
            CalibrationCountdownTimer_Tick(this, EventArgs.Empty); // update immediately
        }
        private void CalibrationCountdownTimer_Tick(object? sender, EventArgs e)
        {
            var sm = SerialManager.Instance;
            int elapsed = (int)(DateTime.Now - sm.CalibrationStartTime).TotalSeconds;
            int remain = sm.CalibrationSeconds - elapsed;
            if (remain <= 0)
            {
                labelStatus.Text = "Status: Calibrating... 0s left";
                labelStatus.ForeColor = Color.Orange;
                calibrationCountdownTimer?.Stop();
                calibrationCountdownTimer?.Dispose();
                calibrationCountdownTimer = null;
            }
            else
            {
                labelStatus.Text = $"Status: Calibrating... {remain}s left";
                labelStatus.ForeColor = Color.Orange;
            }
        }
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
            labelStatus.ForeColor = Color.Green;

            // Update zero offsets display after initial calibration completion
            UpdateZeroOffsetsDisplay();
        }


    }
}
