public class AutoClosingMessage
{
    public static void Show(string message, int timeoutMilliseconds = 1500)
    {
        string[] lines = message.Split('\n');

        int width = lines.Max(s => s.Length)*10; 
        int height = lines.Length*30;

        Form popup = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedSingle,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(width, height),
            TopMost = true,
            BackColor = Color.White,
            ControlBox = false,
            ShowInTaskbar = false    
        };
        var label = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Comic Neue", 12, FontStyle.Regular),
            ForeColor = Color.Black,
            BorderStyle = BorderStyle.FixedSingle,
        };
        popup.Controls.Add(label);

        var timer = new System.Windows.Forms.Timer { Interval = timeoutMilliseconds };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            popup.Close();
        };
        timer.Start();
        popup.ShowDialog();
    }
}
