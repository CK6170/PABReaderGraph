using System.Reflection;

namespace PABReaderGraph
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            var asm = Assembly.GetExecutingAssembly();
            var ver = asm.GetName().Version?.ToString() ?? "Unknown Version";
            var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Unknown Product";
            var company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown Company";
            string author = asm.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "Authors")?.Value ?? "Unknown Author"; 
            
            labelTitle.Text = product;
            labelVersion.Text = $"Version: {ver}";
            labelCompany.Text = company;    
            labelCopyright.Text = author;
        }
    }
}