using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Represents the result of a custom message box user interaction
    /// Provides type-safe enumeration of possible user responses to dialog prompts
    /// Used to determine user choice and control application flow after dialog dismissal
    /// </summary>
    public enum CustomMessageBoxResult
    {
        /// <summary>User clicked the OK button, indicating acceptance or acknowledgment</summary>
        Ok,
        /// <summary>User clicked the Cancel button, indicating rejection or cancellation</summary>
        Cancel,
        /// <summary>User clicked the Retry button, indicating desire to retry the operation</summary>
        Retry,
        /// <summary>No user action taken or dialog closed without button interaction</summary>
        None
    }

    /// <summary>
    /// Defines the button combinations available in custom message boxes
    /// Controls which action buttons are displayed and their associated behaviors
    /// Supports common dialog patterns for user decision making
    /// </summary>
    public enum CustomMessageBoxButtons
    {
        /// <summary>Display only an OK button for acknowledgment scenarios</summary>
        OK,
        /// <summary>Display OK and Cancel buttons for accept/reject scenarios</summary>
        OKCancel,
        /// <summary>Display Retry and Cancel buttons for error recovery scenarios</summary>
        RetryCancel
    }

    /// <summary>
    /// Defines the icon types available for custom message boxes
    /// Provides visual context and semantic meaning to dialog messages
    /// Uses standard Windows system icons for consistency and recognition
    /// </summary>
    public enum CustomMessageBoxIcon
    {
        /// <summary>No icon displayed, clean text-only presentation</summary>
        None,
        /// <summary>Error icon (red X) for critical errors and failures</summary>
        Error,
        /// <summary>Warning icon (yellow triangle) for cautions and potential issues</summary>
        Warning,
        /// <summary>Information icon (blue i) for informational messages</summary>
        Information,
        /// <summary>Question icon (blue ?) for user confirmation prompts</summary>
        Question
    }

    /// <summary>
    /// Custom message box implementation with enhanced styling and functionality
    /// Provides a modern, consistent alternative to standard Windows MessageBox
    /// Features automatic sizing, professional appearance, and flexible configuration
    /// 
    /// Key Features:
    /// - Dynamic sizing based on message content length and line count
    /// - Professional styling with Comic Neue typography for improved readability
    /// - System icon integration for semantic visual context
    /// - Flexible button configurations for various user interaction patterns
    /// - Center screen positioning with TopMost behavior for visibility
    /// - Proper resource management and cleanup
    /// 
    /// Design Philosophy:
    /// - Consistent: Uniform appearance across all application dialogs
    /// - Professional: Clean, modern styling with proper spacing and typography
    /// - Flexible: Configurable buttons, icons, and sizing for different scenarios
    /// - Accessible: High contrast, readable fonts, and standard icon usage
    /// - Reliable: Proper resource disposal and error handling
    /// 
    /// Usage Scenarios:
    /// - Error notifications with retry functionality (serial port failures)
    /// - Warning messages for missing hardware or configuration issues
    /// - User confirmation prompts for critical operations
    /// - Informational displays with acknowledgment requirements
    /// 
    /// Advantages over Standard MessageBox:
    /// - Consistent branding and styling throughout application
    /// - Better typography and spacing for improved readability
    /// - More precise sizing control for various message lengths
    /// - Enhanced visual hierarchy with proper icon placement
    /// </summary>
    public static class CustomMessageBox
    {
        /// <summary>
        /// Displays a custom message box with configurable content, buttons, and styling
        /// Creates a modal dialog that blocks execution until user interaction or timeout
        /// Automatically calculates optimal sizing based on content and provides professional styling
        /// 
        /// Sizing Algorithm:
        /// - Width: Based on longest line length (9px per character) plus margins and icon space
        /// - Height: Dynamically measured using TextRenderer for accurate text bounds
        /// - Minimum constraints ensure readability for short messages
        /// - Custom width/height can override automatic calculations when needed
        /// 
        /// Layout Structure:
        /// - Border panel with FixedSingle style for clean definition
        /// - Content area with icon (if specified) and message text
        /// - Bottom button panel with centered button arrangement
        /// - Proper spacing and margins for professional appearance
        /// 
        /// Typography and Styling:
        /// - Comic Neue font family for friendly, readable appearance
        /// - 12pt regular weight for message text, bold for buttons
        /// - High contrast colors (black text on white background)
        /// - WhiteSmoke button backgrounds for subtle visual hierarchy
        /// 
        /// Behavior:
        /// - Modal display blocks calling thread until dismissed
        /// - TopMost ensures visibility above other application windows
        /// - Automatic keyboard shortcuts (Enter for Accept, Escape for Cancel)
        /// - Proper resource cleanup through using statements
        /// </summary>
        /// <param name="message">
        /// Text content to display in the message box
        /// Supports multi-line messages using \n line separators
        /// Message length affects automatic width calculation
        /// Should be clear, concise, and actionable for best user experience
        /// </param>
        /// <param name="title">
        /// Title bar text for the message box window (default: empty string)
        /// Currently not displayed due to ControlBox = false styling
        /// Reserved for future enhancement or debugging purposes
        /// </param>
        /// <param name="buttons">
        /// Button configuration determining available user actions (default: OK)
        /// Controls which buttons appear and their associated return values
        /// Affects dialog layout and keyboard shortcut assignments
        /// </param>
        /// <param name="icon">
        /// Icon type to display alongside message text (default: None)
        /// Provides visual context and semantic meaning to the message
        /// Uses standard Windows system icons for consistency
        /// Affects automatic width calculation and layout positioning
        /// </param>
        /// <param name="width">
        /// Optional custom width override in pixels (default: automatic calculation)
        /// When specified, overrides automatic width calculation based on content
        /// Should accommodate message text and icon with adequate margins
        /// Minimum width constraints still apply for usability
        /// </param>
        /// <param name="height">
        /// Optional custom height override in pixels (default: automatic calculation)
        /// When specified, overrides automatic height calculation based on content
        /// Should accommodate message text, icon, and button panel
        /// Minimum height constraints still apply for usability
        /// </param>
        /// <returns>
        /// CustomMessageBoxResult indicating user's choice or interaction
        /// Ok: User clicked OK button or pressed Enter
        /// Cancel: User clicked Cancel button or pressed Escape
        /// Retry: User clicked Retry button
        /// None: Dialog closed without user interaction (should not occur in normal usage)
        /// </returns>
        /// <example>
        /// <code>
        /// // Simple informational message
        /// CustomMessageBox.Show("Operation completed successfully.", "Success", 
        ///     CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
        /// 
        /// // Error with retry option
        /// var result = CustomMessageBox.Show(
        ///     "Failed to connect to device.\nWould you like to retry?", "Connection Error",
        ///     CustomMessageBoxButtons.RetryCancel, CustomMessageBoxIcon.Error);
        /// if (result == CustomMessageBoxResult.Retry) 
        /// {
        ///     // Retry logic here
        /// }
        /// 
        /// // Custom sizing for long messages
        /// CustomMessageBox.Show("Very long message content...", "Information",
        ///     CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information, 500, 300);
        /// </code>
        /// </example>
        public static CustomMessageBoxResult Show(
            string message,
            string title = "",
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.None,
            int? width = null,
            int? height = null)
        {
            // ===== LAYOUT CALCULATIONS =====
            // Configure basic layout dimensions and spacing constants
            int iconWidth = icon == CustomMessageBoxIcon.None ? 0 : 48;  // Standard Windows icon size
            int buttonPanelHeight = 70;     // Space for buttons and padding
            int minWidth = 340;             // Minimum usable width for readability
            int minHeight = 180;            // Minimum usable height for all content
            int leftMargin = icon == CustomMessageBoxIcon.None ? 26 : 48 + iconWidth;  // Text left offset
            int topMargin = 32;             // Top spacing for visual breathing room
            int labelPadding = 16;          // Additional text padding for margins

            // Define typography for consistent text rendering
            Font messageFont = new Font("Comic Neue", 12, FontStyle.Regular);

            // Calculate optimal dialog width based on content
            // Algorithm: longest line length × 9px + base width + icon space
            int defaultWidth = width ?? Math.Max(minWidth, message.Split('\n').Max(s => s.Length) * 9 + 100 + iconWidth);
            int maxLabelWidth = defaultWidth - leftMargin - labelPadding;

            // Dynamically measure actual text dimensions for accurate height calculation
            Size textSize;
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                // Use TextRenderer for accurate measurement with word wrapping
                textSize = TextRenderer.MeasureText(g, message, messageFont, new Size(maxLabelWidth, 0), TextFormatFlags.WordBreak);
            }
            int labelHeight = Math.Max(textSize.Height, iconWidth);  // Ensure height accommodates icon

            // Calculate total form height including content and button areas
            int contentHeight = Math.Max(labelHeight, iconWidth) + topMargin + 8; // Content + margins
            int totalHeight = (height ?? (contentHeight + buttonPanelHeight + 36));  // Total + button area
            totalHeight = Math.Max(totalHeight, minHeight);  // Enforce minimum height

            int w = defaultWidth;
            int h = totalHeight;

            // ===== FORM CREATION AND CONFIGURATION =====
            using (Form popup = new Form
            {
                FormBorderStyle = FormBorderStyle.FixedSingle,  // Clean border, non-resizable
                StartPosition = FormStartPosition.CenterScreen, // Center on primary display
                Size = new Size(w, h),                          // Calculated optimal size
                MinimumSize = new Size(minWidth, minHeight),    // Enforce usability minimums
                TopMost = true,                                 // Ensure visibility above other windows
                BackColor = Color.White,                        // Professional white background
                ControlBox = false,                             // Remove window controls for clean look
                ShowInTaskbar = false,                          // Don't clutter taskbar with temporary dialog
                Text = "",                                      // No title text for minimal design
            })
            {
                // ===== CONTAINER PANEL SETUP =====
                // Outer border panel provides clean visual definition
                var borderPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle        // Draws professional black border
                };

                // Inner content panel contains message text and optional icon
                var panelContent = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(0)                    // Precise manual positioning
                };

                // ===== ICON SETUP =====
                // Optional icon display for semantic visual context
                PictureBox? picture = null;
                if (icon != CustomMessageBoxIcon.None)
                {
                    picture = new PictureBox
                    {
                        Image = GetIconBitmap(icon),             // System icon bitmap
                        SizeMode = PictureBoxSizeMode.StretchImage,  // Scale to fit container
                        Width = iconWidth,                       // Standard 48px width
                        Height = iconWidth,                      // Square aspect ratio
                        Left = 26,                              // Left margin from dialog edge
                        Top = topMargin,                        // Align with text top
                        BackColor = Color.White                 // Blend with dialog background
                    };
                    panelContent.Controls.Add(picture);
                }

                // ===== MESSAGE LABEL SETUP =====
                // Primary text content with professional typography
                var label = new Label
                {
                    Text = message,                             // User-provided message content
                    AutoSize = false,                           // Manual sizing for precise control
                    Height = labelHeight,                       // Calculated height for content
                    Width = maxLabelWidth,                      // Available width after margins
                    Top = topMargin,                            // Align with icon top
                    Left = leftMargin,                          // Position after icon and margins
                    TextAlign = ContentAlignment.MiddleLeft,    // Left-aligned, vertically centered
                    Font = messageFont,                         // Comic Neue for readability
                    ForeColor = Color.Black,                    // High contrast text color
                    BorderStyle = BorderStyle.None,             // Clean appearance without borders
                    BackColor = Color.White,                    // Match dialog background
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right  // Responsive sizing
                };
                panelContent.Controls.Add(label);

                // ===== BUTTON PANEL SETUP =====
                // Bottom panel containing user action buttons
                var panelButtons = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = buttonPanelHeight,                 // Fixed height for consistent layout
                    Padding = new Padding(0, 4, 0, 8),         // Vertical padding for button spacing
                    BackColor = Color.White                     // Match overall dialog background
                };
                // ===== BUTTON CREATION =====
                // Pre-create all possible buttons with consistent styling
                Button btnOK = new Button
                {
                    Text = "OK",                                // Standard OK button text
                    Width = 100,                               // Consistent button width
                    Height = 36,                               // Standard button height
                    DialogResult = DialogResult.OK,            // Enable keyboard shortcuts
                    Font = new Font("Comic Neue", 12, FontStyle.Bold),  // Bold for emphasis
                    BackColor = Color.WhiteSmoke               // Subtle background differentiation
                };
                Button btnCancel = new Button
                {
                    Text = "Cancel",                           // Standard Cancel button text
                    Width = 100,                               // Matching width for alignment
                    Height = 36,                               // Matching height for consistency
                    DialogResult = DialogResult.Cancel,        // Enable Escape key shortcut
                    Font = new Font("Comic Neue", 12, FontStyle.Bold),  // Consistent typography
                    BackColor = Color.WhiteSmoke               // Matching visual style
                };
                Button btnRetry = new Button
                {
                    Text = "Retry",                            // Retry operation button text
                    Width = 100,                               // Consistent sizing
                    Height = 36,                               // Standard height
                    DialogResult = DialogResult.Retry,         // Enable retry shortcuts
                    Font = new Font("Comic Neue", 12, FontStyle.Bold),  // Bold emphasis
                    BackColor = Color.WhiteSmoke               // Consistent appearance
                };

                // Result tracking variable for user choice capture
                CustomMessageBoxResult result = CustomMessageBoxResult.None;

                // ===== BUTTON LAYOUT AND EVENT HANDLING =====
                // Configure button arrangement based on requested button combination
                int btnTop = 14;  // Consistent vertical position within button panel
                                  // SINGLE OK BUTTON: Acknowledgment scenarios (errors, information)
                if (buttons == CustomMessageBoxButtons.OK)
                {
                    btnOK.Click += (s, e) => { result = CustomMessageBoxResult.Ok; popup.Close(); };
                    btnOK.Location = new Point((w - btnOK.Width) / 2, btnTop);  // Center horizontally
                    panelButtons.Controls.Add(btnOK);
                    popup.AcceptButton = btnOK;  // Enable Enter key shortcut
                }
                // OK AND CANCEL BUTTONS: Accept/reject scenarios (confirmations)
                else if (buttons == CustomMessageBoxButtons.OKCancel)
                {
                    btnOK.Click += (s, e) => { result = CustomMessageBoxResult.Ok; popup.Close(); };
                    btnCancel.Click += (s, e) => { result = CustomMessageBoxResult.Cancel; popup.Close(); };
                    int spacing = 16;  // Space between buttons for visual separation
                    int totalBtnWidth = btnOK.Width + btnCancel.Width + spacing;
                    int leftStart = (w - totalBtnWidth) / 2;  // Center button group
                    btnOK.Location = new Point(leftStart, btnTop);
                    btnCancel.Location = new Point(leftStart + btnOK.Width + spacing, btnTop);
                    panelButtons.Controls.Add(btnOK);
                    panelButtons.Controls.Add(btnCancel);
                    popup.AcceptButton = btnOK;      // Enter activates OK
                    popup.CancelButton = btnCancel;  // Escape activates Cancel
                }
                // RETRY AND CANCEL BUTTONS: Error recovery scenarios (connection failures)
                else if (buttons == CustomMessageBoxButtons.RetryCancel)
                {
                    btnRetry.Click += (s, e) => { result = CustomMessageBoxResult.Retry; popup.Close(); };
                    btnCancel.Click += (s, e) => { result = CustomMessageBoxResult.Cancel; popup.Close(); };
                    int spacing = 16;  // Consistent button spacing
                    int totalBtnWidth = btnRetry.Width + btnCancel.Width + spacing;
                    int leftStart = (w - totalBtnWidth) / 2;  // Center button group
                    btnRetry.Location = new Point(leftStart, btnTop);
                    btnCancel.Location = new Point(leftStart + btnRetry.Width + spacing, btnTop);
                    panelButtons.Controls.Add(btnRetry);
                    panelButtons.Controls.Add(btnCancel);
                    popup.AcceptButton = btnRetry;   // Enter activates Retry
                    popup.CancelButton = btnCancel; // Escape activates Cancel
                }

                // ===== FORM ASSEMBLY AND DISPLAY =====
                // Assemble the complete dialog hierarchy
                borderPanel.Controls.Add(panelContent);  // Add content area to border
                borderPanel.Controls.Add(panelButtons);  // Add button area to border
                popup.Controls.Add(borderPanel);         // Add border panel to form

                // Display modal dialog and wait for user interaction
                popup.ShowDialog();  // Blocks until dialog closes
                return result;       // Return user's choice
            } // Form disposal handled automatically by using statement
        }
        /// <summary>
        /// Converts CustomMessageBoxIcon enumeration values to Windows system icon bitmaps
        /// Provides consistent icon representation using standard Windows system icons
        /// Ensures visual consistency with other Windows applications and system dialogs
        /// 
        /// Icon Sources:
        /// - Error: Red X icon from SystemIcons.Error (critical failures)
        /// - Warning: Yellow triangle icon from SystemIcons.Warning (cautions)
        /// - Information: Blue "i" icon from SystemIcons.Information (informational)
        /// - Question: Blue "?" icon from SystemIcons.Question (user prompts)
        /// - None: Returns null for no icon display
        /// 
        /// Performance Notes:
        /// - SystemIcons are cached by Windows and have minimal performance impact
        /// - ToBitmap() creates a copy suitable for PictureBox display
        /// - Bitmap disposal is handled automatically by PictureBox control
        /// - Icon size is standardized at 48x48 pixels for consistent appearance
        /// </summary>
        /// <param name="iconType">CustomMessageBoxIcon enumeration value specifying desired icon</param>
        /// <returns>
        /// Bitmap representation of the system icon, or null for CustomMessageBoxIcon.None
        /// Bitmap is ready for display in PictureBox controls with appropriate sizing
        /// </returns>
        private static Bitmap? GetIconBitmap(CustomMessageBoxIcon iconType)
        {
            return iconType switch
            {
                CustomMessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
                CustomMessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
                CustomMessageBoxIcon.Information => SystemIcons.Information.ToBitmap(),
                CustomMessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
                _ => null,
            };
        }
    }
}
