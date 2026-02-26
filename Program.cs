using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PABReaderGraph
{
    static class Program
    {
        public static MultiFormContext? AppContext;

        [STAThread]
        static void Main()
        {
            WindowsMinimizer.MinimizeAllWindows();
            
            void ExitAndRestore()
            {
                AutoClosingMessage.Show(
                            "Goodbye...",
                            timeoutMilliseconds: 1000
                        );
                WindowsMinimizer.RestoreAllWindows();
                Environment.Exit(0);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Settings settings;

            using (var settingsForm = new SettingsForm())
            {
                var result = settingsForm.ShowDialog();
                if (result != DialogResult.OK)
                {                    
                    ExitAndRestore(); // Exit if user clicks Exit
                }
                settings = settingsForm.Settings;
            }
            bool initialized = false;
            while (!initialized)
            {
                try
                {
                    AutoClosingMessage.Show(
                        "Looking for a PAB. Please wait...",
                        timeoutMilliseconds: 5000
                    );
                    SerialManager.Instance.Initialize(settings);
                    initialized = true;
                }
                catch (Exception ex)
                {                    
                    var result = CustomMessageBox.Show(
                        $"Failed to initialize serial port:\n{ex.Message}\nRetry?",
                        "Error",
                        CustomMessageBoxButtons.RetryCancel,
                        CustomMessageBoxIcon.Error
                    );
                    if (result != CustomMessageBoxResult.Retry)
                    {                        
                        ExitAndRestore(); // Exit if user clicks Exit
                    }
                }
            }
            var selectedIds = SerialManager.Instance.GetAvailableIDs();

            if (selectedIds.Count == 0)
            {
                CustomMessageBox.Show(
                    "No valid PAB Ports found (no calibration factors received). Exiting.",
                    "No Ports",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning
                );
                SerialManager.Instance.Close();
                ExitAndRestore(); 
            }
            //selectedIds.Reverse();
            AutoClosingMessage.Show(
                $"PAB Found\nIDs: {string.Join(", ", selectedIds.Select(n => (n+1).ToString()).ToArray())}", 
                timeoutMilliseconds: 1500
            );
            AppContext = new MultiFormContext(selectedIds, settings);
            Application.Run(AppContext);
        }
    }
}
   