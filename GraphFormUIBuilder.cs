using System.Drawing;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Handles UI construction for GraphForm to separate concerns and improve maintainability
    /// Implements the Builder pattern to construct complex form layouts with consistent styling
    /// Separates UI creation logic from business logic for better code organization and testing
    /// 
    /// Architecture Benefits:
    /// - Single Responsibility: Focuses only on UI construction and layout
    /// - Maintainability: UI changes isolated from form logic
    /// - Reusability: Layout patterns can be extracted and reused
    /// - Testability: UI construction can be tested independently
    /// 
    /// Layout Structure:
    /// - Main plot area (ScottPlot) for real-time data visualization
    /// - Top data panel with tabular display (Weight/ADC/Zero/Factor rows)
    /// - Bottom button panel with action controls and status display
    /// - Keyboard shortcut hints for improved user experience
    /// </summary>
    public class GraphFormUIBuilder
    {
        private readonly GraphForm _form;
        private readonly int _portId;

        /// <summary>
        /// Initializes a new UIBuilder instance for the specified GraphForm
        /// Captures form reference and port context for consistent UI construction
        /// </summary>
        /// <param name="form">Target GraphForm instance to build UI for</param>
        /// <param name="portId">Zero-based port identifier for contextual labeling</param>
        public GraphFormUIBuilder(GraphForm form, int portId)
        {
            _form = form;
            _portId = portId;
        }

        /// <summary>
        /// Main orchestration method that builds the complete form layout
        /// Coordinates all UI construction phases in the correct order for proper control layering
        /// Establishes the foundation layout that other managers can then populate with data
        /// </summary>
        public void SetupLayout()
        {
            SetupMainPlot();
            SetupDataPanels();
            SetupButtonPanel();
            SetupShortcutLabel();
        }

        /// <summary>
        /// Creates and configures the main ScottPlot control for real-time data visualization
        /// Establishes the primary display area that will be managed by PlotManager
        /// Uses dock fill to maximize available space for data viewing
        /// </summary>
        private void SetupMainPlot()
        {
            _form.formsPlot = new ScottPlot.WinForms.FormsPlot { Dock = DockStyle.Fill };
            _form.Controls.Add(_form.formsPlot);
        }

        /// <summary>
        /// Constructs the comprehensive data display panels with tabular layout
        /// Creates a structured hierarchy: Top Panel → Weight/ADC/Zero/Factor rows → Headers/Filters
        /// Implements consistent color coding and spacing for professional data presentation
        /// </summary>
        private void SetupDataPanels()
        {
            _form.panelTop = new Panel { Dock = DockStyle.Bottom, Height = UIConstants.Layout.PanelTopHeight };
            _form.Controls.Add(_form.panelTop);

            CreateWeightPanel();
            CreateAdcPanel();
            CreateZeroPanel();
            CreateFactorPanel();
            CreateHeaderPanel();
            CreateFilterBox();
        }

        /// <summary>
        /// Creates the primary weight data display panel with live measurement values
        /// Establishes the main data row showing current readings from all load cells
        /// Uses bright colors from UIConstants for maximum visibility and user attention
        /// </summary>
        private void CreateWeightPanel()
        {
            _form.labelValuesPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = UIConstants.Layout.PanelRowHeight
            };
            _form.panelTop.Controls.Add(_form.labelValuesPanel);

            var liveColors = UIConstants.Colors.GetLiveColors();

            // Create labels using helper method from form
            _form.labelRecord = CreateStandardLabel(UIConstants.Layout.RecordColumn, UIConstants.Colors.HeaderText, "0", ContentAlignment.MiddleCenter);
            _form.labelElapsed = CreateStandardLabel(UIConstants.Layout.ElapsedColumn, UIConstants.Colors.HeaderText, "0.0", ContentAlignment.MiddleCenter);
            var labelType = CreateStandardLabel(UIConstants.Layout.TypeColumn, UIConstants.Colors.HeaderText, "Weight", ContentAlignment.MiddleCenter);
            
            _form.labelLC1 = CreateStandardLabel(UIConstants.Layout.LC1Column, liveColors[0], "0");
            _form.labelLC2 = CreateStandardLabel(UIConstants.Layout.LC2Column, liveColors[1], "0");
            _form.labelLC3 = CreateStandardLabel(UIConstants.Layout.LC3Column, liveColors[2], "0");
            _form.labelLC4 = CreateStandardLabel(UIConstants.Layout.LC4Column, liveColors[3], "0");
            _form.labelTotal = CreateStandardLabel(UIConstants.Layout.TotalColumn, liveColors[4], "0");

            _form.labelValuesPanel.Controls.AddRange(new Control[] { 
                _form.labelRecord, _form.labelElapsed, labelType, 
                _form.labelLC1, _form.labelLC2, _form.labelLC3, _form.labelLC4, _form.labelTotal 
            });
        }

        /// <summary>
        /// Creates the ADC (Analog-to-Digital Converter) values display panel
        /// Shows raw sensor readings for technical analysis and troubleshooting
        /// Uses darker color scheme to distinguish from primary weight display
        /// </summary>
        private void CreateAdcPanel()
        {
            var panelAdc = new Panel { Height = UIConstants.Layout.PanelRowHeight, Dock = DockStyle.Top };
            var adcColors = UIConstants.Colors.GetAdcColors();

            var labelAdcHeaders = CreateStandardLabel(UIConstants.Layout.RecordColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleLeft);
            var labelAdcElapsed = CreateStandardLabel(UIConstants.Layout.ElapsedColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleCenter);
            var labelAdcType = CreateStandardLabel(UIConstants.Layout.TypeColumn, Color.FromArgb(50, 50, 80), "ADC", ContentAlignment.MiddleCenter);

            _form.labelAdcLC1 = CreateStandardLabel(UIConstants.Layout.LC1Column, adcColors[0], "0");
            _form.labelAdcLC2 = CreateStandardLabel(UIConstants.Layout.LC2Column, adcColors[1], "0");
            _form.labelAdcLC3 = CreateStandardLabel(UIConstants.Layout.LC3Column, adcColors[2], "0");
            _form.labelAdcLC4 = CreateStandardLabel(UIConstants.Layout.LC4Column, adcColors[3], "0");
            _form.labelAdcTotal = CreateStandardLabel(UIConstants.Layout.TotalColumn, Color.LightGray, "");

            panelAdc.Controls.AddRange(new Control[] { 
                labelAdcHeaders, labelAdcElapsed, labelAdcType,
                _form.labelAdcLC1, _form.labelAdcLC2, _form.labelAdcLC3, _form.labelAdcLC4, _form.labelAdcTotal 
            });
            _form.panelTop.Controls.Add(panelAdc);
        }

        /// <summary>
        /// Creates the zero offset calibration values display panel
        /// Shows the baseline offset values used for zero calibration calculations
        /// Uses medium-toned colors to indicate calibration data hierarchy
        /// </summary>
        private void CreateZeroPanel()
        {
            var panelZero = new Panel { Height = UIConstants.Layout.PanelRowHeight, Dock = DockStyle.Top };
            var zeroColors = UIConstants.Colors.GetZeroColors();

            _form.labelZeroHeaders = CreateStandardLabel(UIConstants.Layout.RecordColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleCenter);
            _form.labelZeroElapsed = CreateStandardLabel(UIConstants.Layout.ElapsedColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleCenter);
            var labelZeroType = CreateStandardLabel(UIConstants.Layout.TypeColumn, Color.FromArgb(80, 100, 120), "Zero", ContentAlignment.MiddleCenter);

            _form.labelZeroLC1 = CreateStandardLabel(UIConstants.Layout.LC1Column, zeroColors[0], "0");
            _form.labelZeroLC2 = CreateStandardLabel(UIConstants.Layout.LC2Column, zeroColors[1], "0");
            _form.labelZeroLC3 = CreateStandardLabel(UIConstants.Layout.LC3Column, zeroColors[2], "0");
            _form.labelZeroLC4 = CreateStandardLabel(UIConstants.Layout.LC4Column, zeroColors[3], "0");
            _form.labelZeroTotal = CreateStandardLabel(UIConstants.Layout.TotalColumn, Color.Gray, "");

            panelZero.Controls.AddRange(new Control[] { 
                _form.labelZeroHeaders, _form.labelZeroElapsed, labelZeroType,
                _form.labelZeroLC1, _form.labelZeroLC2, _form.labelZeroLC3, _form.labelZeroLC4, _form.labelZeroTotal 
            });
            _form.panelTop.Controls.Add(panelZero);
        }

        /// <summary>
        /// Creates the calibration scaling factors display panel
        /// Shows the mathematical scaling factors used to convert ADC values to weights
        /// Uses lightest color scheme and italic styling to indicate derived calibration data
        /// </summary>
        private void CreateFactorPanel()
        {
            var panelFactors = new Panel { Height = UIConstants.Layout.PanelRowHeight, Dock = DockStyle.Top };
            var factorColors = UIConstants.Colors.GetFactorColors();

            var labelFactorHeaders = CreateStandardLabel(UIConstants.Layout.RecordColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleLeft, FontStyle.Italic);
            var labelFactorElapsed = CreateStandardLabel(UIConstants.Layout.ElapsedColumn, UIConstants.Colors.DisabledText, " ", ContentAlignment.MiddleCenter, FontStyle.Italic);
            var labelFactorType = CreateStandardLabel(UIConstants.Layout.TypeColumn, Color.FromArgb(150, 170, 190), "Factor", ContentAlignment.MiddleCenter, FontStyle.Italic);

            _form.labelFactorLC1 = CreateStandardLabel(UIConstants.Layout.LC1Column, factorColors[0], "0.000000", ContentAlignment.MiddleRight, FontStyle.Italic);
            _form.labelFactorLC2 = CreateStandardLabel(UIConstants.Layout.LC2Column, factorColors[1], "0.000000", ContentAlignment.MiddleRight, FontStyle.Italic);
            _form.labelFactorLC3 = CreateStandardLabel(UIConstants.Layout.LC3Column, factorColors[2], "0.000000", ContentAlignment.MiddleRight, FontStyle.Italic);
            _form.labelFactorLC4 = CreateStandardLabel(UIConstants.Layout.LC4Column, factorColors[3], "0.000000", ContentAlignment.MiddleRight, FontStyle.Italic);
            _form.labelFactorTotal = CreateStandardLabel(UIConstants.Layout.TotalColumn, UIConstants.Colors.DisabledText, "0.000000", ContentAlignment.MiddleRight, FontStyle.Italic);

            panelFactors.Controls.AddRange(new Control[] { 
                labelFactorHeaders, labelFactorElapsed, labelFactorType,
                _form.labelFactorLC1, _form.labelFactorLC2, _form.labelFactorLC3, _form.labelFactorLC4, _form.labelFactorTotal 
            });
            _form.panelTop.Controls.Add(panelFactors);
        }

        /// <summary>
        /// Creates the column header panel providing labels for the tabular data layout
        /// Establishes clear identification for each data column across all display rows
        /// Uses bold styling to create visual hierarchy and improve data table readability
        /// </summary>
        private void CreateHeaderPanel()
        {
            var panelHeaders = new Panel { Height = UIConstants.Layout.PanelRowHeight, Dock = DockStyle.Top };

            var headerLabels = new[]
            {
                CreateHeaderLabel(UIConstants.Layout.RecordColumn, "Records"),
                CreateHeaderLabel(UIConstants.Layout.ElapsedColumn, "Elapsed"),
                CreateHeaderLabel(UIConstants.Layout.TypeColumn, "Type"),
                CreateHeaderLabel(UIConstants.Layout.LC1Column, "LC1"),
                CreateHeaderLabel(UIConstants.Layout.LC2Column, "LC2"),
                CreateHeaderLabel(UIConstants.Layout.LC3Column, "LC3"),
                CreateHeaderLabel(UIConstants.Layout.LC4Column, "LC4"),
                CreateHeaderLabel(UIConstants.Layout.TotalColumn, "Total")
            };

            panelHeaders.Controls.AddRange(headerLabels);
            _form.panelTop.Controls.Add(panelHeaders);
        }

        /// <summary>
        /// Creates the line visibility filter control for real-time plot customization
        /// Provides checkboxes for toggling individual load cell and total weight displays
        /// Positioned for easy access while maintaining clean layout aesthetics
        /// </summary>
        private void CreateFilterBox()
        {
            _form.filterBox = new CheckedListBox
            {
                Width = UIConstants.Layout.FilterBoxWidth,
                Left = UIConstants.Layout.FilterBoxLeft,
                Height = UIConstants.Layout.FilterBoxHeight,
                Top = UIConstants.Layout.FilterBoxTop,
                Font = new Font("Segoe UI", UIConstants.Fonts.SmallFontSize),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UIConstants.Colors.FilterBoxBackground,
                CheckOnClick = true,
                IntegralHeight = false,
                Visible = true
            };

            _form.filterBox.Items.AddRange(new object[] { "LC1", "LC2", "LC3", "LC4", "Total" });
            for (int i = 0; i < _form.filterBox.Items.Count; i++)
                _form.filterBox.SetItemChecked(i, true);

            _form.panelTop.Controls.Add(_form.filterBox);
            _form.filterBox.BringToFront();
        }

        /// <summary>
        /// Creates the bottom action button panel with status display
        /// Establishes user control interface for common operations and system feedback
        /// Implements proper control creation order to ensure status label availability
        /// </summary>
        private void SetupButtonPanel()
        {
            _form.panelBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            _form.Controls.Add(_form.panelBottom);

            // Create status label BEFORE CreateButtons() so it can be added to controls
            _form.labelStatus = new Label { 
                Text = "", // Let GraphForm set the initial status
                AutoSize = true, 
                Margin = new Padding(50, 10, 0, 0)
            };

            CreateButtons();

            _form.Font = new Font(UIConstants.Fonts.DefaultFontFamily, UIConstants.Fonts.DefaultFontSize, FontStyle.Regular);
        }

        /// <summary>
        /// Creates all action buttons with consistent sizing and event wiring
        /// Establishes primary user interface controls for session management
        /// Includes automatic event binding for the Save button functionality
        /// </summary>
        private void CreateButtons()
        {
            _form.buttonZero = new Button { Text = "Zero", Width = UIConstants.Layout.ButtonWidth, Height = UIConstants.Layout.ButtonHeight };
            _form.buttonRestart = new Button { Text = "Restart", Width = UIConstants.Layout.ButtonWidth, Height = UIConstants.Layout.ButtonHeight };
            var buttonSave = new Button { Text = "Save", Width = UIConstants.Layout.ButtonWidth, Height = UIConstants.Layout.ButtonHeight };
            buttonSave.Click += (s, e) => _form.SaveSessionData(); // Wire up save button
            _form.buttonExit = new Button { Text = "Exit", Width = UIConstants.Layout.ButtonWidth, Height = UIConstants.Layout.ButtonHeight };
            _form.buttonExitAll = new Button { Text = "Exit All", Width = UIConstants.Layout.ButtonWidth, Height = UIConstants.Layout.ButtonHeight };

            _form.panelBottom.Controls.AddRange(new Control[] { 
                _form.buttonZero, _form.buttonRestart, buttonSave, _form.buttonExit, _form.buttonExitAll, _form.labelStatus 
            });
        }

        /// <summary>
        /// Creates informational label displaying keyboard shortcuts for improved user experience
        /// Provides quick reference for power users and improves discoverability of hotkeys
        /// Uses subtle styling to inform without cluttering the interface
        /// </summary>
        private void SetupShortcutLabel()
        {
            var shortcutLabel = new Label
            {
                Text = "Shortcuts: Z=Zero, R=Restart, S=Save, X=Exit, A=Exit All",
                Dock = DockStyle.Bottom,
                Font = new Font(UIConstants.Fonts.DefaultFontFamily, UIConstants.Fonts.SmallFontSize, FontStyle.Italic),
                ForeColor = UIConstants.Colors.DisabledText
            };
            _form.Controls.Add(shortcutLabel);
        }

        /// <summary>
        /// Factory method for creating consistently styled data display labels
        /// Provides standardized label creation with configurable positioning, colors, and alignment
        /// Ensures visual consistency across all data display elements in the form
        /// </summary>
        /// <param name="left">X position for label placement</param>
        /// <param name="color">Text color for the label content</param>
        /// <param name="initialText">Initial display text (default: empty)</param>
        /// <param name="alignment">Text alignment within label bounds (default: MiddleRight)</param>
        /// <param name="fontStyle">Font styling options (default: Regular)</param>
        /// <returns>Configured Label control ready for use</returns>
        private Label CreateStandardLabel(int left, Color color, string initialText, ContentAlignment alignment = ContentAlignment.MiddleRight, FontStyle fontStyle = FontStyle.Regular)
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
        /// Factory method for creating column header labels with bold styling
        /// Provides consistent header creation for the tabular data display layout
        /// Ensures proper visual hierarchy and professional appearance for data tables
        /// </summary>
        /// <param name="left">X position for header label placement</param>
        /// <param name="text">Header text to display</param>
        /// <returns>Configured header Label with bold styling and center alignment</returns>
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
    }
}