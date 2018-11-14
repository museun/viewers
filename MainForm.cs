namespace viewers {
    using System;
    using System.Drawing;
    using System.Net.Http;
    using System.Windows.Forms;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Timer = System.Timers.Timer;
    using Newtonsoft.Json;
    using System.Runtime.InteropServices;
    using System.ComponentModel;
    using System.IO;

    public class MainForm : Form, IMessageFilter {
        private readonly HttpClient client = new HttpClient();
        private readonly Timer timer = new Timer { Interval = 30.0 * 1000 };
        private readonly Font font = new Font("Iosevka", 28.0F, FontStyle.Regular, GraphicsUnit.Point, 0);

        private const int IconSize = 40;

        private readonly string name;

        private readonly Dictionary<Keys, Action<MainForm, KeyEventArgs>> keyBindings =
            new Dictionary<Keys, Action<MainForm, KeyEventArgs>> {
                { Keys.Q, (f,a) => { f.timer.Stop(); f.Close(); } },
                { Keys.T, (f,a) => f.TopMost = !f.TopMost }
        };

        public MainForm(string name, string client_id) {
            this.name = name;

            Application.AddMessageFilter(this);
            InitializeComponent();

            client.DefaultRequestHeaders.Add("Client-ID", client_id);

            timer.Elapsed += (s, e) => {
                if (this.label.InvokeRequired) {
                    this.Invoke((MethodInvoker)(async () => await UpdateViewerCount()));
                }
            };
            timer.Start();
        }

        private void ResizeWindow(string text) {
            var size = TextRenderer.MeasureText(text, font);
            this.MinimumSize = new Size(IconSize + size.Width, IconSize + 2); // arbitray numbers
            this.Width = IconSize + size.Width;
        }

        private async Task<string> GetViewers() {
            var json = await client.GetStringAsync("https://api.twitch.tv/helix/streams?user_login=" + this.name);
            var result = JsonConvert.DeserializeAnonymousType(json, new {
                data = new[] { new { viewer_count = default(int) } }
            });
            var viewers = result?.data?[0].viewer_count ?? 0;
            return viewers.ToString();
        }

        private async Task UpdateViewerCount() {
            var text = "0";
            try {
                text = await GetViewers();
            }
            catch (System.Exception) {
                // silently ignore any failures.
            }

            ResizeWindow(text);
            this.label.Text = text;
        }

        protected override async void OnLoad(EventArgs e) {
            base.OnLoad(e);

            MinimumSize = new Size(100, 42);
            Width = 100;

            await UpdateViewerCount();
        }

        private void OnMouseWheel(object sender, MouseEventArgs ev) {
            if (ev.Delta == 0) return;

            var current = this.Opacity;
            if (ev.Delta > 0) {
                this.Opacity = Math.Min(current + 0.05, 1.0);
            }
            else {
                this.Opacity = Math.Max(current - 0.05, 0.35);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs ev) {
            if (keyBindings.TryGetValue(ev.KeyCode, out var action)) {
                action(this, ev);
            }
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) {
            base.SetBoundsCore(x, y, MinimumSize.Width, MinimumSize.Height, specified);
        }

        public bool PreFilterMessage(ref Message m) {
            if (m.Msg == 0x0201 && (m.WParam.ToInt32() & 0x0004) != 0) {
                ReleaseCapture();
                // nonclient l button down, click on the "title bar"
                SendMessage(this.Handle, 0x00A1, (IntPtr)0x0002, (IntPtr)0x0000);
                return true;
            }

            if (m.Msg == 0x020A) {
                var lp = m.LParam.ToInt32();
                var hwnd = WindowFromPoint(new Point(lp & 0xFFFF, lp));
                // sanity check
                if (hwnd == IntPtr.Zero || hwnd == m.HWnd || FromHandle(hwnd) == null) { return false; }

                SendMessage(hwnd, m.Msg, m.WParam, m.LParam);
                return true;
            }
            return false;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private void InitializeComponent() {
            var resources = new ComponentResourceManager(typeof(MainForm));
            this.label = new Label();
            this.twitchIcon = new PictureBox();

            ((ISupportInitialize)this.twitchIcon).BeginInit();
            this.SuspendLayout();

            this.label.BackColor = Color.FromArgb(15, 14, 17);
            this.label.Dock = DockStyle.Fill;
            this.label.Font = font;
            this.label.ForeColor = Color.FromArgb(100, 65, 164);
            this.label.Location = new Point(0, -5);
            // this.label.Margin = new Padding(3, 0, 3, 1);
            this.label.Size = new Size(100, IconSize + 2);
            this.label.Text = "0";
            this.label.TabIndex = 0;
            this.label.TextAlign = ContentAlignment.MiddleRight;

            Image image;
            var data = Convert.FromBase64String(TwitchIcon.IconBase64);
            using (MemoryStream ms = new MemoryStream(data)) {
                image = Image.FromStream(ms);
            }
            this.twitchIcon.Image = image;
            this.twitchIcon.ErrorImage = null;
            this.twitchIcon.InitialImage = null;
            this.twitchIcon.Location = new Point(4, 6);
            this.twitchIcon.MaximumSize = new Size(32, 32);
            this.twitchIcon.MinimumSize = new Size(32, 32);
            this.twitchIcon.BackgroundImageLayout = ImageLayout.Center;
            this.twitchIcon.Size = new Size(32, 32);
            this.twitchIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            this.twitchIcon.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.twitchIcon.TabIndex = 1;
            this.twitchIcon.TabStop = false;

            this.AutoScaleMode = AutoScaleMode.None;
            this.ControlBox = false;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ClientSize = new System.Drawing.Size(100, 42);
            this.Name = "MainForm";
            this.Text = "MainForm";
            this.Opacity = 0.75;
            this.BackColor = Color.FromArgb(15, 14, 17);

            this.Controls.Add(this.twitchIcon);
            this.Controls.Add(this.label);

            this.MouseWheel += OnMouseWheel;
            this.KeyDown += OnKeyDown;

            ((ISupportInitialize)this.twitchIcon).EndInit();
            this.ResumeLayout(false);
        }

        private Label label;
        private PictureBox twitchIcon;

        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    static class TwitchIcon {
        public static string IconBase64 =
        "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" +
        "AgAAAABJRU5ErkJggg==";

    }
}
