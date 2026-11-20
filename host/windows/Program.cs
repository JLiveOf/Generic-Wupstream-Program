// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nefarius.ViGEm.Client.Exceptions;

namespace WupPad
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); }
            catch (EntryPointNotFoundException) { SetProcessDPIAware(); }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool first;
            using (var instance = new Mutex(true, @"Local\GenericWupstreamProgram", out first))
            {
                if (first) Application.Run(new MainWindow());
                else
                {
                    Language.English = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "es";
                    MessageBox.Show(Language.Get("Generic Wupstream Program ya está abierto."),
                        "Generic Wupstream Program", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }

    internal sealed class MainWindow : Form
    {
        internal static readonly Color Background = Color.FromArgb(17, 22, 31);
        internal static readonly Color PanelColor = Color.FromArgb(28, 36, 49);
        internal static readonly Color Border = Color.FromArgb(53, 68, 88);
        internal static readonly Color Ink = Color.FromArgb(238, 244, 252);
        internal static readonly Color Muted = Color.FromArgb(163, 181, 204);
        internal static readonly Color Accent = Color.FromArgb(103, 234, 208);

        private readonly TextBox address = new TextBox();
        private readonly Button connect;
        private readonly Label status;
        private readonly Label battery;
        private readonly Label audioStatus;
        private readonly Label telemetry;
        private readonly Label volume;
        private readonly Button record;
        private readonly Button vibrate;
        private readonly ComboBox monitor;
        private readonly ComboBox brightness;
        private readonly ComboBox language;
        private readonly CheckBox video;
        private readonly CheckBox audio;
        private readonly CheckBox touch;
        private readonly CheckBox microphone;
        private readonly CheckBox cable;
        private readonly CheckBox xboxLayout;
        private readonly ToolTip tips = new ToolTip();
        private readonly Screen[] screens;
        private readonly ProgressBar micMeter = new ProgressBar();
        private readonly PadView preview = new PadView();
        private readonly System.Windows.Forms.Timer refresh = new System.Windows.Forms.Timer();
        private CancellationTokenSource stop;
        private Task session;
        private bool closing;
        private string recording;
        private StreamOptions activeOptions;
        private volatile Display display;
        private Display shown;

        private sealed class Display
        {
            internal readonly string Text;
            internal readonly PadState State;
            internal readonly bool Error;

            internal Display(string text, PadState state, bool error)
            {
                Text = text;
                State = state;
                Error = error;
            }
        }

        internal MainWindow()
        {
            SuspendLayout();
            Language.English = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "es";
            Text = "Generic Wupstream Program";
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(704, 718);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Background;
            ForeColor = Ink;

            Label title = AddLabel("Wii U GamePad", 24, 15, 474, 44, Ink);
            title.Font = new Font("Segoe UI Semibold", 25);
            language = AddCombo("Idioma / Language", 523, 28, 155);
            language.Items.AddRange(new object[] { "Español", "English" });
            language.SelectedIndex = Language.English ? 1 : 0;
            AddLabel("Abre WUP Stream en el GamePad y conéctalo a la misma red que el PC.",
                26, 68, 650, 26, Muted);

            AddLabel("IP del GamePad  (opcional)", 26, 110, 635, 29, Ink);
            address.Bounds = new Rectangle(26, 144, 436, 31);
            address.Font = new Font("Segoe UI", 13);
            address.BackColor = PanelColor;
            address.ForeColor = Ink;
            address.BorderStyle = BorderStyle.FixedSingle;
            address.MaxLength = 15;
            Controls.Add(address);

            connect = AddButton("Buscar GamePad", 476, 140, 202, 38, true);
            connect.Click += ToggleConnection;
            AcceptButton = connect;
            address.TextChanged += delegate
            {
                if (session == null) connect.Text = ConnectText();
            };
            status = AddLabel("", 26, 185, 652, 31, Muted);

            AddLabel("Monitor", 26, 222, 71, 27, Ink);
            monitor = AddCombo("Monitor", 101, 221, 354);
            screens = Screen.AllScreens;
            video = AddCheck("Enviar pantalla", 476, 220, 202, true);
            audio = AddCheck("Enviar audio del PC", 26, 257, 202, true);
            touch = AddCheck("Táctil como ratón", 245, 257, 212, false);
            microphone = AddCheck("Usar micrófono", 476, 257, 202, false);
            cable = AddCheck("Enviar a VB-CABLE", 26, 292, 202, false);
            record = AddButton("Grabar WAV…", 245, 289, 212, 33, false);
            record.Click += ChooseRecording;
            vibrate = AddButton("Probar vibración", 476, 289, 202, 33, false);
            vibrate.Enabled = false;
            vibrate.Click += delegate
            {
                if (activeOptions != null)
                    Interlocked.Exchange(ref activeOptions.VibrationUntil, Stopwatch.GetTimestamp() + Stopwatch.Frequency / 3);
            };

            AddLabel("Brillo", 26, 327, 56, 27, Ink);
            brightness = AddCombo("Brillo del GamePad", 82, 327, 130);
            brightness.Items.AddRange(new object[] { "Del GamePad", "1", "2", "3", "4", "5", "6" });
            brightness.SelectedIndex = 0;
            volume = AddLabel("Volumen del GamePad · —", 245, 327, 212, 27, Muted);
            micMeter.Bounds = new Rectangle(476, 335, 202, 13);
            Controls.Add(micMeter);
            audioStatus = AddLabel("", 26, 358, 652, 42, Muted);
            telemetry = AddLabel("Acelerómetro · —     Giroscopio · —", 26, 400, 652, 24, Muted);
            video.CheckedChanged += delegate { UpdateOptions(); };
            microphone.CheckedChanged += delegate { UpdateOptions(); };
            touch.CheckedChanged += delegate
            {
                if (activeOptions != null) activeOptions.Touch = touch.Checked;
            };
            brightness.SelectedIndexChanged += delegate
            {
                if (activeOptions != null) activeOptions.Brightness = brightness.SelectedIndex == 0 ? -1 : brightness.SelectedIndex;
            };
            UpdateOptions();

            Label heading = AddLabel("Controles en directo", 26, 430, 360, 29, Ink);
            heading.Font = new Font("Segoe UI Semibold", 13);
            battery = AddLabel("Batería · —", 400, 430, 278, 29, Muted);
            battery.TextAlign = ContentAlignment.MiddleRight;
            preview.Bounds = new Rectangle(26, 466, 652, 196);
            Controls.Add(preview);
            AddButton("Probar en Windows", 26, 675, 184, 34, false).Click += delegate
            {
                Open("joy.cpl");
            };
            xboxLayout = AddCheck("ABXY por posición Xbox", 229, 678, 225, false);
            xboxLayout.CheckedChanged += delegate
            {
                if (activeOptions != null) activeOptions.XboxLayout = xboxLayout.Checked;
            };
            AddButton("Instalar Homebrew Menu", 460, 675, 218, 34, false).Click += delegate
            {
                Open("https://github.com/ravensclawx/wup-homebrew#get-it");
            };

            language.SelectedIndexChanged += delegate { ApplyLanguage(); };
            ApplyLanguage();
            refresh.Interval = 45;
            refresh.Tick += UpdateDisplay;
            refresh.Start();
            ResumeLayout(false);
        }

        private Label AddLabel(string text, int x, int y, int width, int height, Color color)
        {
            var label = new Label
            {
                Text = Language.Get(text), Tag = text, Bounds = new Rectangle(x, y, width, height),
                ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(label);
            return label;
        }

        private Button AddButton(string text, int x, int y, int width, int height, bool primary)
        {
            var button = new DarkButton
            {
                Text = Language.Get(text), Tag = text, Bounds = new Rectangle(x, y, width, height),
                BackColor = primary ? Accent : PanelColor, ForeColor = primary ? Background : Ink,
                FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = primary ? Accent : Border;
            Controls.Add(button);
            return button;
        }

        private ComboBox AddCombo(string name, int x, int y, int width)
        {
            var combo = new ComboBox
            {
                AccessibleName = Language.Get(name), Bounds = new Rectangle(x, y, width, 27), DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = PanelColor, ForeColor = Ink, FlatStyle = FlatStyle.Flat
            };
            Controls.Add(combo);
            return combo;
        }

        private CheckBox AddCheck(string text, int x, int y, int width, bool value)
        {
            var checkbox = new DarkCheckBox
            {
                Text = Language.Get(text), Tag = text, Bounds = new Rectangle(x, y, width, 29), Checked = value,
                ForeColor = Ink, UseVisualStyleBackColor = false
            };
            Controls.Add(checkbox);
            return checkbox;
        }

        private void ChooseRecording(object sender, EventArgs e)
        {
            if (recording != null)
            {
                recording = null;
                record.Text = Language.Get("Grabar WAV…");
                return;
            }
            using (var dialog = new SaveFileDialog
            {
                Title = Language.Get("Guardar el micrófono del GamePad"), Filter = "WAV (*.wav)|*.wav",
                FileName = "GamePad.wav", OverwritePrompt = true
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                recording = dialog.FileName;
                record.Text = Language.Get("Quitar grabación WAV");
            }
        }

        private void UpdateOptions()
        {
            bool idle = activeOptions == null;
            language.Enabled = idle;
            monitor.Enabled = idle && video.Checked;
            video.Enabled = audio.Enabled = microphone.Enabled = idle;
            cable.Enabled = record.Enabled = idle && microphone.Checked;
            touch.Enabled = video.Checked;
        }

        private void ApplyLanguage()
        {
            Language.English = language.SelectedIndex == 1;
            foreach (Control control in Controls)
            {
                var text = control.Tag as string;
                if (text != null) control.Text = Language.Get(text);
            }
            address.AccessibleName = Language.Get("IP del GamePad, opcional");
            status.AccessibleName = Language.Get("Estado de la conexión");
            monitor.AccessibleName = Language.Get("Monitor");
            brightness.AccessibleName = Language.Get("Brillo del GamePad");
            micMeter.AccessibleName = Language.Get("Nivel del micrófono");
            preview.AccessibleName = Language.Get("Estado de botones y sticks");
            tips.SetToolTip(xboxLayout, Language.Get("Usa las posiciones de Xbox: B→A, A→B, Y→X, X→Y."));
            brightness.Items[0] = Language.Get("Del GamePad");
            int selected = monitor.SelectedIndex;
            monitor.Items.Clear();
            int primary = 0;
            for (int i = 0; i < screens.Length; i++)
            {
                Screen s = screens[i];
                if (s.Primary) primary = i;
                monitor.Items.Add((s.Primary ? Language.Get("Principal") : Language.Get("Monitor") + " " + (i + 1))
                    + "   " + s.Bounds.Width + " × " + s.Bounds.Height);
            }
            monitor.SelectedIndex = selected >= 0 ? selected : primary;
            connect.Text = ConnectText();
            record.Text = Language.Get(recording == null ? "Grabar WAV…" : "Quitar grabación WAV");
            Publish(Language.Get("Introduce la IP de WUP Stream o deja el campo vacío para buscarlo."), null, false);
            preview.Invalidate();
        }

        private void Open(string target)
        {
            try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(this, ex.Message, Language.Get("No se pudo abrir"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string ConnectText()
        {
            return Language.Get(address.Text.Trim().Length == 0 ? "Buscar GamePad" : "Conectar por IP");
        }

        private void ToggleConnection(object sender, EventArgs e)
        {
            if (session != null)
            {
                stop.Cancel();
                connect.Enabled = false;
                Publish(Language.Get("Desconectando…"), null, false);
                return;
            }

            string text = address.Text.Trim();
            IPAddress ip = null;
            if (text.Length != 0 && (!IPAddress.TryParse(text, out ip)
                || ip.AddressFamily != AddressFamily.InterNetwork || ip.ToString() != text
                || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Broadcast)
                || ip.GetAddressBytes()[0] >= 224))
            {
                Publish(Language.Get("Escribe la dirección IPv4 que aparece en WUP Stream."), null, true);
                address.Focus();
                return;
            }

            address.Enabled = false;
            connect.Text = Language.Get("Cancelar");
            Publish(Language.Get("Iniciando conexión…"), null, false);
            var launch = new StreamOptions
            {
                Monitor = screens[monitor.SelectedIndex].Bounds,
                Video = video.Checked, Audio = audio.Checked, Touch = touch.Checked, XboxLayout = xboxLayout.Checked,
                Microphone = microphone.Checked, Cable = microphone.Checked && cable.Checked,
                Recording = microphone.Checked ? recording : null,
                Brightness = brightness.SelectedIndex == 0 ? -1 : brightness.SelectedIndex
            };
            activeOptions = launch;
            UpdateOptions();
            stop = new CancellationTokenSource();
            CancellationToken token = stop.Token;
            session = Task.Factory.StartNew(delegate
            {
                try
                {
                    new PadSession(token, (message, state) => Publish(message, state, false), launch).Run(ip);
                    Publish(Language.Get("GamePad desconectado."), null, false);
                }
                catch (OperationCanceledException)
                {
                    Publish(Language.Get("GamePad desconectado."), null, false);
                }
                catch (VigemBusNotFoundException)
                {
                    Publish(Language.Get("Windows no encuentra ViGEmBus. Consulta la guía incluida con la app."), null, true);
                }
                catch (SocketException ex)
                {
                    Publish(string.Format(Language.Get("Error de red ({0}). Comprueba la conexión y la IP del GamePad."), ex.SocketErrorCode), null, true);
                }
                catch (InvalidOperationException ex)
                {
                    Publish(ex.Message, null, true);
                }
                catch (DllNotFoundException)
                {
                    Publish(Language.Get("Falta WupStreamNative.dll. Extrae todos los archivos del ZIP juntos."), null, true);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Publish(string message, PadState state, bool error)
        {
            display = new Display(message, state, error);
        }

        private void UpdateDisplay(object sender, EventArgs e)
        {
            if (session != null && session.IsCompleted)
            {
                if (session.IsFaulted)
                    Publish(session.Exception.GetBaseException().Message, null, true);
                session = null;
                activeOptions = null;
                stop.Dispose();
                stop = null;
                if (closing) { Close(); return; }
                address.Enabled = true;
                connect.Enabled = true;
                connect.Text = ConnectText();
                recording = null;
                record.Text = Language.Get("Grabar WAV…");
                UpdateOptions();
            }

            Display current = display;
            if (ReferenceEquals(shown, current)) return;
            shown = current;
            status.Text = current.Text;
            status.ForeColor = current.Error ? Color.FromArgb(255, 171, 165) : current.State != null ? Accent : Muted;
            preview.State = current.State;
            vibrate.Enabled = current.State != null && !closing && !stop.IsCancellationRequested;
            connect.Text = session != null ? Language.Get(current.State != null ? "Desconectar" : "Cancelar") : ConnectText();
            battery.Text = current.State == null || current.State.Battery < 0 ? Language.Get("Batería · —")
                : Language.Get("Batería · ") + current.State.Battery + " %" + (current.State.Charging ? Language.Get("  ·  Cargando") : "");
            micMeter.Value = current.State == null ? 0 : current.State.MicLevel;
            audioStatus.Text = current.State == null ? "" : current.State.AudioStatus;
            volume.Text = current.State == null ? Language.Get("Volumen del GamePad · —")
                : Language.Get("Volumen · ") + (current.State.Volume * 100 / 255) + " %";
            telemetry.Text = current.State == null ? Language.Get("Acelerómetro · —     Giroscopio · —")
                : Language.Get("Acelerómetro · ") + string.Join(" / ", current.State.Accel)
                    + Language.Get("     Giroscopio · ") + string.Join(" / ", current.State.Gyro);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (session != null && e.CloseReason == CloseReason.UserClosing)
            {
                closing = true;
                ToggleConnection(this, EventArgs.Empty);
                e.Cancel = true;
            }
            else if (stop != null) stop.Cancel();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { refresh.Dispose(); tips.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal sealed class DarkButton : Button
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled) { base.OnPaint(e); return; }
            e.Graphics.Clear(MainWindow.PanelColor);
            ControlPaint.DrawBorder(e.Graphics, ClientRectangle, MainWindow.Border, ButtonBorderStyle.Solid);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, MainWindow.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class DarkCheckBox : CheckBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(MainWindow.Background);
            int side = Math.Max(12, Font.Height - 4);
            var box = new Rectangle(2, (Height - side) / 2, side, side);
            if (Checked)
            {
                using (var fill = new SolidBrush(Enabled ? MainWindow.Accent : MainWindow.Muted))
                    e.Graphics.FillRectangle(fill, box);
                using (var pen = new Pen(MainWindow.Background, 1.5f))
                    e.Graphics.DrawLines(pen, new[] { new Point(box.X + side / 5, box.Y + side / 2),
                        new Point(box.X + side * 2 / 5, box.Y + side * 3 / 4),
                        new Point(box.X + side * 4 / 5, box.Y + side / 4) });
            }
            else ControlPaint.DrawBorder(e.Graphics, box, MainWindow.Muted, ButtonBorderStyle.Solid);
            var caption = new Rectangle(box.Right + 6, 0, Width - box.Right - 6, Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, caption, Enabled ? MainWindow.Ink : MainWindow.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class PadView : Control
    {
        private PadState state;

        internal PadView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AccessibleName = "Estado de botones y sticks";
        }

        internal PadState State
        {
            set { state = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.ScaleTransform(Width / 652f, Height / 196f);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(MainWindow.PanelColor);
            using (var border = new Pen(MainWindow.Border))
            using (var accent = new SolidBrush(MainWindow.Accent))
            using (var dim = new SolidBrush(MainWindow.Muted))
            using (var dark = new SolidBrush(MainWindow.Background))
            using (var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var font = new Font("Segoe UI", 13, GraphicsUnit.Pixel))
            {
                g.DrawRectangle(border, 0, 0, 651, 195);
                g.DrawLine(border, 252, 18, 252, 178);
                DrawStick(g, border, dim, accent, center, font, 70, 0, Language.Get("Izquierdo"));
                DrawStick(g, border, dim, accent, center, font, 181, 2, Language.Get("Derecho"));
                for (int i = 0; i < PadState.Names.Length; i++)
                {
                    var box = new Rectangle(271 + (i % 6) * 61, 21 + (i / 6) * 54, 55, 45);
                    bool held = state != null && state.Held[i];
                    g.FillRectangle(held ? accent : dark, box);
                    g.DrawRectangle(border, box);
                    g.DrawString(PadState.Names[i], font, held ? dark : dim, box, center);
                }
            }
        }

        private void DrawStick(Graphics g, Pen border, Brush dim, Brush accent, StringFormat center,
            Font font, int x, int axis, string label)
        {
            g.DrawEllipse(border, x - 41, 39, 82, 82);
            g.DrawLine(border, x - 40, 80, x + 40, 80);
            g.DrawLine(border, x, 40, x, 120);
            float dx = state == null ? 0 : state.Axes[axis] / 32768f;
            float dy = state == null ? 0 : state.Axes[axis + 1] / 32768f;
            float distance = Math.Max(1, (float)Math.Sqrt(dx * dx + dy * dy));
            g.FillEllipse(state == null ? dim : accent, x - 6 + 37 * dx / distance,
                74 - 37 * dy / distance, 12, 12);
            g.DrawString(label, font, dim, new Rectangle(x - 50, 135, 100, 35), center);
        }
    }
}
