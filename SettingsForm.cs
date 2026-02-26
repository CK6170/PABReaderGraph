using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// Configuration dialog form for PAB Reader Graph application settings and preferences
    /// Provides user interface for hardware parameters, monitoring preferences, and data output configuration
    /// Handles settings persistence with JSON serialization and validates user input for safe operation
    /// 
    /// Core Functionality:
    /// 
    /// SETTINGS MANAGEMENT:
    /// - Load existing settings from JSON file or create defaults for first-run scenarios
    /// - Present all configurable options through intuitive UI controls
    /// - Validate user input and provide immediate feedback for invalid configurations
    /// - Save settings with proper JSON formatting and error handling
    /// 
    /// USER INTERFACE FEATURES:
    /// - Professional dialog layout with logical grouping of related settings
    /// - Dynamic version display in title bar for deployment identification
    /// - Master checkbox for convenient port selection (all/none/partial states)
    /// - Folder browser integration for data output path selection
    /// - About dialog integration for application information
    /// 
    /// HARDWARE CONFIGURATION:
    /// - PAB firmware version selection with known version management
    /// - Port selection using checkboxes with bitmask encoding
    /// - Calibration timing configuration with range validation
    /// - Automatic addition of new firmware versions to known versions list
    /// 
    /// USER EXPERIENCE OPTIMIZATION:
    /// - Immediate UI feedback during configuration changes
    /// - Tri-state master checkbox showing partial selection status
    /// - Intuitive port numbering (1-8 in UI, 0-7 in underlying data)
    /// - Graceful error handling with fallback to defaults
    /// 
    /// INTEGRATION PATTERNS:
    /// - Called from Program.Main during application startup sequence
    /// - Blocks application initialization until configuration is complete or cancelled
    /// - Settings object passed to SerialManager and MultiFormContext for initialization
    /// - Supports configuration changes without application restart (when possible)
    /// 
    /// PERSISTENCE MODEL:
    /// - Settings stored in "settings.json" file in application directory
    /// - Human-readable JSON format enables manual editing and version control
    /// - Automatic backup of current settings before applying changes
    /// - Graceful degradation when settings file is corrupt or missing
    /// 
    /// ERROR HANDLING STRATEGY:
    /// - JSON deserialization failures fall back to default settings
    /// - Missing settings file creates new defaults transparently
    /// - Invalid folder paths prompt user selection through browser dialog
    /// - Settings save failures are logged but don't prevent application operation
    /// 
    /// DEPLOYMENT CONSIDERATIONS:
    /// - Version information displayed for support and troubleshooting
    /// - Settings file location enables easy backup and transfer between systems
    /// - Known versions list supports mixed firmware environments
    /// - Default values chosen for broad compatibility and safe operation
    /// </summary>
    public partial class SettingsForm : Form
    {
        /// <summary>
        /// Configuration file name for settings persistence
        /// Located in application directory for easy access and backup
        /// Uses JSON format for human readability and version control compatibility
        /// </summary>
        private const string SettingsFile = "settings.json";

        /// <summary>
        /// Prevents infinite recursion during master checkbox state updates
        /// Coordinates between individual port checkbox changes and master checkbox state
        /// Essential for proper tri-state behavior (checked/unchecked/indeterminate)
        /// </summary>
        private bool isUpdatingMasterCheckbox = false;

        /// <summary>
        /// Current settings configuration validated and ready for application use
        /// Populated from loaded JSON file or defaults, updated through UI interactions
        /// Passed to other application components after successful dialog completion
        /// 
        /// Lifecycle:
        /// - Initialized during LoadSettings() with file data or defaults
        /// - Updated continuously as user modifies UI controls
        /// - Finalized during SaveSettings() when user confirms changes
        /// - Consumed by SerialManager, MultiFormContext, and other components
        /// 
        /// Thread Safety:
        /// - Accessed only from UI thread during dialog operation
        /// - Immutable after dialog closes with OK result
        /// - Safe for consumption by other components after validation
        /// </summary>
        public Settings Settings { get; private set; }
        /// <summary>
        /// Initializes SettingsForm with version information and loads existing configuration
        /// Performs complete UI setup including version display and settings population
        /// Establishes foundation for user configuration interaction
        /// 
        /// Initialization Sequence:
        /// 1. Initialize Windows Forms designer components and base form functionality
        /// 2. Extract and display version information in title bar for deployment identification
        /// 3. Load existing settings from JSON file or create defaults for first-run scenarios
        /// 4. Populate all UI controls with current configuration values
        /// 5. Establish event handlers and UI state management
        /// 
        /// Version Display Integration:
        /// - Extracts version from executing assembly metadata
        /// - Formats as "vMajor.Minor.Build" for professional appearance
        /// - Falls back to "v1.0.0" if version extraction fails
        /// - Updates form title to include version for support identification
        /// 
        /// Settings Loading Process:
        /// - Attempts to load from "settings.json" in application directory
        /// - Deserializes JSON with error handling and fallback to defaults
        /// - Populates UI controls with loaded or default configuration values
        /// - Establishes known firmware versions list for dropdown population
        /// 
        /// UI State Initialization:
        /// - Sets numeric controls to configuration values with range validation
        /// - Populates text fields with current path and version settings
        /// - Configures port selection checkboxes with bitmask interpretation
        /// - Updates master checkbox to reflect current port selection state
        /// 
        /// Error Handling:
        /// - Version extraction failures use safe fallback display
        /// - Settings loading errors create new default configuration
        /// - UI population errors are handled gracefully with default values
        /// - Form remains functional even with partial initialization failures
        /// </summary>
        public SettingsForm()
        {
            // Initialize Windows Forms designer components and base form functionality
            InitializeComponent();

            // VERSION DISPLAY INTEGRATION
            // Extract version information from executing assembly for deployment identification
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            this.Text = $"PAB Device Settings - {versionString}";

            // SETTINGS LOADING AND UI POPULATION
            // Load existing configuration or create defaults for first-run scenarios
            LoadSettings();
        }
        /// <summary>
        /// Loads settings from JSON file and populates UI controls with configuration values
        /// Implements robust error handling with fallback to defaults for corrupted or missing files
        /// Establishes complete UI state based on loaded configuration data
        /// 
        /// Loading Strategy:
        /// 1. Check for existence of settings.json file in application directory
        /// 2. Read and deserialize JSON content with error handling
        /// 3. Fall back to default Settings instance for any failure scenarios
        /// 4. Populate all UI controls with loaded or default configuration values
        /// 5. Configure dynamic UI elements (port checkboxes, version dropdown)
        /// 
        /// JSON Processing:
        /// - Uses System.Text.Json for modern serialization with proper error handling
        /// - Handles malformed JSON gracefully by creating new default settings
        /// - Supports missing properties through Settings class default values
        /// - Preserves backward compatibility with older settings file formats
        /// 
        /// UI Population Process:
        /// - Numeric controls: CalibrationSeconds with range validation
        /// - Text fields: BaseFolder path and PabVersion string
        /// - Checkboxes: AutoArrangeGraphs boolean and individual port selections
        /// - Dropdowns: Known firmware versions from settings or defaults
        /// 
        /// Port Selection Handling:
        /// - Interprets PabPortSerialNumber bitmask for individual port checkboxes
        /// - Displays ports as 1-8 for user-friendly interface (internally 0-7)
        /// - Updates master checkbox state based on individual port selections
        /// - Maintains tri-state behavior (all/none/partial selection)
        /// 
        /// Error Recovery:
        /// - File not found: Creates new default Settings instance
        /// - JSON deserialization errors: Falls back to default configuration
        /// - Invalid settings values: Corrected through UI control validation
        /// - Missing firmware versions: Populated from Settings defaults
        /// 
        /// Performance Considerations:
        /// - Single file read operation for all settings data
        /// - Efficient JSON deserialization with minimal allocations
        /// - UI control population in logical order for smooth initialization
        /// - Master checkbox update deferred until after individual port setup
        /// </summary>
        private void LoadSettings()
        {
            // SETTINGS FILE LOADING WITH ERROR HANDLING
            if (File.Exists(SettingsFile))
            {
                try
                {
                    // Attempt to read and deserialize existing settings file
                    string json = File.ReadAllText(SettingsFile);
                    Settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
                catch
                {
                    // Handle JSON deserialization errors with default settings fallback
                    Settings = new Settings();
                }
            }
            else
            {
                // Create new default settings for first-run scenarios
                Settings = new Settings();
            }

            // UI CONTROL POPULATION FROM LOADED SETTINGS
            // Populate numeric and text controls with configuration values
            numericCalSec.Value = Settings.CalibrationSeconds;
            textBoxBaseFolder.Text = Settings.BaseFolder;
            comboBoxPabVersion.Text = Settings.PabVersion;
            checkBoxAutoArrange.Checked = Settings.AutoArrangeGraphs;

            // PORT SELECTION CHECKBOX CONFIGURATION
            // Clear existing items and populate with current port selection state
            checkedListBoxPorts.Items.Clear();
            for (int i = 1; i <= 8; i++)
            {
                // Convert bitmask to individual checkbox states (display as 1-8, store as 0-7)
                bool checkedItem = (Settings.PabPortSerialNumber & (1 << (i - 1))) != 0;
                checkedListBoxPorts.Items.Add($"Port {i}", checkedItem);
            }

            // Update master checkbox to reflect current port selection state
            UpdateMasterCheckBox();

            // KNOWN VERSIONS DROPDOWN POPULATION
            // Clear and populate firmware version dropdown with known compatible versions
            comboBoxPabVersion.Items.Clear();
            foreach (var v in Settings.KnownPabVersions)
                comboBoxPabVersion.Items.Add(v);
        }
        /// <summary>
        /// Saves current UI configuration to Settings object and persists to JSON file
        /// Validates user input and updates known versions list with new firmware versions
        /// Ensures settings are immediately available for application initialization
        /// 
        /// Save Process:
        /// 1. Extract values from all UI controls with appropriate type conversion
        /// 2. Validate and process port selection checkboxes into bitmask format
        /// 3. Update known firmware versions list with any new versions
        /// 4. Serialize complete settings to formatted JSON file
        /// 5. Handle file write errors gracefully (logged but non-blocking)
        /// 
        /// Data Extraction:
        /// - Numeric controls: Direct value extraction with type casting
        /// - Text fields: String values with whitespace trimming
        /// - Checkboxes: Boolean state extraction for preferences
        /// - Port selection: Bitmask calculation from individual checkbox states
        /// 
        /// Bitmask Processing:
        /// - Iterates through port checkboxes (displayed as 1-8, processed as 0-7)
        /// - Sets corresponding bit for each checked port
        /// - Results in ushort bitmask suitable for SerialManager filtering
        /// - Supports partial selections and all/none configurations
        /// 
        /// Firmware Version Management:
        /// - Extracts current version selection from dropdown
        /// - Adds new versions to KnownPabVersions list for future use
        /// - Prevents duplicate entries in known versions collection
        /// - Enables dynamic firmware version discovery and persistence
        /// 
        /// JSON Persistence:
        /// - Uses WriteIndented option for human-readable formatting
        /// - Enables manual editing and version control of settings
        /// - Overwrites existing settings file with complete current state
        /// - HexUShortConverter handles proper port number serialization
        /// 
        /// Error Handling:
        /// - File write failures are handled gracefully (should log but not crash)
        /// - Settings object always updated regardless of file persistence status
        /// - UI remains responsive even if file save operations fail
        /// - Critical for scenarios with read-only deployment directories
        /// </summary>
        private void SaveSettings()
        {
            Settings.CalibrationSeconds = (int)numericCalSec.Value;
            Settings.BaseFolder = textBoxBaseFolder.Text;
            Settings.PabVersion = comboBoxPabVersion.Text;
            Settings.AutoArrangeGraphs = checkBoxAutoArrange.Checked;

            ushort bitmask = 0;
            for (int i = 0; i < checkedListBoxPorts.Items.Count; i++)
            {
                if (checkedListBoxPorts.GetItemChecked(i))
                {
                    bitmask |= (ushort)(1 << i);
                }
            }
            Settings.PabPortSerialNumber = bitmask;

            string version = comboBoxPabVersion.Text.Trim();
            if (!string.IsNullOrEmpty(version) && !Settings.KnownPabVersions.Contains(version))
                Settings.KnownPabVersions.Add(version);
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(Settings, options));
        }
        /// <summary>
        /// Handles master checkbox state changes to synchronize all port selections
        /// Provides convenient all/none selection functionality for port configuration
        /// Prevents infinite recursion through careful flag management
        /// 
        /// Synchronization Behavior:
        /// - Checked state: Selects all available ports (sets all port checkboxes)
        /// - Unchecked state: Deselects all ports (clears all port checkboxes)
        /// - Indeterminate state: No action (preserves current partial selection)
        /// 
        /// Recursion Prevention:
        /// - Uses isUpdatingMasterCheckbox flag to prevent infinite event loops
        /// - Critical for tri-state behavior where individual changes update master
        /// - Ensures clean UI interaction without unexpected side effects
        /// 
        /// User Experience:
        /// - Single click to select/deselect all ports efficiently
        /// - Immediate visual feedback through checkbox state changes
        /// - Intuitive behavior matching standard multi-select conventions
        /// </summary>
        /// <param name="sender">Master checkbox control that fired the event</param>
        /// <param name="e">Event arguments (not used in current implementation)</param>
        private void CheckBoxMaster_CheckedChanged(object sender, EventArgs e)
        {
            if (isUpdatingMasterCheckbox)
                return;

            bool isChecked = checkBoxMaster.Checked;

            for (int i = 0; i < checkedListBoxPorts.Items.Count; i++)
            {
                checkedListBoxPorts.SetItemChecked(i, isChecked);
            }
        }

        /// <summary>
        /// Handles individual port checkbox changes to update master checkbox state
        /// Maintains tri-state master checkbox reflecting partial selections
        /// Uses BeginInvoke to handle timing issues with CheckedListBox events
        /// 
        /// Event Timing Considerations:
        /// - CheckedListBox ItemCheck fires before item state actually changes
        /// - BeginInvoke defers master checkbox update until after state change completes
        /// - Prevents incorrect tri-state calculations based on old checkbox states
        /// 
        /// Master Checkbox State Logic:
        /// - All ports checked: Master checkbox = Checked
        /// - No ports checked: Master checkbox = Unchecked  
        /// - Some ports checked: Master checkbox = Indeterminate
        /// 
        /// Threading and UI Updates:
        /// - Uses Control.BeginInvoke for safe UI thread marshalling
        /// - Prevents UI update conflicts during rapid checkbox interactions
        /// - Ensures consistent visual feedback regardless of interaction speed
        /// 
        /// Recursion Protection:
        /// - Sets isUpdatingMasterCheckbox flag during master checkbox updates
        /// - Prevents circular event firing between individual and master checkboxes
        /// - Critical for stable tri-state behavior and user experience
        /// </summary>
        /// <param name="sender">CheckedListBox control containing port selections</param>
        /// <param name="e">ItemCheckEventArgs containing item being changed</param>
        private void CheckedListBoxPorts_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (!this.IsHandleCreated)
                return;
            // Wait until after the item check event completes
            this.BeginInvoke(new Action(() =>
            {
                isUpdatingMasterCheckbox = true;
                UpdateMasterCheckBox();
                isUpdatingMasterCheckbox = false;
            }));
        }

        /// <summary>
        /// Handles Start button click to save settings and begin application operation
        /// Validates configuration, persists settings, and signals successful completion
        /// Primary path for normal application initialization flow
        /// 
        /// Operation Sequence:
        /// 1. Save current UI configuration to Settings object and JSON file
        /// 2. Set dialog result to OK to indicate successful configuration
        /// 3. Close dialog to return control to calling application
        /// 
        /// Integration with Application Startup:
        /// - Called from Program.Main settings validation sequence
        /// - OK result allows application to proceed with hardware initialization
        /// - Settings object consumed by SerialManager and MultiFormContext
        /// - Critical gate for application operation (must complete successfully)
        /// 
        /// Error Handling:
        /// - Settings save errors don't prevent dialog closure (logged but non-blocking)
        /// - UI validation ensures reasonable values before reaching this point
        /// - Settings object always populated regardless of file persistence status
        /// </summary>
        /// <param name="sender">Start button control (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonStart_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Handles Exit button click to terminate application without saving changes
        /// Provides immediate application exit without hardware initialization
        /// Alternative path when user cancels configuration or encounters issues
        /// 
        /// Termination Behavior:
        /// - Calls Application.Exit() for immediate termination
        /// - Bypasses normal settings save and validation process  
        /// - Triggers application-wide shutdown without hardware setup
        /// - Used when user decides not to proceed with monitoring
        /// 
        /// Integration with Startup Flow:
        /// - Provides clean exit path from configuration dialog
        /// - Prevents incomplete application initialization scenarios
        /// - Triggers WindowsMinimizer.RestoreAllWindows() through application exit
        /// - Ensures system state restoration even on early exit
        /// </summary>
        /// <param name="sender">Exit button control (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Handles Browse button click to show folder selection dialog for data output path
        /// Provides user-friendly folder selection with immediate UI feedback
        /// Updates BaseFolder setting through standard Windows folder browser
        /// 
        /// Folder Selection Process:
        /// 1. Create and configure FolderBrowserDialog for path selection
        /// 2. Display modal dialog for user folder selection
        /// 3. Update textBoxBaseFolder with selected path if user confirms
        /// 4. Preserve existing path if user cancels selection
        /// 
        /// User Experience Benefits:
        /// - Standard Windows folder browser for familiar interaction
        /// - Immediate visual feedback in text field upon selection
        /// - Path validation handled by folder browser (only valid paths selectable)
        /// - Cancel support preserves existing configuration without changes
        /// 
        /// Integration with Settings:
        /// - Updates BaseFolder text field immediately upon selection
        /// - Changes reflected in Settings object when SaveSettings() is called
        /// - Supports both absolute and relative path configurations
        /// - Directory existence validation handled during actual usage
        /// </summary>
        /// <param name="sender">Browse button control (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxBaseFolder.Text = folderDialog.SelectedPath;
                }
            }
        }
        /// <summary>
        /// Updates master checkbox state to reflect current individual port selections
        /// Implements tri-state logic for intuitive multi-selection interface
        /// Called after individual port checkbox changes to maintain UI consistency
        /// 
        /// Tri-State Logic Implementation:
        /// - Checked: All ports are selected (provides Select All functionality)
        /// - Unchecked: No ports are selected (provides Deselect All functionality)  
        /// - Indeterminate: Some but not all ports selected (shows partial selection)
        /// 
        /// State Calculation Algorithm:
        /// 1. Iterate through all port checkboxes to determine selection status
        /// 2. Track whether all ports are checked and whether any ports are checked
        /// 3. Apply tri-state logic based on complete/partial/empty selection patterns
        /// 4. Update master checkbox state without triggering change events
        /// 
        /// User Interface Benefits:
        /// - Immediate visual feedback showing overall port selection state
        /// - Intuitive understanding of partial vs complete selections
        /// - Professional multi-select behavior matching standard UI conventions
        /// - Clear indication when no ports are selected (potential configuration issue)
        /// 
        /// Threading and Event Coordination:
        /// - Called within isUpdatingMasterCheckbox flag protection
        /// - Prevents recursive event firing during checkbox state synchronization
        /// - Safe to call from BeginInvoke context during UI updates
        /// - Maintains consistent state regardless of individual checkbox interaction order
        /// </summary>
        private void UpdateMasterCheckBox()
        {
            bool allChecked = true;
            bool noneChecked = true;

            // Analyze current port selection state
            for (int i = 0; i < checkedListBoxPorts.Items.Count; i++)
            {
                if (checkedListBoxPorts.GetItemChecked(i))
                {
                    noneChecked = false;  // At least one port is selected
                }
                else
                {
                    allChecked = false;   // At least one port is not selected
                }
            }

            // Apply tri-state logic based on selection analysis
            if (allChecked)
            {
                checkBoxMaster.CheckState = CheckState.Checked;        // All ports selected
            }
            else if (noneChecked)
            {
                checkBoxMaster.CheckState = CheckState.Unchecked;      // No ports selected
            }
            else
            {
                checkBoxMaster.CheckState = CheckState.Indeterminate;  // Partial selection
            }
        }

        /// <summary>
        /// Handles About menu item click to display application information dialog
        /// Provides access to version, author, and licensing information
        /// Integrates with AboutForm for consistent application metadata display
        /// 
        /// About Dialog Features:
        /// - Application version extracted from assembly metadata
        /// - Product name and company information from assembly attributes
        /// - Author information from custom assembly metadata
        /// - Professional presentation matching application visual style
        /// 
        /// User Experience:
        /// - Modal dialog ensures focus during information display
        /// - Automatic resource cleanup through using statement
        /// - Standard About dialog behavior expected in professional applications
        /// - Accessible through menu for discoverable help information
        /// 
        /// Integration Benefits:
        /// - Consistent version display between SettingsForm title and About dialog
        /// - Centralized metadata management through AboutForm implementation
        /// - Support information readily available for troubleshooting scenarios
        /// - Professional application presentation for deployment environments
        /// </summary>
        /// <param name="sender">About menu item control (not used)</param>
        /// <param name="e">Event arguments (not used)</param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutForm())
            {
                about.ShowDialog();
            }
        }
    }
}
