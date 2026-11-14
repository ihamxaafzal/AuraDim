using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyVersion("3.3.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("3.3.0.0")]

namespace LumaDeckInstaller
{
    internal class GradientProgressBar : Control
    {
        private int _value;
        public int Value
        {
            get { return _value; }
            set
            {
                _value = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        public GradientProgressBar()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new RectangleF(0, 0, this.Width - 1, this.Height - 1);
            float rad = this.Height / 2f;

            using (var trackPath = RoundedRect(rect, rad))
            using (var trackBrush = new SolidBrush(Color.FromArgb(20, 27, 45)))
            using (var trackPen = new Pen(Color.FromArgb(45, 58, 90), 1f))
            {
                e.Graphics.FillPath(trackBrush, trackPath);
                e.Graphics.DrawPath(trackPen, trackPath);
            }

            if (_value > 0)
            {
                float fillW = Math.Max(this.Height, (this.Width - 2) * (_value / 100f));
                var fillRect = new RectangleF(1, 1, Math.Min(this.Width - 2, fillW), this.Height - 2);
                Color c2 = (_value >= 100) ? Color.FromArgb(16, 185, 129) : Color.FromArgb(99, 102, 241);
                using (var fillPath = RoundedRect(fillRect, (this.Height - 2) / 2f))
                using (var fillBrush = new LinearGradientBrush(fillRect, Color.FromArgb(56, 189, 248), c2, 0f))
                {
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            }
        }

        private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class SetupForm : Form
    {
        private TextBox txtInstallDir;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkBootStart;
        private CheckBox chkLaunchNow;
        private GradientProgressBar progressBar;
        private Label lblStatus;
        private Label lblPct;
        private Button btnInstall;
        private Button btnClose;
        private Button btnMin;
        private readonly float uiScale;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableBestDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return;
            }
            catch { }
            try { SetProcessDPIAware(); } catch { }
        }

        private int S(int px)
        {
            return Math.Max(1, (int)Math.Round(px * uiScale));
        }

        private static float MeasureSystemScale()
        {
            try
            {
                using (var g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    if (g.DpiX > 0) return Math.Max(1.0f, Math.Min(2.5f, g.DpiX / 96.0f));
                }
            }
            catch { }
            return 1.0f;
        }

        public SetupForm()
        {
            this.uiScale = MeasureSystemScale();
            this.AutoScaleMode = AutoScaleMode.None;

            string defaultDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "LumaDeck");

            Rectangle workArea = Screen.FromPoint(Cursor.Position).WorkingArea;
            int initW = Math.Min(S(640), Math.Max(S(380), workArea.Width - S(32)));
            int initH = Math.Min(S(560), Math.Max(S(420), workArea.Height - S(32)));

            this.Text = "LumaDeck — Setup Wizard";
            this.Size = new Size(initW, initH);
            this.MinimumSize = new Size(Math.Min(S(460), workArea.Width - S(20)), Math.Min(S(400), workArea.Height - S(20)));
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                workArea.Left + (workArea.Width - initW) / 2,
                workArea.Top + (workArea.Height - initH) / 2);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(7, 10, 19);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.Padding = new Padding(1);

            try
            {
                Icon assoc = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (assoc != null) this.Icon = assoc;
            }
            catch { }

            this.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(46, 59, 94), 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // Responsive Gradient Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = S(110),
                BackColor = Color.FromArgb(11, 16, 30)
            };
            header.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                var r = new Rectangle(0, 0, header.Width, header.Height);
                using (var bg = new LinearGradientBrush(r, Color.FromArgb(14, 22, 42), Color.FromArgb(9, 13, 25), 45f))
                {
                    e.Graphics.FillRectangle(bg, r);
                }
                using (var glow = new LinearGradientBrush(new Rectangle(0, header.Height - 2, Math.Max(1, header.Width), 2), Color.FromArgb(56, 189, 248), Color.FromArgb(99, 102, 241), 0f))
                {
                    e.Graphics.FillRectangle(glow, 0, header.Height - 2, header.Width, 2);
                }
                DrawLogo(e.Graphics, S(22), S(23), S(60));
            };
            MouseEventHandler dragHandler = (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
            header.MouseDown += dragHandler;

            int textX = S(96);
            Label lblTitle = new Label
            {
                Text = "LumaDeck Display Studio",
                Font = new Font("Segoe UI Semibold", 15.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(textX, S(20)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(Math.Max(S(200), header.Width - textX - S(92)), S(30)),
                AutoEllipsis = true
            };
            lblTitle.MouseDown += dragHandler;

            Label lblSub = new Label
            {
                Text = "Multi-Monitor & Laptop Brightness Control  •  v3.3",
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                Location = new Point(textX + S(1), S(51)),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(Math.Max(S(200), header.Width - textX - S(30)), S(22)),
                AutoEllipsis = true
            };
            lblSub.MouseDown += dragHandler;

            Label lblTag = new Label
            {
                Text = "PC & LAPTOP EDITION  •  UNIVERSAL ENGINE",
                Font = new Font("Segoe UI", 7.6f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                BackColor = Color.FromArgb(19, 44, 72),
                Location = new Point(textX + S(1), S(76)),
                Padding = new Padding(S(6), S(2), S(6), S(2)),
                AutoSize = true
            };
            lblTag.MouseDown += dragHandler;

            btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(S(34), S(30)),
                Location = new Point(header.Width - S(42), S(10)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(190, 225, 29, 72);
            btnClose.MouseEnter += (s, e) => { btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.ForeColor = Color.FromArgb(148, 163, 184); };
            btnClose.Click += (s, e) => Application.Exit();

            btnMin = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(S(34), S(30)),
                Location = new Point(header.Width - S(78), S(10)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnMin.FlatAppearance.BorderSize = 0;
            btnMin.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 68);
            btnMin.MouseEnter += (s, e) => { btnMin.ForeColor = Color.White; };
            btnMin.MouseLeave += (s, e) => { btnMin.ForeColor = Color.FromArgb(148, 163, 184); };
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            header.Controls.Add(btnClose);
            header.Controls.Add(btnMin);
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);
            header.Controls.Add(lblTag);

            // Responsive Bottom Action Dock (NO weblink at bottom)
            Panel footerDock = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = S(112),
                BackColor = Color.FromArgb(8, 12, 22),
                Padding = new Padding(S(22), S(10), S(22), S(14))
            };
            footerDock.Paint += (s, e) =>
            {
                using (var linePen = new Pen(Color.FromArgb(26, 36, 62), 1f))
                {
                    e.Graphics.DrawLine(linePen, 0, 0, footerDock.Width, 0);
                }
            };

            TableLayoutPanel footerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, S(14)));
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, S(24)));
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            progressBar = new GradientProgressBar
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, S(4)),
                Value = 0
            };

            TableLayoutPanel statusRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            statusRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            lblStatus = new Label
            {
                Text = "Ready — click Install to set up LumaDeck in your selected folder.",
                Font = new Font("Segoe UI", 8.9f),
                ForeColor = Color.FromArgb(160, 174, 198),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0)
            };

            lblPct = new Label
            {
                Text = "0%",
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(S(8), S(2), 0, 0)
            };
            statusRow.Controls.Add(lblStatus, 0, 0);
            statusRow.Controls.Add(lblPct, 1, 0);

            btnInstall = new Button
            {
                Text = "Install LumaDeck Now",
                Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(14, 165, 233),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, S(4), 0, 0),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 178, 245);
            btnInstall.Click += (s, e) => RunInstallation(
                txtInstallDir.Text,
                chkDesktop.Checked,
                chkStartMenu.Checked,
                chkBootStart.Checked,
                chkLaunchNow.Checked,
                false);

            footerLayout.Controls.Add(progressBar, 0, 0);
            footerLayout.Controls.Add(statusRow, 0, 1);
            footerLayout.Controls.Add(btnInstall, 0, 2);
            footerDock.Controls.Add(footerLayout);

            // Scrollable Responsive Body (adapts cleanly to any screen height/width)
            Panel bodyScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(7, 10, 19),
                Padding = new Padding(S(22), S(14), S(22), S(12))
            };

            TableLayoutPanel bodyStack = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            bodyStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bodyStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bodyStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bodyStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 1. Sleek 3-Column Feature Strip
            TableLayoutPanel featureStrip = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = S(36),
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, S(12))
            };
            featureStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            featureStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            featureStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            featureStrip.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            featureStrip.Controls.Add(CreateBadgePill("✓ Auto-Detect Screens", new Padding(0, 0, S(6), 0)), 0, 0);
            featureStrip.Controls.Add(CreateBadgePill("✓ PC + Laptop + Gaming", new Padding(S(3), 0, S(3), 0)), 1, 0);
            featureStrip.Controls.Add(CreateBadgePill("✓ 100% Display Safe", new Padding(S(6), 0, 0, 0)), 2, 0);

            // 2. Responsive Install Location Card
            Panel locPanel = CreateCardPanel(new Padding(S(14), S(12), S(14), S(14)), new Padding(0, 0, 0, S(12)));
            TableLayoutPanel locLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 2
            };
            locLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            locLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            locLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            locLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblLocTitle = new Label
            {
                Text = "INSTALL LOCATION (Select folder for PC or Laptop)",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, S(8))
            };
            locLayout.SetColumnSpan(lblLocTitle, 2);

            txtInstallDir = new TextBox
            {
                Text = defaultDir,
                Font = new Font("Segoe UI", 9.6f),
                BackColor = Color.FromArgb(7, 10, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, S(2), S(10), 0)
            };

            btnBrowse = new Button
            {
                Text = "Browse...",
                Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 41, 68),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(S(92), S(30)),
                Margin = new Padding(0),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
            btnBrowse.FlatAppearance.BorderSize = 1;
            btnBrowse.Click += BtnBrowse_Click;

            locLayout.Controls.Add(lblLocTitle, 0, 0);
            locLayout.Controls.Add(txtInstallDir, 0, 1);
            locLayout.Controls.Add(btnBrowse, 1, 1);
            locPanel.Controls.Add(locLayout);

            // 3. Responsive Options Card
            Panel optPanel = CreateCardPanel(new Padding(S(14), S(10), S(14), S(10)), new Padding(0));
            TableLayoutPanel optLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5
            };
            optLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label lblOptTitle = new Label
            {
                Text = "SETUP PREFERENCES",
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, S(6))
            };
            optLayout.Controls.Add(lblOptTitle, 0, 0);

            chkDesktop = CreateOption("Create Desktop Shortcut (with LumaDeck Logo)", true, S(28));
            chkStartMenu = CreateOption("Add to Windows Start Menu", true, S(28));
            chkBootStart = CreateOption("Launch quietly in System Tray when Windows starts", false, S(28));
            chkLaunchNow = CreateOption("Open LumaDeck immediately after installation", true, S(28));

            optLayout.Controls.Add(chkDesktop, 0, 1);
            optLayout.Controls.Add(chkStartMenu, 0, 2);
            optLayout.Controls.Add(chkBootStart, 0, 3);
            optLayout.Controls.Add(chkLaunchNow, 0, 4);
            optPanel.Controls.Add(optLayout);

            bodyStack.Controls.Add(featureStrip, 0, 0);
            bodyStack.Controls.Add(locPanel, 0, 1);
            bodyStack.Controls.Add(optPanel, 0, 2);
            bodyScroll.Controls.Add(bodyStack);

            // WinForms Dock order matters: add Fill first, then Bottom, then Top
            this.Controls.Add(bodyScroll);
            this.Controls.Add(footerDock);
            this.Controls.Add(header);
        }

        private Panel CreateCardPanel(Padding innerPad, Padding outerMargin)
        {
            Panel p = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(13, 18, 33),
                Padding = innerPad,
                Margin = outerMargin
            };
            p.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(35, 48, 80), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
                }
            };
            p.Resize += (s, e) => p.Invalidate();
            return p;
        }

        private Label CreateBadgePill(string text, Padding margin)
        {
            Label pill = new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 8.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(186, 230, 253),
                BackColor = Color.FromArgb(14, 24, 46),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                Margin = margin
            };
            pill.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(38, 66, 110), 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pill.Width - 1, pill.Height - 1);
                }
            };
            return pill;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int HTLEFT = 10;
            const int HTRIGHT = 11;
            const int HTTOP = 12;
            const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14;
            const int HTBOTTOM = 15;
            const int HTBOTTOMLEFT = 16;
            const int HTBOTTOMRIGHT = 17;

            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST && this.WindowState == FormWindowState.Normal)
            {
                Point pt = this.PointToClient(Cursor.Position);
                int grip = S(6);
                bool left = pt.X <= grip;
                bool right = pt.X >= this.ClientSize.Width - grip;
                bool top = pt.Y <= grip;
                bool bottom = pt.Y >= this.ClientSize.Height - grip;

                if (top && left) m.Result = (IntPtr)HTTOPLEFT;
                else if (top && right) m.Result = (IntPtr)HTTOPRIGHT;
                else if (bottom && left) m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (bottom && right) m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (left) m.Result = (IntPtr)HTLEFT;
                else if (right) m.Result = (IntPtr)HTRIGHT;
                else if (top) m.Result = (IntPtr)HTTOP;
                else if (bottom) m.Result = (IntPtr)HTBOTTOM;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = 2; // DWMWCP_ROUND on Windows 11
                DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
            }
            catch { }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Choose where you want to install LumaDeck:";
                dlg.ShowNewFolderButton = true;
                if (Directory.Exists(txtInstallDir.Text))
                {
                    dlg.SelectedPath = txtInstallDir.Text;
                }
                if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                {
                    string chosen = dlg.SelectedPath.TrimEnd('\\', '/');
                    if (!chosen.EndsWith("LumaDeck", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = Path.Combine(chosen, "LumaDeck");
                    }
                    txtInstallDir.Text = chosen;
                }
            }
        }

        private static CheckBox CreateOption(string text, bool checkedByDefault, int heightPx)
        {
            var chk = new CheckBox
            {
                Text = "  " + text,
                Checked = checkedByDefault,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(226, 232, 240),
                Dock = DockStyle.Top,
                Height = heightPx,
                AutoEllipsis = true,
                Margin = new Padding(0, 1, 0, 1),
                Cursor = Cursors.Hand
            };
            return chk;
        }

        private static void DrawLogo(Graphics g, float x, float y, float size)
        {
            var rect = new RectangleF(x, y, size, size);
            float rad = size * 0.24f;
            using (var path = RoundedRect(rect, rad))
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(14, 165, 233), Color.FromArgb(79, 70, 229), 45f))
            using (var rim = new Pen(Color.FromArgb(110, 255, 255, 255), 1.6f))
            {
                g.FillPath(bg, path);
                g.DrawPath(rim, path);
            }

            float cx = x + size * 0.5f;
            float cy = y + size * 0.45f;
            float sunR = size * 0.145f;
            float rayIn = size * 0.21f;
            float rayOut = size * 0.29f;

            using (var rayPen = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                for (int i = 0; i < 8; i++)
                {
                    double ang = i * Math.PI / 4.0;
                    g.DrawLine(rayPen,
                        cx + (float)(Math.Cos(ang) * rayIn),
                        cy + (float)(Math.Sin(ang) * rayIn),
                        cx + (float)(Math.Cos(ang) * rayOut),
                        cy + (float)(Math.Sin(ang) * rayOut));
                }
            }
            using (var white = new SolidBrush(Color.White))
            {
                g.FillEllipse(white, cx - sunR, cy - sunR, sunR * 2f, sunR * 2f);
            }
            float barW = size * 0.42f;
            float barH = 4f;
            using (var barPath = RoundedRect(new RectangleF(cx - barW / 2f, y + size * 0.77f, barW, barH), 2f))
            using (var barBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                g.FillPath(barBrush, barPath);
            }
        }

        private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void RunInstallation(string targetDir, bool desktopShortcut, bool startMenuShortcut, bool bootStart, bool launchNow, bool silent)
        {
            try
            {
                string installDir = (targetDir ?? "").Trim();
                if (string.IsNullOrEmpty(installDir))
                {
                    throw new Exception("Please choose a valid installation folder.");
                }

                btnInstall.Enabled = false;
                btnBrowse.Enabled = false;
                txtInstallDir.Enabled = false;

                UpdateProgress(12, "Closing any running LumaDeck instance...");
                foreach (var p in Process.GetProcessesByName("LumaDeck"))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }

                UpdateProgress(32, "Preparing destination folder...");
                Directory.CreateDirectory(installDir);

                // Verify write permission cleanly
                string writeProbe = Path.Combine(installDir, ".writetest_" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(writeProbe, "ok");
                File.Delete(writeProbe);

                UpdateProgress(52, "Extracting LumaDeck Display Studio...");
                string tempZip = Path.Combine(Path.GetTempPath(), "LumaDeckPayload_" + Guid.NewGuid().ToString("N") + ".zip");
                using (Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LumaDeckPayload.zip"))
                {
                    if (resStream == null) throw new Exception("Embedded payload resource not found.");
                    using (FileStream fs = File.Create(tempZip))
                    {
                        resStream.CopyTo(fs);
                    }
                }

                using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string destPath = Path.Combine(installDir, entry.FullName);
                        string destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
                        entry.ExtractToFile(destPath, true);
                    }
                }
                try { File.Delete(tempZip); } catch { }

                string exePath = Path.Combine(installDir, "LumaDeck.exe");
                string icoPath = Path.Combine(installDir, "lumadeck.ico");
                if (!File.Exists(icoPath)) icoPath = exePath;

                UpdateProgress(76, "Creating shortcuts with LumaDeck logo...");
                if (desktopShortcut)
                {
                    string desktopLink = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LumaDeck.lnk");
                    CreateShortcut(desktopLink, exePath, installDir, icoPath, "LumaDeck Multi-Display Brightness Studio");
                }

                if (startMenuShortcut)
                {
                    string startMenuDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "LumaDeck");
                    Directory.CreateDirectory(startMenuDir);
                    string startLink = Path.Combine(startMenuDir, "LumaDeck.lnk");
                    CreateShortcut(startLink, exePath, installDir, icoPath, "LumaDeck Multi-Display Brightness Studio");
                }

                UpdateProgress(90, "Configuring startup & Windows app entry...");
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (runKey != null)
                    {
                        if (bootStart)
                            runKey.SetValue("LumaDeck", "\"" + exePath + "\" --tray");
                        else
                            runKey.DeleteValue("LumaDeck", false);
                    }
                }

                string startMenuFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "LumaDeck");
                string desktopShortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "LumaDeck.lnk");
                string uninstallCmd =
                    "cmd.exe /c " +
                    "taskkill /f /im LumaDeck.exe >nul 2>&1 & " +
                    "rmdir /s /q \"" + installDir + "\" >nul 2>&1 & " +
                    "rmdir /s /q \"" + startMenuFolder + "\" >nul 2>&1 & " +
                    "del /f /q \"" + desktopShortcutPath + "\" >nul 2>&1 & " +
                    "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run\" /v LumaDeck /f >nul 2>&1 & " +
                    "reg delete \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\LumaDeck\" /f >nul 2>&1";

                using (RegistryKey uninst = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\LumaDeck"))
                {
                    if (uninst != null)
                    {
                        uninst.SetValue("DisplayName", "LumaDeck");
                        uninst.SetValue("DisplayIcon", icoPath);
                        uninst.SetValue("InstallLocation", installDir);
                        uninst.SetValue("Publisher", "iBlue Apps");
                        uninst.SetValue("URLInfoAbout", "https://iammhamza.com/");
                        uninst.SetValue("DisplayVersion", "3.3");
                        uninst.SetValue("NoModify", 1, RegistryValueKind.DWord);
                        uninst.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                        uninst.SetValue("EstimatedSize", 2048, RegistryValueKind.DWord);
                        uninst.SetValue("UninstallString", uninstallCmd);
                        uninst.SetValue("QuietUninstallString", uninstallCmd);
                    }
                }

                UpdateProgress(100, "Installation Complete! Launching LumaDeck...");
                btnInstall.Text = "✓ Installed Successfully";
                btnInstall.BackColor = Color.FromArgb(16, 185, 129);

                if (launchNow && File.Exists(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = installDir,
                        UseShellExecute = true
                    });
                }

                if (silent)
                {
                    Environment.Exit(0);
                }
                else
                {
                    var closeTimer = new System.Windows.Forms.Timer { Interval = 1100 };
                    closeTimer.Tick += (s, ev) => { closeTimer.Stop(); Application.Exit(); };
                    closeTimer.Start();
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (!silent)
                {
                    MessageBox.Show(
                        "Cannot write to that protected folder without Administrator rights.\nPlease choose a user folder (or run LumaDeckSetup.exe as Administrator).",
                        "Permission Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                btnInstall.Enabled = true;
                btnBrowse.Enabled = true;
                txtInstallDir.Enabled = true;
            }
            catch (Exception ex)
            {
                if (!silent)
                    MessageBox.Show("Installation failed: " + ex.Message, "LumaDeck Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnInstall.Enabled = true;
                btnBrowse.Enabled = true;
                txtInstallDir.Enabled = true;
            }
        }

        private void UpdateProgress(int val, string status)
        {
            if (progressBar != null) progressBar.Value = val;
            if (lblPct != null) lblPct.Text = val + "%";
            if (lblStatus != null) lblStatus.Text = status;
            Application.DoEvents();
        }

        private static void CreateShortcut(string shortcutPath, string targetExe, string workDir, string iconLocation, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetExe;
                shortcut.WorkingDirectory = workDir;
                shortcut.IconLocation = iconLocation;
                shortcut.Description = description;
                shortcut.Save();
            }
            catch { }
        }
    }

    internal static class InstallerProgram
    {
        [STAThread]
        private static void Main(string[] args)
        {
            SetupForm.EnableBestDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool silent = false;
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (string.Equals(a, "--silent", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(a, "/S", StringComparison.OrdinalIgnoreCase))
                    {
                        silent = true;
                    }
                }
            }

            var form = new SetupForm();
            if (silent)
            {
                string defaultDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "LumaDeck");
                form.RunInstallation(defaultDir, true, true, false, true, true);
                return;
            }

            Application.Run(form);
        }
    }
}
