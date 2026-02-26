namespace PABReaderGraph
{
    partial class AboutForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            labelTitle = new Label();
            labelVersion = new Label();
            labelCopyright = new Label();
            labelCompany = new Label();
            SuspendLayout();
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Location = new Point(24, 20);
            labelTitle.Margin = new Padding(5, 0, 5, 0);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(114, 38);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "labeltitle";
            // 
            // labelVersion
            // 
            labelVersion.AutoSize = true;
            labelVersion.Location = new Point(24, 68);
            labelVersion.Margin = new Padding(5, 0, 5, 0);
            labelVersion.Name = "labelVersion";
            labelVersion.Size = new Size(157, 38);
            labelVersion.TabIndex = 1;
            labelVersion.Text = "labelversion";
            // 
            // labelCopyright
            // 
            labelCopyright.AutoSize = true;
            labelCopyright.Location = new Point(24, 164);
            labelCopyright.Margin = new Padding(5, 0, 5, 0);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(187, 38);
            labelCopyright.TabIndex = 2;
            labelCopyright.Text = "labelCopyright";
            // 
            // labelCompany
            // 
            labelCompany.AutoSize = true;
            labelCompany.Location = new Point(24, 116);
            labelCompany.Margin = new Padding(5, 0, 5, 0);
            labelCompany.Name = "labelCompany";
            labelCompany.Size = new Size(184, 38);
            labelCompany.TabIndex = 3;
            labelCompany.Text = "labelCompany";
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new SizeF(15F, 36F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(590, 229);
            Controls.Add(labelCompany);
            Controls.Add(labelCopyright);
            Controls.Add(labelVersion);
            Controls.Add(labelTitle);
            Font = new Font("Comic Neue", 16.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            Text = "About";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelTitle;
        private Label labelVersion;
        private Label labelCopyright;
        private Label labelCompany;
    }
}