using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Application configuration and settings container with JSON serialization support
/// Centralizes all user preferences, hardware parameters, and session state for persistence
/// Provides default values for first-run scenarios and maintains compatibility across versions
/// 
/// Configuration Categories:
/// 
/// HARDWARE SETTINGS:
/// - Serial port configuration and device identification
/// - PAB device version matching and validation parameters
/// - Calibration timing and measurement parameters
/// - Port selection and filtering configuration
/// 
/// USER INTERFACE SETTINGS:
/// - Window positioning and sizing preferences per port
/// - Automatic layout and arrangement behaviors
/// - Data output folder organization preferences
/// 
/// PERSISTENCE FEATURES:
/// - JSON serialization with indented formatting for human readability
/// - HexUShortConverter integration for hardware-friendly port number display
/// - Automatic settings save on successful connections and configuration changes
/// - Backward compatibility with graceful handling of missing properties
/// 
/// USAGE PATTERNS:
/// - Loaded at application startup from "settings.json" file
/// - Passed to SerialManager for hardware initialization parameters
/// - Used by MultiFormContext and GraphForm for UI layout and behavior
/// - Automatically saved when hardware connections succeed or settings change
/// 
/// DEFAULT VALUE STRATEGY:
/// - Conservative defaults suitable for most deployment scenarios
/// - Hardware parameters based on common PAB device configurations
/// - UI preferences optimized for single-monitor professional use
/// - Safety-first approach with broad compatibility settings
/// 
/// SECURITY CONSIDERATIONS:
/// - No sensitive data stored (public configuration only)
/// - File location in application directory for easy backup/transfer
/// - Human-readable JSON format for troubleshooting and manual editing
/// - No encryption required due to non-sensitive nature of settings
/// </summary>
public class Settings
{
    /// <summary>
    /// Last successfully connected serial port name for connection optimization
    /// Enables faster reconnection by trying known-good port first before scanning
    /// Updated automatically when SerialManager establishes successful connections
    /// 
    /// Value Format:
    /// - Windows: "COM1", "COM2", etc. (standard COM port names)
    /// - Empty string indicates no previous successful connection
    /// - Used as first attempt during device discovery process
    /// 
    /// Performance Benefit:
    /// - Reduces connection time from seconds to milliseconds on repeat connections
    /// - Eliminates unnecessary port scanning when device location is stable
    /// - Particularly valuable in production environments with fixed hardware setup
    /// </summary>
    public string LastPort { get; set; } = "";

    /// <summary>
    /// Duration in seconds for calibration data collection periods
    /// Controls both initial startup calibration and manual zero calibration operations
    /// Balances measurement accuracy with user wait time for optimal experience
    /// 
    /// Calibration Process:
    /// - Collects continuous ADC readings during this period
    /// - Calculates averaged baseline values for zero offset determination
    /// - Longer periods improve accuracy but increase startup time
    /// - Shorter periods reduce wait time but may increase measurement noise
    /// 
    /// Recommended Values:
    /// - 10 seconds: Default balanced setting for most applications
    /// - 5 seconds: Faster startup for development/testing scenarios
    /// - 15-30 seconds: High-precision applications requiring maximum stability
    /// 
    /// Usage Context:
    /// - Applied during initial application startup calibration
    /// - Used for manual zero button operations in GraphForm
    /// - Displayed in countdown timer during calibration periods
    /// </summary>
    public int CalibrationSeconds { get; set; } = 10;

    /// <summary>
    /// Expected PAB device firmware version string for device identification and validation
    /// Used during device discovery to distinguish PAB devices from other serial equipment
    /// Ensures compatibility between application and hardware firmware versions
    /// 
    /// Version String Format:
    /// - Pattern: "PABG[generation]-[build].[major].[minor].[patch].[revision]"
    /// - Example: "PABG3-12324.02.27.01.25" (Generation 3, specific build/version)
    /// - Must match exactly for device recognition during port scanning
    /// 
    /// Device Discovery Process:
    /// - Application sends GetVersion command to each potential serial port
    /// - Compares response against this expected version string
    /// - Only ports returning matching version strings are considered valid PAB devices
    /// 
    /// Compatibility Management:
    /// - Update this value when deploying with different firmware versions
    /// - KnownPabVersions list maintains backward compatibility with multiple versions
    /// - Critical for preventing connection to incompatible or unrelated devices
    /// </summary>
    public string PabVersion { get; set; } = "PABG3-12324.02.27.01.25";

