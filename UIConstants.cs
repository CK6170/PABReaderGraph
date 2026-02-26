using System.Drawing;

namespace PABReaderGraph
{
    /// <summary>
    /// Contains all UI-related constants for consistent styling and layout across the application
    /// Centralized configuration for visual elements, spacing, colors, and timing parameters
    /// Ensures consistent user experience and simplifies theme management
    /// </summary>
    public static class UIConstants
    {
        /// <summary>
        /// Layout constants defining spacing, positioning, and sizing for UI elements
        /// Provides pixel-perfect control over form layout and data table alignment
        /// </summary>
        public static class Layout
        {
            /// <summary>
            /// Height of the top data display panel containing all data rows
            /// Sized to accommodate Weight, ADC, Zero, Factor, and Header rows
            /// </summary>
            public const int PanelTopHeight = 120;

            /// <summary>
            /// Standard height for each data row in the top panel
            /// Consistent spacing for Weight, ADC, Zero, and Factor display rows
            /// </summary>
            public const int PanelRowHeight = 20;

            /// <summary>
            /// Standard width for data display labels in columnar layout
            /// Ensures consistent column sizing across all data rows
            /// </summary>
            public const int LabelWidth = 100;

            /// <summary>
            /// Width of the line visibility filter checkbox list
            /// Sized to accommodate LC1-LC4 and Total filter options
            /// </summary>
            public const int FilterBoxWidth = 100;

            /// <summary>
            /// Left position offset for the filter box from panel edge
            /// Provides small margin from panel border for visual clarity
            /// </summary>
            public const int FilterBoxLeft = 5;

            /// <summary>
            /// Top position offset for the filter box from panel top
            /// Aligns with header row for proper visual relationship
            /// </summary>
            public const int FilterBoxTop = 10;

            /// <summary>
            /// Height of the filter box to display all filtering options
            /// Sized to show LC1, LC2, LC3, LC4, and Total checkboxes
            /// </summary>
            public const int FilterBoxHeight = 100;

            /// <summary>
            /// Standard height for action buttons (Zero, Restart, Save, Exit)
            /// Consistent button sizing for professional appearance
            /// </summary>
            public const int ButtonHeight = 30;

            /// <summary>
            /// Standard width for action buttons in the bottom panel
            /// Balanced sizing for button text and visual consistency
            /// </summary>
            public const int ButtonWidth = 100;

            // Column positions for tabular data layout
            /// <summary>Left position for the Records count column</summary>
            public const int RecordColumn = 110;
            /// <summary>Left position for the Elapsed time column</summary>
            public const int ElapsedColumn = 209;
            /// <summary>Left position for the data Type column (Weight/ADC/Zero/Factor)</summary>
            public const int TypeColumn = 308;
            /// <summary>Left position for Load Cell 1 data column</summary>
            public const int LC1Column = 407;
            /// <summary>Left position for Load Cell 2 data column</summary>
            public const int LC2Column = 506;
            /// <summary>Left position for Load Cell 3 data column</summary>
            public const int LC3Column = 605;
            /// <summary>Left position for Load Cell 4 data column</summary>
            public const int LC4Column = 704;
            /// <summary>Left position for the Total weight column</summary>
            public const int TotalColumn = 803;
        }

        /// <summary>
        /// Plot and graph-related timing and visual constants
        /// Controls real-time display behavior and plot appearance settings
        /// </summary>
        public static class Plot
        {
            /// <summary>
            /// Timer interval in milliseconds for plot updates and data refresh
            /// Balances responsiveness with performance (10 FPS update rate)
            /// </summary>
            public const int UpdateInterval = 100;

            /// <summary>
            /// Time window in seconds for live data display
            /// Shows most recent data points within this sliding window
            /// </summary>
            public const int DisplaySeconds = 10;

            /// <summary>
            /// Standard marker size for individual load cell data points
            /// Optimized for visibility without cluttering the display
            /// </summary>
            public const int MarkerSize = 2;

            /// <summary>
            /// Enhanced line width for the Total weight plot line
            /// Makes the combined measurement more visually prominent
            /// </summary>
            public const int TotalLineWidth = 2;

            /// <summary>
            /// Enhanced marker size for Total weight data points
            /// Emphasizes the combined measurement for better visibility
            /// </summary>
            public const int TotalMarkerSize = 3;

            /// <summary>
            /// Vertical padding percentage for automatic Y-axis scaling
            /// Adds 10% padding above and below data range for visual clarity
            /// </summary>
            public const double YPaddingPercent = 0.1;
        }

        /// <summary>
        /// Typography constants for consistent text rendering across the application
        /// Defines font families and sizes for different UI element types
        /// </summary>
        public static class Fonts
        {
            /// <summary>
            /// Primary monospace font family for data display
            /// Ensures consistent character spacing for numerical data alignment
            /// </summary>
            public const string DefaultFontFamily = "Consolas";

            /// <summary>
            /// Standard font size for data labels and primary UI text
            /// Optimized for readability while maintaining compact layout
            /// </summary>
            public const float DefaultFontSize = 12F;

            /// <summary>
            /// Reduced font size for secondary information and help text
            /// Used for status hints and non-critical interface elements
            /// </summary>
            public const float SmallFontSize = 9F;
        }

