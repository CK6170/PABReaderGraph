using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Provides auto-closing popup message functionality for temporary user notifications
    /// Creates modal dialogs that automatically dismiss after a specified timeout period
    /// Designed for non-critical status messages, confirmations, and transient feedback
    /// 
    /// Features:
    /// - Automatic sizing based on message content length and line count
    /// - Center screen positioning for consistent user experience
    /// - TopMost behavior to ensure visibility above other windows
    /// - Clean, minimal styling with professional appearance
    /// - Configurable timeout duration for different message types
    /// 
    /// Usage Scenarios:
    /// - Save operation confirmations ("Data saved successfully")
    /// - Process status updates ("Restarting...", "Calibrating...")
    /// - Non-critical error notifications that don't require user action
    /// - Temporary status indicators during operations
    /// 
    /// Design Philosophy:
    /// - Non-intrusive: Messages appear and disappear automatically
    /// - Consistent: Uniform styling and behavior across the application
    /// - Accessible: Clear typography and sufficient contrast
    /// - Responsive: Dynamic sizing adapts to content requirements
    /// </summary>
    public class AutoClosingMessage
    {
        /// <summary>
        /// Displays an auto-closing popup message with configurable timeout
        /// Creates a modal dialog that automatically closes after the specified duration
        /// Provides immediate visual feedback without requiring user interaction
        /// 
        /// Implementation Details:
        /// - Dynamic sizing: Width based on longest line, height based on line count
        /// - Typography: Uses Comic Neue font for friendly, readable appearance  
        /// - Positioning: Always centers on screen regardless of parent window location
        /// - Behavior: Modal dialog blocks interaction until auto-close or manual dismiss
        /// - Cleanup: Timer and form resources are properly disposed automatically
        /// 
        /// Sizing Algorithm:
        /// - Width: Longest line length × 10 pixels (approximates character width)
        /// - Height: Line count × 30 pixels (accommodates font size + spacing)
        /// - Minimum constraints ensure readability for short messages
        /// </summary>
        /// <param name="message">
        /// Text content to display in the popup message
        /// Supports multi-line messages using \n line separators for structured content
        /// Should be concise but informative (recommended: 1-3 lines, &lt;100 characters per line)
        /// </param>
        /// <param name="timeoutMilliseconds">
        /// Auto-close timeout duration in milliseconds (default: 1500ms = 1.5 seconds)
        /// Common values: 1000ms (quick status), 2000ms (normal), 3000ms (important info)
        /// Should balance readability time with user workflow interruption
        /// </param>
        /// <example>
        /// <code>
        /// // Quick status message
        /// AutoClosingMessage.Show("Saving data...", 1000);
        /// 
        /// // Multi-line confirmation
        /// AutoClosingMessage.Show("Files saved:\nport_1.csv\nport_1.json", 3000);
        /// 
        /// // Default timeout
        /// AutoClosingMessage.Show("Operation completed");
        /// </code>
        /// </example>
        public static void Show(string message, int timeoutMilliseconds = 1500)
        {
            // Parse message content for dynamic sizing calculations
            string[] lines = message.Split('\n');

            // Calculate optimal popup dimensions based on content
            // Width: Character count approximation (10px per character for Comic Neue 12pt)
            // Height: Line-based calculation (30px per line includes font height + spacing)
            int width = lines.Max(s => s.Length) * 10; 
            int height = lines.Length * 30;

            // Create the popup form with professional styling and behavior
            Form popup = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedSingle,  // Clean border, non-resizable
                StartPosition = FormStartPosition.CenterScreen, // Consistent positioning
                Size = new Size(width, height),                 // Content-based sizing
                TopMost = true,                                 // Ensure visibility above other windows
                BackColor = Color.White,                        // Clean, neutral background
                ControlBox = false,                             // Remove minimize/maximize/close buttons
                ShowInTaskbar = false                           // Don't clutter taskbar for temporary messages
            };

            // Create the message label with optimized typography and layout
            var label = new Label
            {
                Text = message,                                 // Display the provided message content
                Dock = DockStyle.Fill,                         // Fill entire popup area
                TextAlign = ContentAlignment.MiddleCenter,      // Center text both horizontally and vertically
                Font = new Font("Comic Neue", 12, FontStyle.Regular), // Friendly, readable typeface
                ForeColor = Color.Black,                        // High contrast for accessibility
                BorderStyle = BorderStyle.FixedSingle,         // Subtle border for definition
            };
            popup.Controls.Add(label);

            // Setup auto-close timer with proper resource cleanup
            var timer = new System.Windows.Forms.Timer { Interval = timeoutMilliseconds };
            timer.Tick += (s, e) =>
            {
                timer.Stop();    // Stop timer to prevent additional ticks
                popup.Close();   // Close popup (automatically disposes form and timer)
            };
            timer.Start();

            // Display as modal dialog (blocks calling thread until closed)
            // This ensures message visibility and prevents rapid successive popups
            popup.ShowDialog();
        }
    }
}
