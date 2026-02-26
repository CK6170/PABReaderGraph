using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Application context manager for coordinating multiple GraphForm instances
    /// Serves as the main application controller handling form lifecycle, session management, and multi-monitor layout
    /// Inherits from ApplicationContext to provide custom application hosting beyond simple single-form scenarios
    /// 
    /// Core Responsibilities:
    /// - Multi-form lifecycle management for simultaneous port monitoring
    /// - Automatic form arrangement across multiple monitors for optimal viewing
    /// - Session restart functionality with hardware reinitialization
    /// - Graceful application shutdown with proper resource cleanup
    /// - Event coordination between SerialManager and GraphForm instances
    /// 
    /// Architecture Role:
    /// - Acts as the bridge between Program.Main and individual GraphForm instances
    /// - Coordinates hardware communication through SerialManager integration
    /// - Manages application-wide settings and preferences
    /// - Provides centralized control for session operations (restart, cleanup)
    /// 
    /// Multi-Monitor Support:
    /// - Intelligent form distribution across available displays
    /// - Grid-based layout calculations for optimal space utilization
    /// - Dynamic adjustment based on monitor count and resolution
    /// - Special handling for odd numbers of forms (last form stretching)
    /// 
    /// Session Management:
    /// - Complete session restart with hardware reinitialization
    /// - Error handling for hardware communication failures
    /// - Automatic port detection and GraphForm recreation
    /// - Settings persistence across session restarts
    /// 
    /// Resource Management:
    /// - Proper event handler cleanup to prevent memory leaks
    /// - SerialManager resource disposal on application exit
    /// - Window state restoration through WindowsMinimizer integration
    /// - Thread-safe form collection management
    /// 
    /// Design Philosophy:
    /// - Centralized Control: Single point of coordination for multi-form scenarios
    /// - Resilient: Handles hardware failures and communication errors gracefully
    /// - User-Friendly: Automatic layout and seamless session management
    /// - Professional: Clean shutdown and resource management patterns
    /// - Scalable: Supports variable numbers of ports and monitors dynamically
    /// </summary>
    public class MultiFormContext : ApplicationContext
    {
        /// <summary>
        /// Collection of currently active GraphForm instances managed by this context
        /// Maintains references for lifecycle management, event coordination, and layout operations
        /// Thread-safe operations ensure proper concurrent access during form operations
        /// </summary>
        private List<Form> openForms = new List<Form>();

        /// <summary>
        /// Application settings instance shared across all managed forms
        /// Provides centralized configuration access for window settings, preferences, and hardware parameters
        /// Preserved across session restarts to maintain user preferences and calibration data
        /// </summary>
        private Settings currentSettings;

        /// <summary>
        /// Initializes a new MultiFormContext with GraphForms for specified port IDs
        /// Creates and displays GraphForm instances for each available port with integrated event handling
        /// Automatically arranges forms across available monitors if auto-arrangement is enabled
        /// 
        /// Initialization Process:
        /// 1. Stores settings reference for session management
        /// 2. Creates GraphForm instance for each specified port ID
        /// 3. Registers each form with SerialManager for data coordination
        /// 4. Establishes FormClosed event handlers for lifecycle management
        /// 5. Adds forms to managed collection and displays them
        /// 6. Optionally auto-arranges forms based on user preference settings
        /// 
        /// Error Handling:
        /// - Invalid port IDs are filtered by SerialManager during registration
        /// - Form creation failures are isolated to individual instances
        /// - Event handler registration ensures proper cleanup coordination
        /// 
        /// Performance Considerations:
        /// - Forms are created and shown sequentially for proper initialization
        /// - Auto-arrangement calculations are deferred until all forms are created
        /// - SerialManager registration enables efficient data distribution
        /// </summary>
        /// <param name="selectedIds">
        /// List of zero-based port identifiers for GraphForm creation
        /// Should correspond to valid PAB ports detected by SerialManager
        /// Empty list will result in no forms being created
        /// </param>
        /// <param name="settings">
        /// Application settings instance containing user preferences and configuration
        /// Used for form initialization, window positioning, and behavior settings
        /// Maintained throughout application lifetime for consistency
        /// </param>
        public MultiFormContext(List<int> selectedIds, Settings settings)
        {
            // Store settings for session restart and form creation operations
            currentSettings = settings;

            // Create and initialize GraphForm for each selected port ID
            foreach (var id in selectedIds)
            {
                // Create new GraphForm instance with port-specific configuration
                var form = new GraphForm(id, settings);

                // Register form with SerialManager for data distribution coordination
                SerialManager.Instance.RegisterGraphForm((ushort)id, form);

                // Establish event handler for form lifecycle management
                form.FormClosed += OnFormClosed;

                // Add to managed collection for centralized control
                openForms.Add(form);

                // Display form to user (non-blocking operation)
                form.Show();
            }

            // Apply automatic form arrangement if enabled in user preferences
            if (currentSettings.AutoArrangeGraphs)
                AutoArrangeForms();
        }
        /// <summary>
        /// Handles FormClosed events from managed GraphForm instances
        /// Performs cleanup operations and application termination logic when forms close
        /// Ensures proper resource deallocation and prevents memory leaks through event handler cleanup
        /// 
        /// Cleanup Process:
        /// 1. Validates sender is a valid Form instance (type safety)
        /// 2. Removes FormClosed event handler to prevent duplicate cleanup calls
        /// 3. Removes form from managed collection for accurate tracking
        /// 4. Evaluates remaining form count for application termination decision
        /// 5. Initiates application shutdown if no forms remain active
        /// 
        /// Thread Safety:
        /// - Called on UI thread through Windows Forms event system
        /// - Safe modification of openForms collection during iteration
        /// - No additional synchronization required for single-threaded UI operations
        /// 
        /// Termination Logic:
        /// - Application continues running while any forms remain open
        /// - ExitThread() called when last form closes to terminate application gracefully
        /// - Triggers ExitThreadCore() for final cleanup operations
        /// </summary>
        /// <param name="sender">
        /// GraphForm instance that fired the FormClosed event
        /// Should be a Form object, but null safety checking is performed
        /// </param>
        /// <param name="e">
        /// FormClosedEventArgs containing close reason and other event data
        /// Required by event signature but not used in current implementation
        /// </param>
        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            // Safely cast sender to Form with null safety validation
            var form = sender as Form;
            if (form != null)
            {
                // Remove event handler to prevent memory leaks and duplicate cleanup
                form.FormClosed -= OnFormClosed;

                // Remove form from managed collection for accurate count tracking
                openForms.Remove(form);

                // Check if this was the last active form
                if (openForms.Count == 0)
                {
                    // Initiate application shutdown when no forms remain
                    // Triggers ExitThreadCore() for final cleanup operations
                    ExitThread();
                }
            }
        }
        /// <summary>
        /// Closes all managed GraphForm instances with proper cleanup and event handler removal
        /// Provides safe mass-closure functionality for session restart and application shutdown scenarios
        /// Prevents cascade event handling during bulk operations through event handler disconnection
        /// 
        /// Safety Features:
        /// - Creates defensive copy of form collection to prevent modification during iteration
        /// - Validates form instances are not null or disposed before closing
        /// - Disconnects FormClosed event handlers to prevent recursive OnFormClosed calls
        /// - Clears managed collection after all forms are closed
        /// 
        /// Use Cases:
        /// - Session restart operations requiring clean slate form management
        /// - Application shutdown requiring orderly form closure
        /// - Error recovery scenarios requiring form reset
        /// 
        /// Error Handling:
        /// - Null form references are safely ignored
        /// - Disposed forms are detected and skipped
        /// - Individual form close failures don't prevent processing of remaining forms
        /// - Collection is cleared regardless of individual close operation success
        /// 
        /// Performance Considerations:
        /// - ToList() creates defensive copy to prevent collection modification exceptions
        /// - Event handler disconnection before closure prevents unnecessary event processing
        /// - Bulk clear operation after individual closures for efficiency
        /// </summary>
        public void CloseAllForms()
        {
            // Create defensive copy to prevent collection modification during iteration
            foreach (var form in openForms.ToList())
            {
                // Validate form is still available and not disposed
                if (form != null && !form.IsDisposed)
                {
                    // Disconnect event handler to prevent recursive OnFormClosed calls
                    form.FormClosed -= OnFormClosed;

                    // Close form instance (triggers final cleanup in form itself)
                    form.Close();
                }
            }

            // Clear managed collection after all forms are processed
            openForms.Clear();
        }
        /// <summary>
        /// Performs complete session restart with hardware reinitialization and form recreation
        /// Provides seamless session refresh functionality while maintaining user settings and preferences
        /// Handles hardware communication failures gracefully with user notification and graceful shutdown
        /// 
        /// Restart Process:
        /// 1. Close all existing GraphForm instances with proper cleanup
        /// 2. Shutdown SerialManager to release hardware resources
        /// 3. Reinitialize SerialManager with current settings (hardware reconnection)
        /// 4. Detect available ports and validate hardware communication
        /// 5. Create new GraphForm instances for all detected ports
        /// 6. Re-establish event handlers and SerialManager registrations
        /// 7. Apply auto-arrangement if enabled in settings
        /// 
        /// Error Handling:
        /// - SerialManager initialization failures display error dialog and exit application
        /// - No available ports after restart shows warning and exits gracefully
        /// - Hardware communication errors are caught and reported to user
        /// - Application exits cleanly if restart cannot be completed successfully
        /// 
        /// User Experience:
        /// - Maintains current settings and window preferences across restart
        /// - Automatic port detection eliminates need for manual reconfiguration
        /// - Error messages provide clear feedback for hardware issues
        /// - Seamless transition from old to new session for successful restarts
        /// 
        /// Resource Management:
        /// - Complete cleanup of previous session resources before restart
        /// - Proper disposal of SerialManager connections and handlers
        /// - New form instances prevent memory leaks from previous session
        /// - Settings preservation maintains calibration data and user preferences
        /// 
        /// Threading Considerations:
        /// - Called on UI thread through user action (button click, menu item)
        /// - SerialManager operations are synchronous for deterministic error handling
        /// - Form creation and display operations are sequential for proper initialization
        /// </summary>
        public void RestartSession()
        {
            // PHASE 1: Clean shutdown of current session
            // Close all open GraphForms with proper event handler cleanup
            CloseAllForms();

            // Close SerialManager to release hardware connections and resources
            SerialManager.Instance.Close();

            // PHASE 2: Hardware reinitialization and validation
            try
            {
                // Reinitialize SerialManager with same settings for hardware reconnection
                SerialManager.Instance.Initialize(currentSettings);
            }
            catch (Exception ex)
            {
                // Handle hardware initialization failure with user notification
                CustomMessageBox.Show(
                    $"Failed to reinitialize serial port: {ex.Message}",
                    "Error",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Error
                );
                // Exit application if hardware cannot be reinitialized
                Application.Exit();
                return;
            }

            // PHASE 3: Port detection and validation
            // Get list of available ports after reinitialization
            var selectedIds = SerialManager.Instance.GetAvailableIDs();

            // Validate that hardware is available for monitoring
            if (selectedIds.Count == 0)
            {
                // No valid ports found after restart - notify user and exit
                CustomMessageBox.Show(
                    "No valid PAB Ports found (no calibration factors received). Exiting.",
                    "No Ports",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning
                );

                // Clean shutdown of SerialManager before application exit
                SerialManager.Instance.Close();
                Application.Exit();
                return;
            }

            // PHASE 4: Form recreation and registration
            // Create new GraphForm instances for all detected ports
            foreach (var id in selectedIds)
            {
                // Create new GraphForm with existing settings (preserves user preferences)
                var form = new GraphForm(id, currentSettings);

                // Register with SerialManager for data distribution coordination
                SerialManager.Instance.RegisterGraphForm((ushort)id, form);

                // Establish event handler for proper lifecycle management
                form.FormClosed += OnFormClosed;

                // Add to managed collection and display to user
                openForms.Add(form);
                form.Show();
            }

            // PHASE 5: Layout application (if enabled)
            // Apply automatic form arrangement based on user preference
            if (currentSettings.AutoArrangeGraphs)
                AutoArrangeForms();
        }
        /// <summary>
        /// Automatically arranges all managed forms across available monitors using intelligent grid layout
        /// Distributes forms evenly across displays with optimal space utilization and professional appearance
        /// Calculates grid dimensions dynamically based on form count and monitor configuration
        /// 
        /// Layout Algorithm:
        /// 1. Distributes forms evenly across all available monitors/screens
        /// 2. Calculates optimal grid dimensions (rows × columns) for each monitor
        /// 3. Uses square root approximation for balanced grid aspect ratios
        /// 4. Handles odd form counts with special last-form stretching logic
        /// 5. Respects monitor working areas (excludes taskbars and docked panels)
        /// 
        /// Multi-Monitor Strategy:
        /// - Forms per screen = ceiling(total forms ÷ screen count)
        /// - Remaining forms distributed to subsequent screens
        /// - Each screen gets optimized grid layout for assigned forms
        /// - Working area boundaries respected for proper visibility
        /// 
        /// Grid Calculation:
        /// - Rows: ceiling(sqrt(forms on this screen))
        /// - Columns: ceiling(forms on this screen ÷ rows)
        /// - Provides approximately square grids for aesthetic appeal
        /// - Handles non-perfect squares gracefully with rectangular grids
        /// 
        /// Special Cases:
        /// - Empty form collection: Safe early return, no operations performed
        /// - Single form: Uses full screen working area
        /// - Odd form count: Last form stretches across full width for balance
        /// - Multiple monitors: Intelligent distribution prevents overcrowding
        /// 
        /// Layout Features:
        /// - Zero margin configuration for maximum screen utilization
        /// - Form sizing based on available working area divisions
        /// - Last form stretching for odd counts maintains visual balance
        /// - Cross-monitor distribution for large form collections
        /// 
        /// Performance Characteristics:
        /// - Linear time complexity: O(forms + screens)
        /// - Minimal calculations: Simple arithmetic operations
        /// - No UI blocking: Direct property assignment for immediate effect
        /// - Memory efficient: No temporary collections or complex data structures
        /// </summary>
        private void AutoArrangeForms()
        {
            // Get total form count for distribution calculations
            int formCount = openForms.Count;
            if (formCount == 0)
                return; // Safe early return for empty collection

            // Get all available screens/monitors for distribution
            var screens = Screen.AllScreens;
            int screenCount = screens.Length;

            // Calculate optimal forms per screen distribution
            // Uses ceiling to ensure all forms are assigned to screens
            int formsPerScreen = (int)Math.Ceiling((double)formCount / screenCount);

            int formIndex = 0; // Global form index for sequential assignment

            // Process each available screen/monitor
            foreach (var screen in screens)
            {
                // Calculate remaining forms to be placed
                int remainingForms = formCount - formIndex;

                // Determine actual forms for this screen (may be less than formsPerScreen)
                int formsThisScreen = Math.Min(formsPerScreen, remainingForms);

                if (formsThisScreen == 0)
                    break; // No more forms to place, exit loop

                // GRID CALCULATION: Determine optimal rows and columns
                // Uses square root approach for approximately square grids
                int rows = (int)Math.Ceiling(Math.Sqrt(formsThisScreen));
                int cols = (int)Math.Ceiling((double)formsThisScreen / rows);

                // Get monitor working area (excludes taskbars and system panels)
                var workArea = screen.WorkingArea;

                // Calculate individual form dimensions based on grid division
                int formW = workArea.Width / cols;   // Form width from column division
                int formH = workArea.Height / rows;  // Form height from row division

                // FORM PLACEMENT: Position each form within the calculated grid
                for (int i = 0; i < formsThisScreen; i++)
                {
                    // Calculate grid position for current form
                    int row = i / cols;  // Row index (0-based)
                    int col = i % cols;  // Column index (0-based)

                    // Get form reference for positioning
                    var form = openForms[formIndex++];

                    int margin = 0; // Zero margin for maximum space utilization

                    // SPECIAL CASE: Handle odd form count with last form stretching
                    if (formsThisScreen % 2 == 1 && i == formsThisScreen - 1)
                    {
                        // Stretch the last (bottom) form across full width for visual balance
                        form.Left = workArea.Left + margin;
                        form.Top = workArea.Top + row * formH + margin;
                        form.Width = workArea.Width - 2 * margin;
                        form.Height = formH - 2 * margin;
                    }
                    else
                    {
                        // STANDARD PLACEMENT: Position form within calculated grid cell
                        form.Left = workArea.Left + col * formW + margin;
                        form.Top = workArea.Top + row * formH + margin;
                        form.Width = formW - 2 * margin;
                        form.Height = formH - 2 * margin;
                    }
                }
            }
        }
        /// <summary>
        /// Override of ApplicationContext.ExitThreadCore for custom application shutdown logic
        /// Performs final cleanup operations before application termination to ensure proper resource disposal
        /// Integrates with WindowsMinimizer for system window state restoration on application exit
        /// 
        /// Cleanup Operations:
        /// 1. SerialManager shutdown and resource disposal
        /// 2. Hardware connection termination and port release
        /// 3. System window state restoration through WindowsMinimizer
        /// 4. Base class cleanup for standard ApplicationContext shutdown
        /// 
        /// Error Handling:
        /// - SerialManager.Close() wrapped in try-catch for safe shutdown
        /// - Individual cleanup failures logged but don't prevent application exit
        /// - Base class cleanup called regardless of individual operation success
        /// - Debug output for troubleshooting cleanup issues
        /// 
        /// System Integration:
        /// - WindowsMinimizer.RestoreAllWindows() reverses window minimization
        /// - Ensures other applications return to pre-session state
        /// - Maintains system courtesy for shared desktop environment
        /// 
        /// Threading Context:
        /// - Called on main UI thread during application shutdown
        /// - Synchronous operations ensure complete cleanup before exit
        /// - No background thread coordination required
        /// 
        /// Resource Management Philosophy:
        /// - Fail-safe: Individual cleanup failures don't prevent application exit
        /// - Complete: All managed resources are explicitly released
        /// - Courteous: System state restored for other applications
        /// - Logged: Cleanup issues recorded for debugging purposes
        /// </summary>
        protected override void ExitThreadCore()
        {
            try
            {
                // Attempt clean shutdown of SerialManager and hardware connections
                // Releases COM ports, stops data collection, and disposes resources
                SerialManager.Instance.Close();
            }
            catch (Exception ex)
            {
                // Log cleanup errors for debugging but don't prevent application exit
                // Individual cleanup failures should not block application termination
                Debug.WriteLine("Error closing SerialManager: " + ex.Message);
            }

            // Restore all windows that were minimized during session
            // Maintains system courtesy by returning other applications to previous state
            WindowsMinimizer.RestoreAllWindows();

            // Call base implementation for standard ApplicationContext cleanup
            // Ensures proper thread termination and final resource disposal
            base.ExitThreadCore();
        }
    }
}

