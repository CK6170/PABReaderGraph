using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PABReaderGraph
{
    /// <summary>
    /// Manages all session data saving operations including JSON, CSV, and graph exports
    /// Provides both synchronous and asynchronous save operations with comprehensive error handling
    /// Supports duplicate detection, emergency folder creation, and multi-format data export
    /// 
    /// Export Formats:
    /// - JSON: Structured data with metadata for programmatic access
    /// - CSV: Tabular format for spreadsheet analysis and data processing
    /// - PNG: Visual graph representation for reports and documentation
    /// 
    /// Features:
    /// - Thread-safe operations with proper exception handling
    /// - Duplicate file detection for Exit All scenarios
    /// - Emergency session folder creation for early exits
    /// - Structured result feedback with detailed status information
    /// </summary>
    public class SessionDataManager
    {
        /// <summary>
        /// Saves session data synchronously for UI thread compatibility
        /// Primary method for manual saves and single-form exits with immediate feedback
        /// Provides comprehensive error handling with detailed result information
        /// </summary>
        /// <param name="sessionData">Complete session data including measurements and markers</param>
        /// <param name="skipIfExists">If true, skips save when files already exist (for Exit All)</param>
        /// <returns>SaveSessionResult containing operation status and file information</returns>
        public SaveSessionResult SaveSessionData(SessionData sessionData, bool skipIfExists = false)
        {
            try
            {
                return SaveSessionDataInternal(sessionData, skipIfExists);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Port {sessionData.PortId + 1}: Error saving session data: {ex.Message}";
                Debug.WriteLine(errorMsg);
                return SaveSessionResult.CreateError(errorMsg);
            }
        }

        /// <summary>
        /// Saves session data asynchronously for improved performance and UI responsiveness
        /// Offloads file I/O operations to background thread to prevent UI blocking
        /// Ideal for large datasets or when multiple save operations are performed
        /// </summary>
        /// <param name="sessionData">Complete session data including measurements and markers</param>
        /// <param name="skipIfExists">If true, skips save when files already exist (for Exit All)</param>
        /// <returns>Task containing SaveSessionResult with operation status and file information</returns>
        public async Task<SaveSessionResult> SaveSessionDataAsync(SessionData sessionData, bool skipIfExists = false)
        {
            try
            {
                return await Task.Run(() => SaveSessionDataInternal(sessionData, skipIfExists));
            }
            catch (Exception ex)
            {
                var errorMsg = $"Port {sessionData.PortId + 1}: Error saving session data: {ex.Message}";
                Debug.WriteLine(errorMsg);
                return SaveSessionResult.CreateError(errorMsg);
            }
        }

        /// <summary>
        /// Internal implementation that performs the actual save operation
        /// Coordinates validation, folder management, file generation, and result creation
        /// Provides centralized logic for both synchronous and asynchronous save paths
        /// </summary>
        /// <param name="sessionData">Session data to save</param>
        /// <param name="skipIfExists">Whether to skip saving if files already exist</param>
        /// <returns>Detailed result of the save operation</returns>
        private SaveSessionResult SaveSessionDataInternal(SessionData sessionData, bool skipIfExists)
        {
            if (sessionData.RecordedData.Count == 0)
            {
                Debug.WriteLine($"Port {sessionData.PortId + 1}: No session data to save - recordedData is empty.");
                return SaveSessionResult.CreateSkipped("No data to save");
            }

            string sessionFolder = GetOrCreateSessionFolder();
            var filePaths = GenerateFilePaths(sessionFolder, sessionData.PortId);

            // Check for duplicate files only if skipIfExists is true (Exit All scenario)
            if (skipIfExists && AllFilesExist(filePaths))
            {
                Debug.WriteLine($"Port {sessionData.PortId + 1}: Session files already exist, skipping duplicate save during Exit All.");
                return SaveSessionResult.CreateSkipped("Files already exist during Exit All");
            }

            var savedFiles = new List<string>();

            // Export to JSON
            ExportToJson(sessionData, filePaths.JsonPath);
            savedFiles.Add(filePaths.JsonPath);

            // Export to CSV
            ExportToCsv(sessionData, filePaths.CsvPath);
            savedFiles.Add(filePaths.CsvPath);

            // Export to PNG graph
            ExportToGraph(sessionData, filePaths.GraphPath);
            savedFiles.Add(filePaths.GraphPath);

            Debug.WriteLine($"Port {sessionData.PortId + 1}: Session data saved - {sessionData.RecordedData.Count} records to {sessionFolder}");
            
            return SaveSessionResult.CreateSuccess(sessionFolder, savedFiles);
        }

        /// <summary>
        /// Retrieves the current session folder or creates an emergency folder if none exists
        /// Handles early exit scenarios where SerialManager hasn't established a session folder
        /// Creates timestamped folders with consistent naming convention for data organization
        /// </summary>
        /// <returns>Valid session folder path for file operations</returns>
        private string GetOrCreateSessionFolder()
        {
            string sessionFolder = SerialManager.Instance.SessionFolder;

            if (string.IsNullOrEmpty(sessionFolder))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                sessionFolder = Path.Combine(Directory.GetCurrentDirectory(), "Results", $"results_{timestamp}");
                Directory.CreateDirectory(sessionFolder);
                Debug.WriteLine($"Created emergency session folder: {sessionFolder}");
            }

            return sessionFolder;
        }

        /// <summary>
        /// Generates standardized file paths for all export formats
        /// Creates consistent naming convention: port_X.json, port_X.csv, port_X.png
        /// Ensures all file formats are saved to the same session folder for organization
        /// </summary>
        /// <param name="sessionFolder">Base folder for session files</param>
        /// <param name="portId">Zero-based port identifier for file naming</param>
        /// <returns>Tuple containing paths for JSON, CSV, and PNG files</returns>
        private (string JsonPath, string CsvPath, string GraphPath) GenerateFilePaths(string sessionFolder, int portId)
        {
            var jsonPath = Path.Combine(sessionFolder, $"port_{portId + 1}.json");
            var csvPath = Path.Combine(sessionFolder, $"port_{portId + 1}.csv");
            var graphPath = Path.Combine(sessionFolder, $"port_{portId + 1}.png");
            return (jsonPath, csvPath, graphPath);
        }

        /// <summary>
        /// Checks whether all session files already exist at their expected locations
        /// Used for duplicate detection during Exit All scenarios to prevent overwriting
        /// Verifies existence of JSON, CSV, and PNG files as a complete set
        /// </summary>
        /// <param name="filePaths">Tuple containing all file paths to check</param>
        /// <returns>True if all files exist, false if any are missing</returns>
        private bool AllFilesExist((string JsonPath, string CsvPath, string GraphPath) filePaths)
        {
            return File.Exists(filePaths.JsonPath) && 
                   File.Exists(filePaths.CsvPath) && 
                   File.Exists(filePaths.GraphPath);
        }

        /// <summary>
        /// Exports session data to JSON format with structured metadata
        /// Creates human-readable JSON with proper indentation for analysis and debugging
        /// Includes timestamps, port identification, individual load cell values, and calculated totals
        /// </summary>
        /// <param name="sessionData">Session data to export</param>
        /// <param name="jsonPath">Target file path for JSON output</param>
        private void ExportToJson(SessionData sessionData, string jsonPath)
        {
            var jsonData = sessionData.RecordedData.Select(d => new
            {
                time = d.time,
                id = sessionData.PortId,
                weights = d.values,
                totalWeight = d.values.Sum()
            });

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(jsonData, jsonOptions);
            File.WriteAllText(jsonPath, jsonString);
        }

        /// <summary>
        /// Exports session data to CSV format for spreadsheet analysis
        /// Creates tabular data with headers for easy import into Excel or data analysis tools
        /// Includes timestamp, individual load cell readings, and calculated total weight
        /// </summary>
        /// <param name="sessionData">Session data to export</param>
        /// <param name="csvPath">Target file path for CSV output</param>
        private void ExportToCsv(SessionData sessionData, string csvPath)
        {
            var csvLines = new List<string> { "Time,LC1,LC2,LC3,LC4,Total" };
            
            csvLines.AddRange(sessionData.RecordedData.Select(d =>
                $"{d.time:F3},{d.values[0]},{d.values[1]},{d.values[2]},{d.values[3]},{d.values.Sum()}"));
            
            File.WriteAllLines(csvPath, csvLines);
        }

        /// <summary>
        /// Exports session data to PNG graph format for visual analysis
        /// Creates high-resolution plot image with all data series, zero markers, and statistics
        /// Uses PlotManager for consistent styling and comprehensive data visualization
        /// </summary>
        /// <param name="sessionData">Session data to plot and export</param>
        /// <param name="graphPath">Target file path for PNG image output</param>
        private void ExportToGraph(SessionData sessionData, string graphPath)
        {
            var plot = new ScottPlot.Plot();
            PlotManager.AddSessionPlot(plot, sessionData.RecordedData, sessionData.ZeroMarkers, sessionData.PortId);
            plot.SavePng(graphPath, 1200, 800);
        }

        /// <summary>
        /// Creates user-friendly status messages based on save operation results
        /// Provides appropriate feedback for success, error, and skip scenarios
        /// Formats file lists and error information for display in UI notifications
        /// </summary>
        /// <param name="result">Save operation result containing status and details</param>
        /// <param name="portId">Port identifier for message context</param>
        /// <returns>Formatted message string ready for user display</returns>
        public string CreateSaveMessage(SaveSessionResult result, int portId)
        {
            if (!result.Success)
            {
                return $"Error saving Port {portId + 1} session data: {result.ErrorMessage}";
            }

            if (result.WasSkipped)
            {
                return $"Port {portId + 1} save skipped: {result.SkipReason}";
            }

            var fileList = string.Join("\n", result.SavedFiles);
            return $"Port {portId + 1} session data saved:\n{fileList}";
        }
    }
}