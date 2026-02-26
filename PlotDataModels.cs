namespace PABReaderGraph
{
    /// <summary>
    /// Data models and enums for plot data management and performance optimization
    /// Provides type-safe access to load cell data and efficient caching mechanisms
    /// Reduces memory allocations and LINQ operations in performance-critical paths
    /// </summary>

    /// <summary>
    /// Type-safe enumeration for load cell line indexing in arrays and collections
    /// Prevents magic number usage and provides compile-time safety for data access
    /// Maps to the four individual load cells plus the calculated total weight
    /// </summary>
    public enum LineIndex
    {
        /// <summary>Load Cell 1 data index (first sensor)</summary>
        LC1 = 0,
        /// <summary>Load Cell 2 data index (second sensor)</summary>
        LC2 = 1,
        /// <summary>Load Cell 3 data index (third sensor)</summary>
        LC3 = 2,
        /// <summary>Load Cell 4 data index (fourth sensor)</summary>
        LC4 = 3,
        /// <summary>Calculated total weight index (sum of all sensors)</summary>
        Total = 4
    }

    /// <summary>
    /// High-performance cached plot data container for real-time visualization
    /// Pre-computed arrays reduce LINQ operations in the UI update hot path (UpdateTimer_Tick)
    /// Optimizes memory usage and plotting performance for smooth real-time updates
    /// 
    /// Performance Benefits:
    /// - Eliminates repeated LINQ operations on raw data collections
    /// - Provides direct array access for ScottPlot operations  
    /// - Reduces garbage collection pressure from temporary objects
    /// - Enables efficient batch updates to plot visualization
    /// </summary>
    public class CachedPlotData
    {
        /// <summary>
        /// Array of timestamps corresponding to each data point
        /// Used as X-axis values for all plot series in time-series visualization
        /// </summary>
        public double[] Times { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Array of Load Cell 1 weight measurements over time
        /// Pre-computed from raw data for direct plotting performance
        /// </summary>
        public double[] LC1 { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Array of Load Cell 2 weight measurements over time  
        /// Pre-computed from raw data for direct plotting performance
        /// </summary>
        public double[] LC2 { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Array of Load Cell 3 weight measurements over time
        /// Pre-computed from raw data for direct plotting performance
        /// </summary>
        public double[] LC3 { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Array of Load Cell 4 weight measurements over time
        /// Pre-computed from raw data for direct plotting performance
        /// </summary>
        public double[] LC4 { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Array of calculated total weight measurements (sum of all load cells)
        /// Pre-computed from individual load cell values for performance
        /// </summary>
        public double[] Total { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Type-safe accessor for load cell data by index enumeration
        /// Provides dynamic access to specific load cell arrays using LineIndex enum
        /// Enables generic processing while maintaining type safety and performance
        /// </summary>
        /// <param name="index">LineIndex enum value specifying which data set to retrieve</param>
        /// <returns>Reference to the requested load cell data array</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when index is not a valid LineIndex value</exception>
        public double[] GetLCData(LineIndex index) => index switch
        {
            LineIndex.LC1 => LC1,
            LineIndex.LC2 => LC2,
            LineIndex.LC3 => LC3,
            LineIndex.LC4 => LC4,
            LineIndex.Total => Total,
            _ => throw new ArgumentOutOfRangeException(nameof(index), $"Invalid LineIndex: {index}")
        };
    }
}