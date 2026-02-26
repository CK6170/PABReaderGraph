using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using System.Reflection;

namespace PABReaderGraph
{
    public partial class SettingsForm : Form
    {
        private const string SettingsFile = "settings.json";
        private bool isUpdatingMasterCheckbox = false;
        public Settings Settings { get; private set; }
        public SettingsForm()
        {
            InitializeComponent();

            // Add version to window title
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
            this.Text = $"PAB Device Settings - {versionString}";

            LoadSettings();
        }
        private void LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    Settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                }
                catch
                {
                    Settings = new Settings();
                }
            }
            else
            {
                Settings = new Settings();
            }
            // Populate fields
            numericCalSec.Value = Settings.CalibrationSeconds;
            textBoxBaseFolder.Text = Settings.BaseFolder;
            comboBoxPabVersion.Text = Settings.PabVersion;
            checkBoxAutoArrange.Checked = Settings.AutoArrangeGraphs;
            checkedListBoxPorts.Items.Clear();
            for (int i = 1; i <= 8; i++)
            {
                bool checkedItem = (Settings.PabPortSerialNumber & (1 << (i - 1))) != 0;
                checkedListBoxPorts.Items.Add($"Port {i}", checkedItem);
            }
            UpdateMasterCheckBox();
            comboBoxPabVersion.Items.Clear();
            foreach (var v in Settings.KnownPabVersions)
                comboBoxPabVersion.Items.Add(v);

        }
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
        private void buttonStart_Click(object sender, EventArgs e)
        {            
            SaveSettings();
            DialogResult = DialogResult.OK;
        }
        private void buttonExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
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
        private void UpdateMasterCheckBox()
        {
            bool allChecked = true;
            bool noneChecked = true;

            for (int i = 0; i < checkedListBoxPorts.Items.Count; i++)
            {
                if (checkedListBoxPorts.GetItemChecked(i))
                {
                    noneChecked = false;
                }
                else
                {
                    allChecked = false;
                }
            }
            if (allChecked)
            {
                checkBoxMaster.CheckState = CheckState.Checked;
            }
            else if (noneChecked)
            {
                checkBoxMaster.CheckState = CheckState.Unchecked;
            }
            else
            {
                checkBoxMaster.CheckState = CheckState.Indeterminate;
            }
        }
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var about = new AboutForm())
            {
                about.ShowDialog();
            }
        }
    }
}
