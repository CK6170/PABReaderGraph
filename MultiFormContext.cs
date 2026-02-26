using ScottPlot.Statistics;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PABReaderGraph
{
    public class MultiFormContext : ApplicationContext
    {
        private List<Form> openForms = new List<Form>();
        private Settings currentSettings;
        public MultiFormContext(List<int> selectedIds, Settings settings)
        {
            currentSettings = settings;

            foreach (var id in selectedIds)
            {
                var form = new GraphForm(id, settings);
                SerialManager.Instance.RegisterGraphForm((ushort)id, form);
                form.FormClosed += OnFormClosed;
                openForms.Add(form);
                form.Show();
            }
            if (currentSettings.AutoArrangeGraphs)
                AutoArrangeForms();
        }
        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            var form = sender as Form;
            if (form != null)
            {
                form.FormClosed -= OnFormClosed;
                openForms.Remove(form);

                if (openForms.Count == 0)
                {
                    ExitThread();
                }
            }
        }
        public void CloseAllForms()
        {
            foreach (var form in openForms.ToList())
            {
                if (form != null && !form.IsDisposed)
                {
                    form.FormClosed -= OnFormClosed; // prevent double close  
                    form.Close();
                }
            }
            openForms.Clear();
        }
        public void RestartSession()
        {
            // Close all open GraphForms  
            CloseAllForms();
            // Close SerialManager  
            SerialManager.Instance.Close();
            // Reinitialize SerialManager with same settings  
            try
            {
                SerialManager.Instance.Initialize(currentSettings);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    $"Failed to reinitialize serial port: { ex.Message}",
                    "Error",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Error
                );
                Application.Exit();
                return;
            }
            // Open new GraphForms  
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
                Application.Exit();
                return;
            }
            foreach (var id in selectedIds)
            {
                var form = new GraphForm(id, currentSettings);
                SerialManager.Instance.RegisterGraphForm((ushort)id, form);
                form.FormClosed += OnFormClosed;
                openForms.Add(form);
                form.Show();
            }
            if (currentSettings.AutoArrangeGraphs)
                AutoArrangeForms();
        }
        private void AutoArrangeForms()
        {            
            int formCount = openForms.Count;
            if (formCount == 0)
                return;

            var screens = Screen.AllScreens;
            int screenCount = screens.Length;

            // Distribute forms evenly across screens  
            int formsPerScreen = (int)Math.Ceiling((double)formCount / screenCount);

            int formIndex = 0;

            foreach (var screen in screens)
            {
                int remainingForms = formCount - formIndex;
                int formsThisScreen = Math.Min(formsPerScreen, remainingForms);

                if (formsThisScreen == 0)
                    break;

                // Determine grid for formsThisScreen  
                int rows = (int)Math.Ceiling(Math.Sqrt(formsThisScreen));
                int cols = (int)Math.Ceiling((double)formsThisScreen / rows);

                var workArea = screen.WorkingArea;
                int formW = workArea.Width / cols;
                int formH = workArea.Height / rows;
                for (int i = 0; i < formsThisScreen; i++)
                {
                    int row = i / cols;
                    int col = i % cols;
                    var form = openForms[formIndex++];

                    int margin = 0;

                    if (formsThisScreen % 2 == 1 && i == formsThisScreen - 1)
                    {
                        // Stretch the last (bottom) form
                        form.Left = workArea.Left + margin;
                        form.Top = workArea.Top + row * formH + margin;
                        form.Width = workArea.Width - 2 * margin;
                        form.Height = formH - 2 * margin;
                    }
                    else
                    {
                        form.Left = workArea.Left + col * formW + margin;
                        form.Top = workArea.Top + row * formH + margin;
                        form.Width = formW - 2 * margin;
                        form.Height = formH - 2 * margin;
                    }
                }
            }
        }
        protected override void ExitThreadCore()
        {
            try
            {
                // CLEANUP before exiting  
                SerialManager.Instance.Close();
            }
            catch (Exception ex)
            {
                // Log or ignore if necessary  
                Debug.WriteLine("Error closing SerialManager: " + ex.Message);
            }
            // Restore all windows here!
            WindowsMinimizer.RestoreAllWindows();
            base.ExitThreadCore();
        }

    }
}