    /// <summary>
    /// Base directory path for session data output and file organization
    /// Establishes root location for all session folders and exported data files
    /// Supports both relative and absolute paths for deployment flexibility
    /// 
    /// Directory Structure:
    /// - Base folder contains timestamped session subfolders
    /// - Each session creates: "results_yyyyMMdd_HHmmss" subfolder
    /// - Session files include: JSON data, CSV exports, PNG graphs per port
    /// 
    /// Path Handling:
    /// - Relative paths resolved from application directory
    /// - Absolute paths used as-is for specific deployment requirements
    /// - Directory created automatically if it doesn't exist
    /// 
    /// Storage Organization:
    /// - "Data" default provides clean separation from application files
    /// - Timestamped subfolders prevent data conflicts between sessions
    /// - Consistent naming enables automated backup and archival processes
    /// 
    /// Deployment Considerations:
    /// - Network shares supported for centralized data collection
    /// - Local paths recommended for performance and reliability
    /// - Ensure sufficient disk space for continuous monitoring scenarios
    /// </summary>
    public string BaseFolder { get; set; } = "Data";

    /// <summary>
    /// Port selection bitmask defining which detected ports should be monitored
    /// Uses hexadecimal representation for hardware-friendly configuration and display
    /// Enables selective monitoring when multiple PAB devices are connected simultaneously
    /// 
    /// Bitmask Encoding:
    /// - Each bit position corresponds to a zero-based port ID
    /// - Bit 0 (LSB): Port 0 monitoring enabled/disabled
    /// - Bit 1: Port 1 monitoring enabled/disabled
    /// - Bit N: Port N monitoring enabled/disabled
    /// - 0xFF default: All 8 possible ports enabled (bits 0-7 set)
    /// 
    /// HexUShortConverter Integration:
    /// - Serializes as "0xFF" hex string for human-readable configuration files
    /// - Supports manual editing with intuitive hex values
    /// - Maintains numeric precision while improving readability
    /// 
    /// Usage Examples:
    /// - 0xFF: Monitor all detected ports (default)
    /// - 0x01: Monitor only port 0
    /// - 0x03: Monitor ports 0 and 1
    /// - 0x0F: Monitor ports 0-3
    /// 
    /// Multi-Device Scenarios:
    /// - Allows selective monitoring in multi-PAB installations
    /// - Supports load balancing across multiple monitoring stations
    /// - Enables troubleshooting by isolating specific ports
    /// </summary>
    [JsonConverter(typeof(HexUShortConverter))]
    public ushort PabPortSerialNumber { get; set; } = 0xFF;

    /// <summary>
    /// Automatic window arrangement feature enabling intelligent multi-monitor layout
    /// Controls whether GraphForm windows are positioned automatically across available displays
    /// Optimizes screen real estate utilization for professional monitoring environments
    /// 
    /// Arrangement Algorithm:
    /// - Distributes windows evenly across all available monitors/screens
    /// - Calculates optimal grid layout (rows × columns) for each display
    /// - Handles odd window counts with intelligent stretching for visual balance
    /// - Respects monitor working areas (excludes taskbars and system panels)
    /// 
    /// User Experience Benefits:
    /// - Eliminates manual window positioning for multi-port monitoring
    /// - Maximizes available screen space for data visualization
    /// - Provides consistent layout across different monitor configurations
    /// - Supports dynamic reconfiguration when monitor setup changes
    /// 
    /// Professional Use Cases:
    /// - Control room environments with multiple displays
    /// - Engineering workstations with dual/triple monitor setups
    /// - Production monitoring with dedicated display walls
    /// - Quality assurance stations requiring simultaneous port observation
    /// 
    /// Manual Override:
    /// - When disabled, windows appear at default locations with manual positioning
    /// - Individual window positions and sizes are saved in WindowSettingsPerId
    /// - Allows custom layouts for specialized monitoring requirements
    /// </summary>
    public bool AutoArrangeGraphs { get; set; } = true;

