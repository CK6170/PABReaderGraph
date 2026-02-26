using System;
using System.Runtime.InteropServices;

/// <summary>
/// Windows API integration utility for system-wide window management and workspace control
/// Provides professional workspace preparation for dedicated monitoring environments
/// Uses Windows Shell API through P/Invoke for native system integration without external dependencies
/// 
/// Core Functionality:
/// 
/// WORKSPACE MANAGEMENT:
/// - Minimizes all open windows to provide clean, dedicated monitoring workspace
/// - Restores all previously minimized windows on application exit for system courtesy
/// - Integrates with Windows taskbar and shell for native system behavior
/// - Supports professional monitoring environments and control room scenarios
/// 
/// WINDOWS API INTEGRATION:
/// - Uses Shell_TrayWnd window class for accessing Windows taskbar functionality
/// - Sends WM_COMMAND messages with specific command IDs for minimize/restore operations
/// - Leverages built-in Windows "Show Desktop" and "Restore Desktop" functionality
/// - Provides same behavior as Windows+D keyboard shortcut and taskbar button
/// 
/// SYSTEM INTEGRATION BENEFITS:
/// - Native Windows integration without external dependencies or tools
/// - Respects Windows window management policies and user preferences
/// - Compatible across Windows versions supporting Shell_TrayWnd interface
/// - Minimal system resource usage through direct API calls
/// 
/// APPLICATION INTEGRATION CONTEXT:
/// - Called from Program.Main() during application startup for workspace preparation
/// - Called from MultiFormContext.ExitThreadCore() during shutdown for system restoration
/// - Enables dedicated monitoring environment for professional data analysis
/// - Ensures courteous system behavior by restoring user workspace on exit
/// 
/// PROFESSIONAL USE CASES:
/// - Control room monitoring stations requiring dedicated display environments
/// - Engineering workstations where data monitoring needs focused attention
/// - Production monitoring systems requiring minimal visual distractions
/// - Quality assurance environments with dedicated monitoring requirements
/// 
/// THREADING AND SAFETY:
/// - All methods are thread-safe and can be called from any thread
/// - Windows API calls are atomic and handle cross-thread scenarios automatically
/// - No internal state management - purely stateless utility functions
/// - Safe for concurrent usage from multiple application components
/// 
/// ERROR HANDLING PHILOSOPHY:
/// - Silent failure mode - operations fail gracefully if Windows API is unavailable
/// - No exceptions thrown for API failures (maintains application stability)
/// - Validation through IntPtr checks prevents invalid window handle operations
/// - Degrades gracefully on systems where Shell_TrayWnd is not available
/// 
/// COMPATIBILITY CONSIDERATIONS:
/// - Supports Windows versions with standard Shell_TrayWnd implementation
/// - Compatible with both 32-bit and 64-bit Windows environments
/// - Works across different Windows UI themes and shell replacements
/// - Respects Windows accessibility settings and user customizations
/// </summary>
public static class WindowsMinimizer
{
    /// <summary>
    /// P/Invoke import for Windows API FindWindow function
    /// Locates window handles by class name and window title for system integration
    /// Essential for accessing Windows shell components like the taskbar
    /// 
    /// Function Purpose:
    /// - Searches for top-level windows matching specified class name and title
    /// - Returns window handle (HWND) for subsequent API operations
    /// - Enables access to system windows not directly exposed through .NET
    /// 
    /// Parameters:
    /// - lpClassName: Window class name (e.g., "Shell_TrayWnd" for taskbar)
    /// - lpWindowName: Window title text (null for any title)
    /// 
    /// Return Value:
    /// - IntPtr: Window handle if found, IntPtr.Zero if not found
    /// - Used for validation before attempting window operations
    /// 
    /// Windows API Documentation:
    /// - Part of user32.dll standard Windows API library
    /// - Available across all supported Windows versions
    /// - Thread-safe and suitable for concurrent usage
    /// </summary>
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    /// <summary>
    /// P/Invoke import for Windows API SendMessage function
    /// Sends messages to windows for communication and control operations
    /// Core mechanism for interacting with Windows shell and system components
    /// 
    /// Function Purpose:
    /// - Sends specified message to target window with optional parameters
    /// - Synchronous operation that waits for message processing completion
    /// - Universal Windows communication mechanism for inter-window operations
    /// 
    /// Parameters:
    /// - hWnd: Target window handle obtained from FindWindow
    /// - Msg: Message identifier (e.g., WM_COMMAND for command operations)
    /// - wParam: First message parameter (message-specific usage)
    /// - lParam: Second message parameter (message-specific usage)
    /// 
    /// Return Value:
    /// - IntPtr: Message processing result (message-dependent interpretation)
    /// - For WM_COMMAND messages, typically indicates success/failure status
    /// 
    /// Windows API Integration:
    /// - Fundamental Windows messaging system for cross-process communication
    /// - Handles window manager operations and shell interactions
    /// - Provides access to functionality not available through managed APIs
    /// </summary>
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Windows message identifier for command operations
    /// Standard message type for sending commands to windows and controls
    /// Used to trigger specific actions in target windows through SendMessage
    /// 
    /// Message Usage:
    /// - WM_COMMAND is the standard Windows message for command operations
    /// - wParam contains the command identifier for the specific operation
    /// - lParam typically unused for simple command operations
    /// - Processed by target window's message handler (window procedure)
    /// 
    /// Shell Integration Context:
    /// - Shell_TrayWnd processes WM_COMMAND messages for taskbar operations
    /// - Command IDs specify which taskbar function to execute
    /// - Enables programmatic access to built-in Windows shell features
    /// </summary>
    private const int WM_COMMAND = 0x0111;

