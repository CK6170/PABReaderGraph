using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public enum CustomMessageBoxResult
{
    Ok,
    Cancel,
    Retry,
    None
}

public enum CustomMessageBoxButtons
{
    OK,
    OKCancel,
    RetryCancel
}

public enum CustomMessageBoxIcon
{
    None,
    Error,
    Warning,
    Information,
    Question
}

public static class CustomMessageBox
{
    public static CustomMessageBoxResult Show(
        string message,
        string title = "",
        CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK,
        CustomMessageBoxIcon icon = CustomMessageBoxIcon.None,
        int? width = null,
        int? height = null)
    {
        // Basic settings
        int iconWidth = icon == CustomMessageBoxIcon.None ? 0 : 48;
        int buttonPanelHeight = 70;
        int minWidth = 340;
        int minHeight = 180;
        int leftMargin = icon == CustomMessageBoxIcon.None ? 26 : 48 + iconWidth;
        int topMargin = 32;
        int labelPadding = 16;

        Font messageFont = new Font("Comic Neue", 12, FontStyle.Regular);

        // Calculate the maximum width for the label
        int defaultWidth = width ?? Math.Max(minWidth, message.Split('\n').Max(s => s.Length) * 9 + 100 + iconWidth);
        int maxLabelWidth = defaultWidth - leftMargin - labelPadding;

        // Dynamically measure required label size
        Size textSize;
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
        {
            textSize = TextRenderer.MeasureText(g, message, messageFont, new Size(maxLabelWidth, 0), TextFormatFlags.WordBreak);
        }
        int labelHeight = Math.Max(textSize.Height, iconWidth);

        // Calculate required form height
        int contentHeight = Math.Max(labelHeight, iconWidth) + topMargin + 8; // + margin
        int totalHeight = (height ?? (contentHeight + buttonPanelHeight + 36));
        totalHeight = Math.Max(totalHeight, minHeight);

        int w = defaultWidth;
        int h = totalHeight;

        using (Form popup = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedSingle,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(w, h),
            MinimumSize = new Size(minWidth, minHeight),
            TopMost = true,
            BackColor = Color.White,
            ControlBox = false,
            ShowInTaskbar = false,
            Text = "",
        })
        {
            // Panel for border
            var borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle // Draws a black border!
            };
            // Panel for message and icon (Fill)
            var panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            // Icon
            PictureBox? picture = null;
            if (icon != CustomMessageBoxIcon.None)
            {
                picture = new PictureBox
                {
                    Image = GetIconBitmap(icon),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Width = iconWidth,
                    Height = iconWidth,
                    Left = 26,
                    Top = topMargin,
                    BackColor = Color.White
                };
                panelContent.Controls.Add(picture);
            }
            // Label for the message
            var label = new Label
            {
                Text = message,
                AutoSize = false,
                Height = labelHeight,
                Width = maxLabelWidth,
                Top = topMargin,
                Left = leftMargin,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = messageFont,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            panelContent.Controls.Add(label);
            // Panel for buttons
            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = buttonPanelHeight,
                Padding = new Padding(0, 4, 0, 8),
                BackColor = Color.White
            };
            Button btnOK = new Button
            {
                Text = "OK",
                Width = 100,
                Height = 36,
                DialogResult = DialogResult.OK,
                Font = new Font("Comic Neue", 12, FontStyle.Bold),
                BackColor = Color.WhiteSmoke
            };
            Button btnCancel = new Button
            {
                Text = "Cancel",
                Width = 100,
                Height = 36,
                DialogResult = DialogResult.Cancel,
                Font = new Font("Comic Neue", 12, FontStyle.Bold),
                BackColor = Color.WhiteSmoke
            };
            Button btnRetry = new Button
            {
                Text = "Retry",
                Width = 100,
                Height = 36,
                DialogResult = DialogResult.Retry,
                Font = new Font("Comic Neue", 12, FontStyle.Bold),
                BackColor = Color.WhiteSmoke
            };

            CustomMessageBoxResult result = CustomMessageBoxResult.None;
            // Button arrangement
            int btnTop = 14;
            if (buttons == CustomMessageBoxButtons.OK)
            {
                btnOK.Click += (s, e) => { result = CustomMessageBoxResult.Ok; popup.Close(); };
                btnOK.Location = new Point((w - btnOK.Width) / 2, btnTop);
                panelButtons.Controls.Add(btnOK);
                popup.AcceptButton = btnOK;
            }
            else if (buttons == CustomMessageBoxButtons.OKCancel)
            {
                btnOK.Click += (s, e) => { result = CustomMessageBoxResult.Ok; popup.Close(); };
                btnCancel.Click += (s, e) => { result = CustomMessageBoxResult.Cancel; popup.Close(); };
                int spacing = 16;
                int totalBtnWidth = btnOK.Width + btnCancel.Width + spacing;
                int leftStart = (w - totalBtnWidth) / 2;
                btnOK.Location = new Point(leftStart, btnTop);
                btnCancel.Location = new Point(leftStart + btnOK.Width + spacing, btnTop);
                panelButtons.Controls.Add(btnOK);
                panelButtons.Controls.Add(btnCancel);
                popup.AcceptButton = btnOK;
                popup.CancelButton = btnCancel;
            }
            else if (buttons == CustomMessageBoxButtons.RetryCancel)
            {
                btnRetry.Click += (s, e) => { result = CustomMessageBoxResult.Retry; popup.Close(); };
                btnCancel.Click += (s, e) => { result = CustomMessageBoxResult.Cancel; popup.Close(); };
                int spacing = 16;
                int totalBtnWidth = btnRetry.Width + btnCancel.Width + spacing;
                int leftStart = (w - totalBtnWidth) / 2;
                btnRetry.Location = new Point(leftStart, btnTop);
                btnCancel.Location = new Point(leftStart + btnRetry.Width + spacing, btnTop);
                panelButtons.Controls.Add(btnRetry);
                panelButtons.Controls.Add(btnCancel);
                popup.AcceptButton = btnRetry;
                popup.CancelButton = btnCancel;
            }
            // Add panels to form
            borderPanel.Controls.Add(panelContent);
            borderPanel.Controls.Add(panelButtons);
            // Add borderPanel to popup
            popup.Controls.Add(borderPanel);
            popup.ShowDialog();
            return result;
        }
    }
    private static Bitmap? GetIconBitmap(CustomMessageBoxIcon iconType)
    {
        return iconType switch
        {
            CustomMessageBoxIcon.Error => SystemIcons.Error.ToBitmap(),
            CustomMessageBoxIcon.Warning => SystemIcons.Warning.ToBitmap(),
            CustomMessageBoxIcon.Information => SystemIcons.Information.ToBitmap(),
            CustomMessageBoxIcon.Question => SystemIcons.Question.ToBitmap(),
            _ => null,
        };
    }
}
