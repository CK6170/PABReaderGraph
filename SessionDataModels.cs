using ScottPlot.Plottables;

namespace PABReaderGraph
{
    /// <summary>
    /// Data transfer object containing all information needed to save a session
    /// Encapsulates session data, metadata, and calculated statistics for file export operations
    /// Used by SessionDataManager to perform save operations with all necessary context
    /// </summary>
    public class SessionData
    {
        /// <summary>
        /// Zero-based port identifier for this session data
        /// Used for file naming and identification in multi-port scenarios
        /// </summary>
        public int PortId { get; set; }

        /// <summary>
        /// Complete collection of recorded data points with timestamps and load cell values
        /// Each tuple contains (timestamp, array of 4 load cell values)
        /// </summary>
        public List<(double time, double[] values)> RecordedData { get; set; } = new();

        /// <summary>
        /// Collection of zero calibration markers placed during the session
        /// Used to visually indicate zero calibration points on exported graphs
        /// </summary>
        public List<VerticalLine> ZeroMarkers { get; set; } = new();

        /// <summary>
        /// Total number of data records processed during the session
        /// May differ from RecordedData.Count in windowed display scenarios
        /// </summary>
        public int TotalRecordCount { get; set; }

        /// <summary>
        /// Calculated duration of the session in seconds
        /// Based on the time difference between first and last recorded data points
        /// Returns 0.0 if less than 2 data points are available
        /// </summary>
        public double SessionDuration => RecordedData.Count > 1 
            ? RecordedData.Last().time - RecordedData.First().time 
            : 0.0;

        /// <summary>
        /// Calculated average data acquisition rate in records per second
        /// Computed as total records divided by session duration
        /// Returns 0.0 if session duration is zero or negative
        /// </summary>
        public double AverageRate => SessionDuration > 0 
            ? RecordedData.Count / SessionDuration 
            : 0.0;
    }

    /// <summary>
    /// Result of a session save operation containing status, file paths, and error information
    /// Provides structured feedback for save operations with detailed success/failure context
    /// Supports both successful saves and various skip/error scenarios
    /// </summary>
    public class SaveSessionResult
    {
        /// <summary>
        /// Indicates whether the save operation completed successfully
        /// True for both successful saves and intentional skips, false only for errors
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Detailed error message if the save operation failed
        /// Null for successful operations and skipped saves
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Path to the session folder where files were saved or would have been saved
        /// Available for both successful and failed operations for reference
        /// </summary>
        public string? SessionFolder { get; set; }

        /// <summary>
        /// Collection of full file paths that were successfully created during the save operation
        /// Typically includes JSON, CSV, and PNG files for complete session data export
        /// </summary>
        public List<string> SavedFiles { get; set; } = new();

        /// <summary>
        /// Indicates whether the save operation was intentionally skipped
        /// True when files already exist during Exit All scenarios or when no data is available
        /// </summary>
        public bool WasSkipped { get; set; }

        /// <summary>
        /// Human-readable reason why the save operation was skipped
        /// Provides context for skipped operations, such as "Files already exist during Exit All"
        /// </summary>
        public string? SkipReason { get; set; }

        /// <summary>
        /// Factory method to create a successful save result with file information
        /// Used when save operations complete successfully with files created
        /// </summary>
        /// <param name="sessionFolder">Path to the folder containing the saved files</param>
        /// <param name="savedFiles">List of full paths to files that were created</param>
        /// <returns>SaveSessionResult configured for successful operation</returns>
        public static SaveSessionResult CreateSuccess(string sessionFolder, List<string> savedFiles)
        {
            return new SaveSessionResult
            {
                Success = true,
                SessionFolder = sessionFolder,
                SavedFiles = savedFiles
            };
        }

        /// <summary>
        /// Factory method to create a skipped save result with explanation
        /// Used when save operations are intentionally bypassed due to specific conditions
        /// </summary>
        /// <param name="reason">Human-readable explanation for why the save was skipped</param>
        /// <returns>SaveSessionResult configured for skipped operation</returns>
        public static SaveSessionResult CreateSkipped(string reason)
        {
            return new SaveSessionResult
            {
                Success = true,
                WasSkipped = true,
                SkipReason = reason
            };
        }

        /// <summary>
        /// Factory method to create an error result when save operations fail
        /// Used when exceptions occur or validation fails during save operations
        /// </summary>
        /// <param name="errorMessage">Detailed error message describing the failure</param>
        /// <returns>SaveSessionResult configured for error condition</returns>
        public static SaveSessionResult CreateError(string errorMessage)
        {
            return new SaveSessionResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}