namespace PABReaderGraph
{
    partial class GraphForm : Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GraphForm));
            panelTop = new Panel();
            panelBottom = new FlowLayoutPanel();
            buttonZero = new Button();
            buttonRestart = new Button();
            buttonExit = new Button();
            labelStatus = new Label();
            SuspendLayout();
            // 
            // panelTop
            // 
            panelTop.Location = new Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new Size(200, 100);
            panelTop.TabIndex = 0;
            // 
            // panelBottom
            // 
            panelBottom.Location = new Point(0, 0);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(200, 100);
            panelBottom.TabIndex = 0;
            // 
            // buttonZero
            // 
            buttonZero.Location = new Point(0, 0);
            buttonZero.Name = "buttonZero";
            buttonZero.Size = new Size(75, 23);
            buttonZero.TabIndex = 0;
            // 
            // buttonRestart
            // 
            buttonRestart.Location = new Point(0, 0);
            buttonRestart.Name = "buttonRestart";
            buttonRestart.Size = new Size(75, 23);
            buttonRestart.TabIndex = 0;
            // 
            // buttonExit
            // 
            buttonExit.Location = new Point(0, 0);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(75, 23);
            buttonExit.TabIndex = 0;
            // 
            // labelStatus
            // 
            labelStatus.Location = new Point(0, 0);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(100, 23);
            labelStatus.TabIndex = 0;
            // 
            // GraphForm
            // 
            ClientSize = new Size(282, 253);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "GraphForm";
            ResumeLayout(false);
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot formsPlot;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.FlowLayoutPanel panelBottom;
        private System.Windows.Forms.Button buttonZero;
        private System.Windows.Forms.Button buttonRestart;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Button buttonExitAll;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelHeaders;
        private System.Windows.Forms.Panel labelValuesPanel;
        private System.Windows.Forms.Label labelAdcValues;
        private System.Windows.Forms.Label labelFactors;
        private System.Windows.Forms.Label labelZeroOffsets;
        private System.Windows.Forms.Label labelRecord;
        private System.Windows.Forms.Label labelElapsed;
        private System.Windows.Forms.Label labelLC1;
        private System.Windows.Forms.Label labelLC2;
        private System.Windows.Forms.Label labelLC3;
        private System.Windows.Forms.Label labelLC4;
        private System.Windows.Forms.Label labelTotal;

    }
}