    /// <summary>
    /// Per-port window positioning and sizing preferences for persistent UI state
    /// Maintains individual window configurations across application sessions
    /// Enables personalized layouts while supporting multi-port monitoring scenarios
    /// 
    /// Dictionary Structure:
    /// - Key: Zero-based port ID (integer)
    /// - Value: WindowSettings object containing position, size, and state information
    /// - Automatically populated when windows are moved, resized, or state-changed
    /// 
    /// Persistence Behavior:
    /// - Updated automatically when GraphForm windows are closed
    /// - Restored when corresponding GraphForm is created
    /// - Preserved across application restarts and session changes
    /// - Independent settings for each port enable custom monitoring layouts
    /// 
    /// Multi-Monitor Support:
    /// - Window positions maintained relative to primary display
    /// - Supports negative coordinates for secondary monitor positioning
    /// - Handles monitor configuration changes gracefully
    /// - Falls back to safe defaults if stored position is invalid
    /// 
    /// Use Cases:
    /// - Operators with preferred window arrangements for different monitoring tasks
    /// - Multi-shift environments with personalized workspace configurations
    /// - Specialized monitoring requiring specific window sizes and positions
    /// - Dual-monitor setups with port-specific display assignments
    /// </summary>
    public Dictionary<int, WindowSettings> WindowSettingsPerId { get; set; } = new();

    /// <summary>
    /// Registry of known compatible PAB firmware versions for device validation
    /// Maintains backward compatibility while supporting multiple firmware generations
    /// Enables graceful handling of firmware updates and mixed-version deployments
    /// 
    /// Version Management Strategy:
    /// - Primary version (PabVersion) used for new device discovery
    /// - Known versions list supports legacy devices and gradual upgrades
    /// - Extensible design allows adding new versions without code changes
    /// - Facilitates testing with multiple firmware versions
    /// 
    /// Compatibility Benefits:
    /// - Prevents breaking changes when firmware is updated
    /// - Supports mixed environments with different device generations
    /// - Enables phased firmware rollouts in production environments
    /// - Provides fallback options for device discovery operations
    /// 
    /// Firmware Evolution Support:
    /// - PABG1 series: Original generation devices
    /// - PABG3 series: Current generation with enhanced features
    /// - Future PABG versions can be added as they become available
    /// 
    /// Maintenance Guidelines:
    /// - Add new firmware versions as they are validated
    /// - Retain legacy versions until all devices are upgraded
    /// - Document compatibility requirements for each version
    /// - Test device discovery across all supported versions
    /// </summary>
    public List<string> KnownPabVersions { get; set; } = new List<string>
    {
        "PABG1-12266.02.17.05.23",  // Original generation PAB devices
        "PABG3-12324.02.27.01.25"   // Current generation PAB devices
    };
}

/// <summary>
/// Window positioning, sizing, and state configuration for individual GraphForm instances
/// Provides persistent storage of user-preferred window layouts across application sessions
/// Supports multi-monitor environments with flexible positioning and state management
/// 
/// State Management Features:
/// - Position and size persistence for consistent user experience
/// - Window state tracking (normal, maximized) with proper restoration
/// - Multi-monitor coordinate support including negative positions
/// - Safe fallback defaults for invalid or missing configurations
/// 
/// Coordinate System:
/// - Position coordinates are screen pixels relative to primary display
/// - Negative coordinates supported for secondary monitors (left/above primary)
/// - Width/Height in pixels with reasonable minimum enforced by Windows Forms
/// - Maximized state overrides position/size but preserves restore bounds
/// 
/// User Experience Benefits:
/// - Windows reopen exactly where user positioned them
/// - Consistent layouts across application restarts and session changes
/// - Support for complex multi-monitor professional monitoring setups
/// - Personalized workspace configuration for different operators
/// 
/// Integration with Settings:
/// - Stored in Settings.WindowSettingsPerId dictionary by port ID
/// - Automatically updated when GraphForm windows are moved, resized, or closed
/// - Applied when new GraphForm instances are created for each port
/// - Independent settings per port enable custom monitoring arrangements
/// 
/// Multi-Monitor Considerations:
/// - Handles monitor configuration changes gracefully
/// - Falls back to primary display if saved position is off-screen
/// - Supports dynamic monitor setups (docking/undocking scenarios)
/// - Preserves user intent across hardware configuration changes
/// </summary>
public class WindowSettings
{
    /// <summary>
    /// Window width in pixels for GraphForm display area
    /// Controls horizontal size of individual monitoring windows
    /// Default provides readable data display on standard monitors
    /// 
    /// Size Considerations:
    /// - 800 pixels default accommodates most common monitor resolutions
    /// - Sufficient width for data table columns and plot visualization
    /// - Balanced between information density and readability
    /// - Automatically adjusted by Windows Forms if below minimum constraints
    /// 
    /// Multi-Monitor Scaling:
    /// - Values adjusted automatically for different DPI settings
    /// - Maintains proportional sizing across different display densities
    /// - User modifications preserved exactly as configured
    /// </summary>
    public int Width { get; set; } = 800;