        /// <summary>
        /// Comprehensive color scheme for all UI elements with semantic organization
        /// Provides consistent visual hierarchy through structured color relationships
        /// Colors are organized by brightness levels for different data types
        /// </summary>
        public static class Colors
        {
            // Live data colors (brightest) - Primary real-time display
            /// <summary>Load Cell 1 color for live weight display (bright blue)</summary>
            public static readonly Color LC1Live = Color.Blue;
            /// <summary>Load Cell 2 color for live weight display (bright red)</summary>
            public static readonly Color LC2Live = Color.Red;
            /// <summary>Load Cell 3 color for live weight display (bright green)</summary>
            public static readonly Color LC3Live = Color.Green;
            /// <summary>Load Cell 4 color for live weight display (bright orange)</summary>
            public static readonly Color LC4Live = Color.Orange;
            /// <summary>Total weight color for combined measurement display (black)</summary>
            public static readonly Color TotalLive = Color.Black;

            // ADC colors (darkest) - Raw sensor values
            /// <summary>Load Cell 1 ADC color for raw sensor display (dark blue)</summary>
            public static readonly Color LC1Adc = Color.FromArgb(25, 25, 112);
            /// <summary>Load Cell 2 ADC color for raw sensor display (dark red)</summary>
            public static readonly Color LC2Adc = Color.FromArgb(128, 0, 0);
            /// <summary>Load Cell 3 ADC color for raw sensor display (dark green)</summary>
            public static readonly Color LC3Adc = Color.FromArgb(0, 80, 0);
            /// <summary>Load Cell 4 ADC color for raw sensor display (dark orange)</summary>
            public static readonly Color LC4Adc = Color.FromArgb(204, 85, 0);

            // Zero colors (medium) - Calibration offset values
            /// <summary>Load Cell 1 zero offset color for calibration display (medium blue)</summary>
            public static readonly Color LC1Zero = Color.FromArgb(100, 149, 237);
            /// <summary>Load Cell 2 zero offset color for calibration display (medium red)</summary>
            public static readonly Color LC2Zero = Color.FromArgb(220, 20, 60);
            /// <summary>Load Cell 3 zero offset color for calibration display (medium green)</summary>
            public static readonly Color LC3Zero = Color.FromArgb(60, 179, 113);
            /// <summary>Load Cell 4 zero offset color for calibration display (medium orange)</summary>
            public static readonly Color LC4Zero = Color.FromArgb(255, 140, 0);

            // Factor colors (lightest) - Calibration scaling factors
            /// <summary>Load Cell 1 factor color for calibration scaling display (light blue)</summary>
            public static readonly Color LC1Factor = Color.FromArgb(135, 206, 250);
            /// <summary>Load Cell 2 factor color for calibration scaling display (light red)</summary>
            public static readonly Color LC2Factor = Color.FromArgb(255, 160, 160);
            /// <summary>Load Cell 3 factor color for calibration scaling display (light green)</summary>
            public static readonly Color LC3Factor = Color.FromArgb(152, 251, 152);
            /// <summary>Load Cell 4 factor color for calibration scaling display (light orange)</summary>
            public static readonly Color LC4Factor = Color.FromArgb(255, 222, 173);

            // Helper methods to get coordinated color arrays
            /// <summary>
            /// Gets array of live data colors in LC1-LC4, Total order
            /// Used for primary real-time data visualization
            /// </summary>
            /// <returns>Array of 5 colors for live data display</returns>
            public static Color[] GetLiveColors() => new[] { LC1Live, LC2Live, LC3Live, LC4Live, TotalLive };

            /// <summary>
            /// Gets array of ADC colors in LC1-LC4 order  
            /// Used for raw sensor value visualization
            /// </summary>
            /// <returns>Array of 4 colors for ADC data display</returns>
            public static Color[] GetAdcColors() => new[] { LC1Adc, LC2Adc, LC3Adc, LC4Adc };

            /// <summary>
            /// Gets array of zero offset colors in LC1-LC4 order
            /// Used for calibration offset value visualization  
            /// </summary>
            /// <returns>Array of 4 colors for zero offset display</returns>
            public static Color[] GetZeroColors() => new[] { LC1Zero, LC2Zero, LC3Zero, LC4Zero };

            /// <summary>
            /// Gets array of calibration factor colors in LC1-LC4 order
            /// Used for scaling factor visualization
            /// </summary>
            /// <returns>Array of 4 colors for factor display</returns>
            public static Color[] GetFactorColors() => new[] { LC1Factor, LC2Factor, LC3Factor, LC4Factor };

            // UI element colors for status and interface components
            /// <summary>Standard color for header text and labels</summary>
            public static readonly Color HeaderText = Color.Black;
            /// <summary>Muted color for disabled or secondary text elements</summary>
            public static readonly Color DisabledText = Color.DarkGray;
            /// <summary>Background color for filter selection controls</summary>
            public static readonly Color FilterBoxBackground = Color.FromArgb(240, 240, 240);
            /// <summary>Status color indicating calibration in progress</summary>
            public static readonly Color StatusCalibrating = Color.Orange;
            /// <summary>Status color indicating system ready for operation</summary>
            public static readonly Color StatusReady = Color.Green;
            /// <summary>Color for zero calibration markers on plots</summary>
            public static readonly Color ZeroMarker = Color.Red;
        }
    }
}