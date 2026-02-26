using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Main program entry point and application initialization controller for PAB Reader Graph
    /// Orchestrates complete application startup sequence including settings, hardware initialization, and form management
    /// Provides centralized error handling and user interaction for critical startup operations
    /// 
    /// Startup Workflow:
    /// 1. System Preparation: Minimize other applications for dedicated workspace
    /// 2. User Configuration: Present settings dialog for hardware and preferences
    /// 3. Hardware Initialization: Connect to PAB devices with retry logic and error handling
    /// 4. Port Detection: Discover available monitoring ports with validation
    /// 5. Application Launch: Create multi-form context and begin data monitoring
    /// 
    /// Error Handling Philosophy:
    /// - User Choice: Provide retry options for recoverable hardware failures
    /// - Graceful Exit: Clean system restoration on user cancellation or critical errors
    /// - Informative Feedback: Clear error messages with actionable options
    /// - Resource Cleanup: Proper disposal and system state restoration on all exit paths
    /// 
    /// System Integration:
    /// - WindowsMinimizer integration for professional workspace management
    /// - SerialManager coordination for hardware communication
    /// - MultiFormContext orchestration for multi-port monitoring
    /// - Settings persistence across application sessions
    /// 
    /// Design Patterns:
    /// - Application Controller: Centralized startup orchestration
    /// - Error Recovery: Retry loops with user choice for hardware failures
    /// - Resource Management: Deterministic cleanup through using statements and exit handlers
    /// - User Feedback: Consistent messaging through AutoClosingMessage and CustomMessageBox
    /// 
    /// Threading Model:
    /// - Single Threaded Apartment (STA) for Windows Forms compatibility
    /// - Synchronous startup sequence for deterministic error handling
    /// - UI thread initialization for proper control creation and event handling
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Global reference to the main application context managing multiple GraphForm instances
        /// Provides application-wide access to the MultiFormContext for session management operations
        /// Enables centralized control of form lifecycle, restart operations, and graceful shutdown
        /// 
        /// Usage Context:
        /// - Session restart operations initiated from individual GraphForm instances
        /// - Application-wide shutdown coordination and resource cleanup
        /// - Form collection management for operations spanning multiple windows
        /// 
        /// Lifecycle:
        /// - Initialized during successful application startup after hardware validation
        /// - Maintained throughout application lifetime for session management
        /// - Disposed automatically through Application.Run completion and exit paths
        /// 
        /// Thread Safety:
        /// - Accessed only from main UI thread due to Windows Forms threading model
        /// - No explicit synchronization required due to single-threaded access pattern
        /// - MultiFormContext handles internal thread safety for form collection operations
        /// </summary>
        public static MultiFormContext? AppContext;

        /// <summary>
        /// Application entry point orchestrating complete startup sequence for PAB Reader Graph
        /// Manages five-phase initialization with comprehensive error handling and user interaction
        /// Provides professional application experience with workspace management and hardware coordination
        /// 
        /// Startup Phases:
        /// 
        /// PHASE 1: System Preparation
        /// - Minimizes all other applications for dedicated monitoring workspace
        /// - Configures Windows Forms visual styles and text rendering for professional appearance
        /// - Establishes STA threading model for proper Windows Forms operation
        /// 
        /// PHASE 2: User Configuration
        /// - Presents settings dialog for hardware parameters and user preferences
        /// - Validates user choices and handles cancellation with graceful system restoration
        /// - Preserves settings for hardware initialization and session management
        /// 
        /// PHASE 3: Hardware Initialization
        /// - Attempts SerialManager initialization with user-configured parameters
        /// - Implements retry loop for transient hardware communication failures
        /// - Provides user choice between retry and exit for failed initialization attempts
        /// 
        /// PHASE 4: Port Detection and Validation
        /// - Discovers available PAB ports through SerialManager hardware enumeration
        /// - Validates port functionality through calibration factor verification
        /// - Handles no-port scenarios with informative user notification
        /// 
        /// PHASE 5: Application Launch
        /// - Creates MultiFormContext for coordinated multi-form management
        /// - Launches Application.Run message loop for Windows Forms event processing
        /// - Provides confirmation feedback showing discovered port information
        /// 
        /// Error Handling Strategy:
        /// - Hardware Failures: Retry dialog with clear error description and user choice
        /// - User Cancellation: Immediate graceful exit with system restoration
        /// - Port Detection Issues: Informative warning with automatic cleanup
        /// - Resource Management: Deterministic cleanup on all exit paths
        /// 
        /// User Experience Features:
        /// - Professional workspace preparation through window minimization
        /// - Clear progress feedback during hardware detection operations
        /// - Informative success confirmation showing detected port information
        /// - Consistent error presentation with actionable user choices
        /// 
        /// Threading and Performance:
        /// - STA thread model for Windows Forms compatibility
        /// - Synchronous startup for predictable error handling
        /// - Efficient initialization with minimal user wait times
        /// - Resource cleanup through using statements and exit handlers
        /// </summary>
        [STAThread]
        static void Main()
        {
            // PHASE 1: SYSTEM PREPARATION
            // Minimize all other applications to provide dedicated monitoring workspace
            // Enhances user focus and reduces visual distractions during data monitoring
            WindowsMinimizer.MinimizeAllWindows();

            // Local function for consistent exit behavior with system restoration
            // Provides goodbye message and restores minimized applications on exit
            void ExitAndRestore()
            {
                AutoClosingMessage.Show(
                            "Goodbye...",
                            timeoutMilliseconds: 1000
                        );
                WindowsMinimizer.RestoreAllWindows();
                Environment.Exit(0);
            }

            // Configure Windows Forms for professional appearance and compatibility
            Application.EnableVisualStyles();                    // Modern visual styles
            Application.SetCompatibleTextRenderingDefault(false); // Improved text rendering

            // PHASE 2: USER CONFIGURATION
            Settings settings; // Will store user configuration for entire session

            // Present settings dialog for hardware configuration and user preferences
            using (var settingsForm = new SettingsForm())
            {
                var result = settingsForm.ShowDialog();
                if (result != DialogResult.OK)
                {
                    // User cancelled or closed settings dialog - exit gracefully
                    ExitAndRestore();
                }
                // Extract validated settings for hardware initialization
                settings = settingsForm.Settings;
            }

            // PHASE 3: HARDWARE INITIALIZATION WITH RETRY LOGIC
            bool initialized = false;
            while (!initialized) // Retry loop for transient hardware failures
            {
                try
                {
                    // Provide user feedback during hardware detection process
                    AutoClosingMessage.Show(
                        "Looking for a PAB. Please wait...",
                        timeoutMilliseconds: 5000
                    );

                    // Attempt SerialManager initialization with user settings
                    SerialManager.Instance.Initialize(settings);
                    initialized = true; // Success - exit retry loop
                }
                catch (Exception ex)
                {
                    // Hardware initialization failed - offer retry option to user
                    var result = CustomMessageBox.Show(
                        $"Failed to initialize serial port:\n{ex.Message}\nRetry?",
                        "Error",
                        CustomMessageBoxButtons.RetryCancel,
                        CustomMessageBoxIcon.Error
                    );
                    if (result != CustomMessageBoxResult.Retry)
                    {
                        // User chose not to retry - exit application gracefully
                        ExitAndRestore();
                    }
                    // User chose retry - continue loop for another initialization attempt
                }
            }

            // PHASE 4: PORT DETECTION AND VALIDATION
            // Get list of available PAB ports after successful initialization
            var selectedIds = SerialManager.Instance.GetAvailableIDs();

            // Validate that hardware ports are available for monitoring
            if (selectedIds.Count == 0)
            {
                // No valid ports found - inform user and exit gracefully
                CustomMessageBox.Show(
                    "No valid PAB Ports found (no calibration factors received). Exiting.",
                    "No Ports",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning
                );
                SerialManager.Instance.Close(); // Clean up SerialManager resources
                ExitAndRestore(); // Restore system and exit
            }

            // PHASE 5: APPLICATION LAUNCH
            // Provide success feedback showing discovered ports to user
            // Note: Port IDs are displayed as 1-based for user-friendly presentation
            AutoClosingMessage.Show(
                $"PAB Found\nIDs: {string.Join(", ", selectedIds.Select(n => (n+1).ToString()).ToArray())}", 
                timeoutMilliseconds: 1500
            );

            // Create MultiFormContext for coordinated multi-form management
            AppContext = new MultiFormContext(selectedIds, settings);

            // Launch main application message loop - blocks until application exit
            Application.Run(AppContext);
        }
    }
}
   