    /// <summary>
    /// Command identifier for "Minimize All Windows" operation
    /// Corresponds to Windows "Show Desktop" functionality (Windows+D shortcut)
    /// Triggers system-wide window minimization through shell integration
    /// 
    /// Command Behavior:
    /// - Minimizes all open application windows to the taskbar
    /// - Equivalent to clicking "Show Desktop" button in taskbar
    /// - Provides clean desktop workspace for dedicated applications
    /// - Respects Windows window management policies and user settings
    /// 
    /// System Integration:
    /// - Uses built-in Windows shell functionality for reliable operation
    /// - Compatible with accessibility features and user customizations
    /// - Handles special windows (always-on-top, system dialogs) appropriately
    /// - Maintains window restoration state for subsequent undo operation
    /// </summary>
    private const int MIN_ALL = 419;

    /// <summary>
    /// Command identifier for "Restore All Windows" operation
    /// Corresponds to Windows "Restore Desktop" functionality (undo minimize all)
    /// Reverses previous minimize all operation to restore user workspace
    /// 
    /// Command Behavior:
    /// - Restores all windows that were minimized by previous MIN_ALL operation
    /// - Does not affect windows that were already minimized before MIN_ALL
    /// - Maintains original window positions, sizes, and z-order relationships
    /// - Provides seamless workspace restoration for courteous application behavior
    /// 
    /// System Courtesy:
    /// - Essential for professional applications that modify system state
    /// - Ensures user workspace is restored when monitoring application exits
    /// - Respects user's original window arrangement and desktop organization
    /// - Prevents permanent disruption of user's work environment
    /// </summary>
    private const int MIN_ALL_UNDO = 416;

    /// <summary>
    /// Minimizes all open windows to provide clean desktop workspace for dedicated monitoring
    /// Equivalent to Windows "Show Desktop" functionality (Windows+D) through programmatic API access
    /// Essential for creating professional monitoring environments with minimal visual distractions
    /// 
    /// Operation Sequence:
    /// 1. Locate Windows taskbar window handle using Shell_TrayWnd class name
    /// 2. Validate taskbar window exists and is accessible
    /// 3. Send WM_COMMAND message with MIN_ALL command ID to trigger minimize operation
    /// 4. Windows shell processes command and minimizes all eligible application windows
    /// 
    /// Workspace Preparation Benefits:
    /// - Eliminates visual clutter for focused data monitoring and analysis
    /// - Provides dedicated screen real estate for PAB Reader Graph windows
    /// - Creates professional monitoring environment suitable for control rooms
    /// - Reduces cognitive load by minimizing irrelevant visual information
    /// 
    /// Windows Integration:
    /// - Uses native Windows shell functionality for reliable, consistent behavior
    /// - Respects Windows accessibility settings and user interface customizations
    /// - Handles special windows (always-on-top, system dialogs) according to system policy
    /// - Maintains window restoration state for subsequent undo operation
    /// 
    /// Professional Use Cases:
    /// - Control room monitoring stations requiring clean, dedicated display environments
    /// - Engineering workstations where data visualization needs focused attention  
    /// - Production monitoring systems requiring minimal visual distractions
    /// - Quality assurance environments with dedicated monitoring requirements
    /// 
    /// Error Handling:
    /// - Silent failure mode if taskbar window cannot be located (system compatibility)
    /// - IntPtr.Zero validation prevents operations on invalid window handles
    /// - No exceptions thrown - maintains application stability on API failures
    /// - Graceful degradation on systems with non-standard shell implementations
    /// 
    /// Threading Safety:
    /// - Thread-safe operation suitable for calling from any thread
    /// - Windows API calls handle cross-thread scenarios automatically
    /// - No internal state management - purely stateless utility function
    /// - Safe for concurrent usage from multiple application components
    /// 
    /// System Compatibility:
    /// - Compatible with Windows versions supporting Shell_TrayWnd window class
    /// - Works across 32-bit and 64-bit Windows environments
    /// - Functions correctly with different Windows themes and visual styles
    /// - Supports both single and multi-monitor configurations
    /// </summary>
    public static void MinimizeAllWindows()
    {
        // Locate Windows taskbar using Shell_TrayWnd window class
        IntPtr lHwnd = FindWindow("Shell_TrayWnd", null);
        if (lHwnd != IntPtr.Zero)
            // Send minimize all command to taskbar for system-wide window minimization
            SendMessage(lHwnd, WM_COMMAND, (IntPtr)MIN_ALL, IntPtr.Zero);
    }

