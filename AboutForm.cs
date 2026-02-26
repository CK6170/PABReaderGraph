using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PABReaderGraph
{
    /// <summary>
    /// About dialog form displaying application metadata and version information
    /// Provides users with essential application details including version, company, and authorship
    /// Automatically extracts information from assembly attributes for consistent branding
    /// 
    /// Features:
    /// - Dynamic version display from assembly metadata
    /// - Automatic product name and company extraction
    /// - Author information from custom assembly attributes
    /// - Professional about dialog presentation
    /// - Consistent branding across application builds
    /// 
    /// Design Philosophy:
    /// - Self-updating: Information comes from assembly attributes, not hardcoded values
    /// - Professional: Clean presentation of essential application metadata
    /// - Maintainable: No need to manually update version strings in code
    /// - Consistent: Uses standard .NET assembly attribute conventions
    /// 
    /// Assembly Attributes Used:
    /// - AssemblyVersion: Numeric version information (major.minor.build.revision)
    /// - AssemblyProductAttribute: Application/product display name
    /// - AssemblyCompanyAttribute: Company or organization name
    /// - AssemblyMetadataAttribute: Custom metadata including author information
    /// 
    /// Usage Context:
    /// - Accessible via Help → About menu item in main application
    /// - Provides users with version information for support purposes
    /// - Displays legal and attribution information
    /// - Helps users verify they have the correct application version
    /// </summary>
    public partial class AboutForm : Form
    {
        /// <summary>
        /// Initializes a new AboutForm instance and populates it with assembly metadata
        /// Extracts version, product, company, and author information using reflection
        /// Automatically updates UI labels with current assembly attribute values
        /// 
        /// Metadata Extraction Process:
        /// 1. Gets executing assembly reference for current application
        /// 2. Extracts version from AssemblyName (handles null version gracefully)
        /// 3. Reads product name from AssemblyProductAttribute
        /// 4. Retrieves company information from AssemblyCompanyAttribute  
        /// 5. Locates author data from custom AssemblyMetadataAttribute
        /// 6. Updates corresponding UI labels with extracted information
        /// 
        /// Error Handling:
        /// - Provides fallback "Unknown" values for missing or null attributes
        /// - Handles version extraction safely with null coalescing
        /// - Gracefully handles missing custom metadata attributes
        /// 
        /// Performance Notes:
        /// - Reflection operations performed once during form initialization
        /// - Assembly attribute lookup is cached by .NET runtime
        /// - Minimal overhead for simple metadata extraction
        /// </summary>
        public AboutForm()
        {
            // Initialize the form UI components from designer
            InitializeComponent();

            // Get reference to the currently executing assembly for metadata extraction
            var asm = Assembly.GetExecutingAssembly();

            // Extract version information with null safety
            // GetName().Version returns null for unversioned assemblies
            var ver = asm.GetName().Version?.ToString() ?? "Unknown Version";

            // Extract product name from standard assembly attribute
            // Falls back to "Unknown Product" if attribute is missing or null
            var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Unknown Product";

            // Extract company/organization name from standard assembly attribute
            // Provides fallback value for missing company information
            var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown Company";

            // Extract author information from custom metadata attribute
            // Searches for "Authors" key in AssemblyMetadataAttribute collection
            // Uses FirstOrDefault with null coalescing for safe extraction
            string author = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "Authors")?.Value ?? "Unknown Author"; 

            // Update UI labels with extracted metadata
            labelTitle.Text = product;              // Main application/product name
            labelVersion.Text = $"Version: {ver}";  // Formatted version string
            labelCompany.Text = company;            // Company/organization name
            labelCopyright.Text = author;           // Author/copyright information
        }
    }
}