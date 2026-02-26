namespace PABReaderGraph
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        // Dispose pattern (auto-generated)
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        // Form Controls
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            labelCalSec = new Label();
            numericCalSec = new NumericUpDown();
            labelPorts = new Label();
            checkBoxMaster = new CheckBox();
            checkedListBoxPorts = new CheckedListBox();
            labelBaseFolder = new Label();
            buttonBrowse = new Button();
            labelPabVersion = new Label();
            checkBoxAutoArrange = new CheckBox();
            buttonStart = new Button();
            buttonExit = new Button();
            menuStrip1 = new MenuStrip();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            comboBoxPabVersion = new ComboBox();
            textBoxBaseFolder = new TextBox();
            ((System.ComponentModel.ISupportInitialize)numericCalSec).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // labelCalSec
            // 
            labelCalSec.AutoSize = true;
            labelCalSec.Location = new Point(11, 34);
            labelCalSec.Name = "labelCalSec";
            labelCalSec.Size = new Size(188, 27);
            labelCalSec.TabIndex = 0;
            labelCalSec.Text = "Calibration Seconds:";
            // 
            // numericCalSec
            // 
            numericCalSec.Location = new Point(192, 35);
            numericCalSec.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            numericCalSec.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericCalSec.Name = "numericCalSec";
            numericCalSec.Size = new Size(120, 30);
            numericCalSec.TabIndex = 1;
            numericCalSec.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // labelPorts
            // 
            labelPorts.AutoSize = true;
            labelPorts.Location = new Point(11, 112);
            labelPorts.Name = "labelPorts";
            labelPorts.Size = new Size(168, 27);
            labelPorts.TabIndex = 2;
            labelPorts.Text = "Select Ports (IDs):";
            // 
            // checkBoxMaster
            // 
            checkBoxMaster.AutoSize = true;
            checkBoxMaster.Location = new Point(192, 85);
            checkBoxMaster.Name = "checkBoxMaster";
            checkBoxMaster.Size = new Size(163, 31);
            checkBoxMaster.TabIndex = 11;
            checkBoxMaster.Text = "Select All Ports";
            checkBoxMaster.CheckedChanged += CheckBoxMaster_CheckedChanged;
            // 
            // checkedListBoxPorts
            // 
            checkedListBoxPorts.FormattingEnabled = true;
            checkedListBoxPorts.Location = new Point(192, 116);
            checkedListBoxPorts.Name = "checkedListBoxPorts";
            checkedListBoxPorts.Size = new Size(120, 154);
            checkedListBoxPorts.TabIndex = 3;
            checkedListBoxPorts.ItemCheck += CheckedListBoxPorts_ItemCheck;
            // 
            // labelBaseFolder
            // 
            labelBaseFolder.AutoSize = true;
            labelBaseFolder.Location = new Point(11, 326);
            labelBaseFolder.Name = "labelBaseFolder";
            labelBaseFolder.Size = new Size(119, 27);
            labelBaseFolder.TabIndex = 4;
            labelBaseFolder.Text = "Base Folder:";
            // 
            // buttonBrowse
            // 
            buttonBrowse.Location = new Point(417, 326);
            buttonBrowse.Name = "buttonBrowse";
            buttonBrowse.Size = new Size(84, 31);
            buttonBrowse.TabIndex = 6;
            buttonBrowse.Text = "Browse...";
            buttonBrowse.UseVisualStyleBackColor = true;
            buttonBrowse.Click += buttonBrowse_Click;
            // 
            // labelPabVersion
            // 
            labelPabVersion.AutoSize = true;
            labelPabVersion.Location = new Point(11, 366);
            labelPabVersion.Name = "labelPabVersion";
            labelPabVersion.Size = new Size(122, 27);
            labelPabVersion.TabIndex = 7;
            labelPabVersion.Text = "PAB Version:";
            // 
            // checkBoxAutoArrange
            // 
            checkBoxAutoArrange.AutoSize = true;
            checkBoxAutoArrange.Location = new Point(192, 296);
            checkBoxAutoArrange.Name = "checkBoxAutoArrange";
            checkBoxAutoArrange.Size = new Size(219, 31);
            checkBoxAutoArrange.TabIndex = 11;
            checkBoxAutoArrange.Text = "Auto Arrange Graphs";
            // 
            // buttonStart
            // 
            buttonStart.DialogResult = DialogResult.OK;
            buttonStart.Location = new Point(187, 410);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(100, 30);
            buttonStart.TabIndex = 9;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += buttonStart_Click;
            // 
            // buttonExit
            // 
            buttonExit.DialogResult = DialogResult.Cancel;
            buttonExit.Location = new Point(297, 410);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(90, 30);
            buttonExit.TabIndex = 10;
            buttonExit.Text = "Exit";
            buttonExit.UseVisualStyleBackColor = true;
            buttonExit.Click += buttonExit_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(528, 29);
            menuStrip1.TabIndex = 12;
            menuStrip1.Text = "menuStrip1";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(66, 25);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // comboBoxPabVersion
            // 
            comboBoxPabVersion.FormattingEnabled = true;
            //comboBoxPabVersion.Items.AddRange(new object[] { "PABG1-12266.02.17.05.23", "PABG3-12324.02.27.01.25" });
            comboBoxPabVersion.Location = new Point(187, 366);
            comboBoxPabVersion.Name = "comboBoxPabVersion";
            comboBoxPabVersion.Size = new Size(200, 35);
            comboBoxPabVersion.TabIndex = 13;
            // 
            // textBoxBaseFolder
            // 
            textBoxBaseFolder.Location = new Point(187, 327);
            textBoxBaseFolder.Name = "textBoxBaseFolder";
            textBoxBaseFolder.Size = new Size(200, 30);
            textBoxBaseFolder.TabIndex = 5;
            // 
            // SettingsForm
            // 
            AcceptButton = buttonStart;
            CancelButton = buttonExit;
            ClientSize = new Size(528, 449);
            Controls.Add(comboBoxPabVersion);
            Controls.Add(buttonExit);
            Controls.Add(buttonStart);
            Controls.Add(checkBoxMaster);
            Controls.Add(checkBoxAutoArrange);
            Controls.Add(labelPabVersion);
            Controls.Add(buttonBrowse);
            Controls.Add(textBoxBaseFolder);
            Controls.Add(labelBaseFolder);
            Controls.Add(checkedListBoxPorts);
            Controls.Add(labelPorts);
            Controls.Add(numericCalSec);
            Controls.Add(labelCalSec);
            Controls.Add(menuStrip1);
            Font = new Font("Comic Neue", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PAB Device Settings";
            ((System.ComponentModel.ISupportInitialize)numericCalSec).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label labelCalSec;
        private System.Windows.Forms.NumericUpDown numericCalSec;
        private System.Windows.Forms.Label labelPorts;
        private CheckBox checkBoxMaster;
        private System.Windows.Forms.CheckedListBox checkedListBoxPorts;
        private System.Windows.Forms.Label labelBaseFolder;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelPabVersion;
        private System.Windows.Forms.CheckBox checkBoxAutoArrange;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Button buttonExit;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ComboBox comboBoxPabVersion;
        private TextBox textBoxBaseFolder;
    }
}