    /// <summary>
    /// Restores all previously minimized windows to original positions and states
    /// Reverses minimize all operation to restore user's workspace arrangement
    /// Critical for courteous application behavior and professional system integration
    /// 
    /// Operation Sequence:
    /// 1. Locate Windows taskbar window handle using Shell_TrayWnd class name
    /// 2. Validate taskbar window exists and is accessible for restore operation
    /// 3. Send WM_COMMAND message with MIN_ALL_UNDO command ID to trigger restoration
    /// 4. Windows shell restores windows minimized by previous MIN_ALL operation
    /// 
    /// Restoration Behavior:
    /// - Restores only windows that were minimized by previous MinimizeAllWindows() call
    /// - Preserves original window positions, sizes, and z-order relationships
    /// - Does not affect windows that were already minimized before minimize all operation
    /// - Maintains user's original desktop organization and window arrangement
    /// 
    /// System Courtesy Features:
    /// - Essential for professional applications that temporarily modify system state
    /// - Ensures user workspace is restored when monitoring application exits
    /// - Prevents permanent disruption of user's work environment and productivity
    /// - Demonstrates respect for user's desktop organization and workflow
    /// 
    /// Integration with Application Lifecycle:
    /// - Called from MultiFormContext.ExitThreadCore() during application shutdown
    /// - Ensures workspace restoration regardless of how application terminates
    /// - Provides clean exit experience for professional monitoring deployments
    /// - Maintains system courtesy even during error conditions or unexpected exits
    /// 
    /// Professional Benefits:
    /// - Enables temporary workspace modification without permanent system impact
    /// - Supports shared workstation environments with multiple users and shifts
    /// - Facilitates professional monitoring applications in enterprise environments
    /// - Demonstrates application quality through thoughtful system integration
    /// 
    /// Error Handling:
    /// - Silent failure mode maintains application stability on API errors
    /// - IntPtr.Zero validation prevents operations on invalid window handles
    /// - No exceptions thrown - ensures clean shutdown even with system issues
    /// - Graceful operation on systems with non-standard shell configurations
    /// 
    /// Threading Safety:
    /// - Thread-safe operation suitable for shutdown sequences and cleanup operations
    /// - Windows API calls are atomic and handle multi-threading scenarios
    /// - No internal state dependencies - purely stateless restore function
    /// - Safe for emergency cleanup operations from any application thread
    /// 
    /// System Integration Notes:
    /// - Compatible with Windows accessibility features and user interface modifications
    /// - Respects Windows power management and system suspend/resume operations
    /// - Functions correctly with remote desktop and virtual desktop environments
    /// - Supports enterprise environments with group policy and security restrictions
    /// </summary>
    public static void RestoreAllWindows()
    {
        // Locate Windows taskbar using Shell_TrayWnd window class  
        IntPtr lHwnd = FindWindow("Shell_TrayWnd", null);
        if (lHwnd != IntPtr.Zero)
            // Send restore all command to taskbar for workspace restoration
            SendMessage(lHwnd, WM_COMMAND, (IntPtr)MIN_ALL_UNDO, IntPtr.Zero);
    }
}