    /// <summary>
    /// Window height in pixels for GraphForm display area
    /// Controls vertical size of individual monitoring windows
    /// Default provides adequate space for plot and data panel visualization
    /// 
    /// Layout Considerations:
    /// - 600 pixels default accommodates plot area and data display panels
    /// - Sufficient height for real-time graph visualization with reasonable time span
    /// - Balances plot visibility with screen space efficiency
    /// - Includes space for top data panel (Weight/ADC/Zero/Factor rows)
    /// 
    /// Content Sizing:
    /// - Height accounts for title bar, data panels, and button areas
    /// - Plot area receives majority of vertical space for data visualization
    /// - Responsive layout adjusts internal components based on total height
    /// </summary>
    public int Height { get; set; } = 600;

    /// <summary>
    /// Window left position in screen coordinates (X-axis)
    /// Defines horizontal placement of GraphForm on display(s)
    /// Supports multi-monitor setups with negative coordinates for secondary displays
    /// 
    /// Coordinate System:
    /// - Positive values: Positions to the right of primary display origin
    /// - Negative values: Positions to the left of primary display (secondary monitors)
    /// - Zero: Aligned with left edge of primary display
    /// - Values automatically validated against current monitor configuration
    /// 
    /// Multi-Monitor Support:
    /// - Coordinates span across all available displays
    /// - Secondary monitors may require negative X coordinates
    /// - Windows Forms handles cross-monitor positioning automatically
    /// - Invalid positions fall back to primary display with safe defaults
    /// 
    /// Default Staggering:
    /// - 100 pixel default with per-port offset prevents window overlap
    /// - Progressive positioning (100, 150, 200, etc.) for multiple ports
    /// - Ensures all windows are visible on initial display
    /// </summary>
    public int Left { get; set; } = 100;

    /// <summary>
    /// Window top position in screen coordinates (Y-axis)
    /// Defines vertical placement of GraphForm on display(s)
    /// Supports multi-monitor environments with flexible vertical positioning
    /// 
    /// Coordinate System:
    /// - Positive values: Positions below primary display origin
    /// - Negative values: Positions above primary display (monitors arranged vertically)
    /// - Zero: Aligned with top edge of primary display
    /// - Coordinates respect taskbar and system panel exclusion areas
    /// 
    /// Vertical Positioning Strategy:
    /// - 100 pixel default provides safe distance from screen top
    /// - Avoids interference with system taskbar and notification areas
    /// - Progressive positioning for multiple ports prevents overlap
    /// - Accounts for title bar and window chrome in positioning calculations
    /// 
    /// Multi-Monitor Considerations:
    /// - Supports monitors arranged above/below primary display
    /// - Automatically adjusted if saved position becomes invalid
    /// - Respects working area boundaries on each display
    /// </summary>
    public int Top { get; set; } = 100;

    /// <summary>
    /// Window maximized state flag for full-screen monitoring scenarios
    /// Enables dedicated full-screen data visualization when detailed analysis is required
    /// Preserves normal window size/position for restoration when un-maximized
    /// 
    /// State Management:
    /// - False (default): Normal windowed mode with user-defined size and position
    /// - True: Maximized to fill entire working area of containing monitor
    /// - State preserved across application restarts and session changes
    /// - Restoration bounds maintained separately for seamless state transitions
    /// 
    /// Use Cases:
    /// - Detail analysis requiring maximum plot visibility
    /// - Single-port monitoring with dedicated display
    /// - Presentation scenarios requiring full-screen data visualization
    /// - Control room environments with monitor-per-port assignments
    /// 
    /// Interaction with Other Properties:
    /// - When true, Width/Height represent restore bounds (not current size)
    /// - Left/Top represent restore position (not current position)
    /// - Window appears maximized but preserves sizing for restoration
    /// - Seamless transitions between maximized and normal states
    /// 
    /// Professional Benefits:
    /// - Maximizes data visualization real estate for critical monitoring
    /// - Supports both detailed analysis and overview monitoring modes
    /// - Enables operator preference for window management style
    /// </summary>
    public bool Maximized { get; set; } = false;
}

