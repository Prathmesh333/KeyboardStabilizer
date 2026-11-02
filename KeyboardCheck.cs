using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace KeyboardCheck
{
    internal sealed class KeyStats
    {
        public string Name;
        public bool Held;
        public int Presses, Releases, Repeats, RapidRepresses;
        public double? LastUp;
    }

    internal sealed class Tracker
    {
        public readonly Dictionary<string, KeyStats> Keys = new Dictionary<string, KeyStats>();
        public int MaxHeld;
        // A short release-to-press interval is evidence to inspect, not a fault diagnosis.
        public string Record(string id, string name, bool up, double milliseconds)
        {
            KeyStats key;
            if (!Keys.TryGetValue(id, out key))
            {
                if (up) return null; // Started listening after the press.
                key = new KeyStats { Name = name };
                Keys.Add(id, key);
            }
            if (up)
            {
                if (!key.Held) return null;
                key.Held = false;
                key.Releases++;
                key.LastUp = milliseconds;
                return "UP";
            }
            if (key.Held) { key.Repeats++; return "REPEAT"; }
            bool rapid = key.LastUp.HasValue && milliseconds - key.LastUp.Value >= 0 && milliseconds - key.LastUp.Value < 35;
            key.Held = true;
            key.Presses++;
            if (rapid) key.RapidRepresses++;
            int held = 0;
            foreach (KeyStats item in Keys.Values) if (item.Held) held++;
            MaxHeld = Math.Max(MaxHeld, held);
            return rapid ? "DOWN (rapid re-press)" : "DOWN";
        }
        public void ClearHeld()
        {
            foreach (KeyStats key in Keys.Values) { key.Held = false; key.LastUp = null; }
        }
    }

    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] internal struct Device
        { public ushort Page, Usage; public uint Flags; public IntPtr Target; }
        [StructLayout(LayoutKind.Sequential)] internal struct Header
        { public uint Type, Size; public IntPtr Device, WParam; }
        [StructLayout(LayoutKind.Sequential)] internal struct Keyboard
        { public ushort MakeCode, Flags, Reserved, VKey; public uint Message, Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct Filter
        { public uint Size, Flags, Wait, Delay, Repeat, Bounce; }
        [StructLayout(LayoutKind.Sequential)] internal struct Accessibility
        { public uint Size, Flags; }
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterRawInputDevices(Device[] devices, uint count, uint size);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputData(IntPtr handle, uint command, IntPtr data, ref uint size, uint headerSize);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetKeyNameText(int code, StringBuilder text, int size);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        internal static extern bool GetFilter(uint action, uint param, ref Filter value, uint flags);
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        internal static extern bool GetAccessibility(uint action, uint param, ref Accessibility value, uint flags);
    }

    internal sealed class CheckWindow : Form
    {
        private readonly Tracker tracker = new Tracker();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Dictionary<IntPtr, int> devices = new Dictionary<IntPtr, int>();
        private readonly Dictionary<string, ListViewItem> rows = new Dictionary<string, ListViewItem>();
        private readonly Label held = new Label(), status = new Label(), totals = new Label();
        private readonly ListView stats = new ListView(), events = new ListView();
        private readonly TextBox checks = new TextBox();
        private readonly Button capture = new Button();
        private bool listening, ready;

        public CheckWindow()
        {
            Text = "Keyboard Check - input diagnostics";
            ClientSize = new Size(1000, 730);
            MinimumSize = new Size(850, 650);
            Font = new Font("Segoe UI", 10);
            KeyPreview = true;
            // Keep test keys (especially Space/Enter) from activating the focused button.
            KeyDown += delegate(object sender, KeyEventArgs args)
            {
                if (listening) { args.Handled = true; args.SuppressKeyPress = true; }
            };
            KeyUp += delegate(object sender, KeyEventArgs args)
            {
                if (listening) { args.Handled = true; args.SuppressKeyPress = true; }
            };
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 7 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Find what your keyboard actually sends", Font = new Font("Segoe UI", 20, FontStyle.Bold), Dock = DockStyle.Fill, AutoSize = false }, 0, 0);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
            capture.Text = "Start test"; capture.AutoSize = true;
            capture.Click += delegate { listening = !listening; tracker.ClearHeld(); UpdateState(); };
            actions.Controls.Add(capture);
            var reset = new Button { Text = "Reset counts", AutoSize = true };
            reset.Click += delegate { tracker.Keys.Clear(); tracker.MaxHeld = 0; rows.Clear(); stats.Items.Clear(); events.Items.Clear(); UpdateState(); };
            actions.Controls.Add(reset);
            var refresh = new Button { Text = "Refresh settings", AutoSize = true };
            refresh.Click += delegate { ReadSettings(); };
            actions.Controls.Add(refresh);
            var settings = new Button { Text = "Windows keyboard settings", AutoSize = true };
            settings.Click += delegate
            {
                try { Process.Start(new ProcessStartInfo("ms-settings:easeofaccess-keyboard") { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not open Settings"); }
            };
            actions.Controls.Add(settings);
            layout.Controls.Add(actions, 0, 1);
            status.Dock = DockStyle.Fill; layout.Controls.Add(status, 0, 2);
            held.Dock = DockStyle.Fill; held.Font = new Font("Segoe UI", 12, FontStyle.Bold); layout.Controls.Add(held, 0, 3);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage test = new TabPage("Press counts"), log = new TabPage("Recent events"), system = new TabPage("Windows checks"), guide = new TabPage("How to test");
            tabs.TabPages.AddRange(new TabPage[] { test, log, system, guide });
            SetupList(stats, new string[] { "Device / key", "Down", "Up", "Repeats", "Rapid re-press", "State" }, new int[] { 280, 75, 75, 90, 125, 100 });
            SetupList(events, new string[] { "Time (s)", "Device / key", "Event" }, new int[] { 110, 330, 290 });
            test.Controls.Add(stats); log.Controls.Add(events);
            checks.Multiline = true; checks.ReadOnly = true; checks.ScrollBars = ScrollBars.Vertical; checks.Dock = DockStyle.Fill;
            system.Controls.Add(checks);
            guide.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical, Text = Instructions });
            layout.Controls.Add(tabs, 0, 4);
            totals.Dock = DockStyle.Fill; layout.Controls.Add(totals, 0, 5);
            layout.Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Captures only while this window is active and the test is started. No files or keystrokes are saved.\r\nTiming is app receipt time, not keyboard latency. This tool does not repair switches or change settings." }, 0, 6);
            Deactivate += delegate { tracker.ClearHeld(); UpdateState(); };
            Activated += delegate { UpdateState(); };
            ReadSettings();
            UpdateState();
        }

        private static void SetupList(ListView view, string[] names, int[] widths)
        {
            view.Dock = DockStyle.Fill; view.View = View.Details; view.FullRowSelect = true; view.GridLines = true;
            for (int i = 0; i < names.Length; i++) view.Columns.Add(names[i], widths[i]);
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (listening) return true;
            return base.ProcessCmdKey(ref message, keyData);
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (listening) return true;
            return base.ProcessDialogKey(keyData);
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // No INPUTSINK, hooks, input suppression, or synthesized key events.
            ready = Native.RegisterRawInputDevices(new Native.Device[] { new Native.Device { Page = 1, Usage = 6, Flags = 0, Target = Handle } }, 1, (uint)Marshal.SizeOf(typeof(Native.Device)));
            UpdateState();
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x00FF && listening && ready && Form.ActiveForm == this)
                ReadInput(message.LParam);
            base.WndProc(ref message); // Required foreground WM_INPUT cleanup.
        }
        private void ReadInput(IntPtr handle)
        {
            uint size = 0, headerSize = (uint)Marshal.SizeOf(typeof(Native.Header));
            if (Native.GetRawInputData(handle, 0x10000003, IntPtr.Zero, ref size, headerSize) == uint.MaxValue || size < headerSize || size > 65536) return;
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint read = Native.GetRawInputData(handle, 0x10000003, buffer, ref size, headerSize);
                if (read == uint.MaxValue || read < headerSize + Marshal.SizeOf(typeof(Native.Keyboard))) return;
                var header = (Native.Header)Marshal.PtrToStructure(buffer, typeof(Native.Header));
                if (header.Type != 1) return;
                var key = (Native.Keyboard)Marshal.PtrToStructure(IntPtr.Add(buffer, (int)headerSize), typeof(Native.Keyboard));
                if (key.MakeCode == 0xFF) { AddEvent("Keyboard", "ROLLOVER OVERRUN reported"); return; }
                if (key.VKey == 0 || key.VKey >= 255) return;
                int device;
                if (!devices.TryGetValue(header.Device, out device)) { device = devices.Count + 1; devices.Add(header.Device, device); }
                var name = new StringBuilder(128);
                int code = (key.MakeCode << 16) | (((key.Flags & 2) != 0) ? (1 << 24) : 0);
                Native.GetKeyNameText(code, name, name.Capacity);
                string keyName = name.Length > 0 ? name.ToString() : ((System.Windows.Forms.Keys)key.VKey).ToString();
                string label = "Keyboard " + device + " / " + keyName;
                string id = header.Device + ":" + key.MakeCode + ":" + (key.Flags & 6) + ":" + key.VKey;
                string result = tracker.Record(id, label, (key.Flags & 1) != 0, clock.Elapsed.TotalMilliseconds);
                if (result == null) return;
                if (!rows.ContainsKey(id)) { var row = new ListViewItem(new string[] { label, "0", "0", "0", "0", "" }); rows.Add(id, row); stats.Items.Add(row); }
                if (result != "REPEAT") AddEvent(label, result);
                UpdateState();
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        private void AddEvent(string name, string action)
        {
            events.Items.Insert(0, new ListViewItem(new string[] { clock.Elapsed.TotalSeconds.ToString("F3"), name, action }));
            while (events.Items.Count > 150) events.Items.RemoveAt(events.Items.Count - 1);
        }
        private void UpdateState()
        {
            if (capture == null) return;
            capture.Text = listening ? "Stop test" : "Start test";
            status.Text = !ready ? "Input capture unavailable (or still initializing)." : !listening ? "Stopped. Click Start test, then press the keys you want to check." : Form.ActiveForm == this ? "Listening - use only the keyboard being tested. Release all keys before switching windows." : "Paused while another window is active. Held-key state was cleared.";
            var active = new List<string>();
            foreach (var pair in tracker.Keys)
            {
                KeyStats key = pair.Value;
                if (key.Held) active.Add(key.Name);
                ListViewItem row;
                if (!rows.TryGetValue(pair.Key, out row)) continue;
                row.SubItems[1].Text = key.Presses.ToString(); row.SubItems[2].Text = key.Releases.ToString();
                row.SubItems[3].Text = key.Repeats.ToString(); row.SubItems[4].Text = key.RapidRepresses.ToString();
                row.SubItems[5].Text = key.Held ? "HELD" : "Released";
                row.BackColor = key.Held ? Color.FromArgb(215, 241, 221) : SystemColors.Window;
            }
            held.Text = active.Count == 0 ? "Held keys: none" : "Held: " + string.Join("  +  ", active.ToArray());
            totals.Text = "Currently held: " + active.Count + "     Most held together this test: " + tracker.MaxHeld + " (across all devices used)";
        }
        private static string AccessibilityState(uint action)
        {
            var value = new Native.Accessibility { Size = (uint)Marshal.SizeOf(typeof(Native.Accessibility)) };
            return Native.GetAccessibility(action, value.Size, ref value, 0) ? ((value.Flags & 1) != 0 ? "ON" : "OFF") + " (activation shortcut " + ((value.Flags & 4) != 0 ? "enabled" : "disabled") + ")" : "Could not read";
        }
        private void ReadSettings()
        {
            var value = new Native.Filter { Size = (uint)Marshal.SizeOf(typeof(Native.Filter)) };
            string filter = Native.GetFilter(0x32, value.Size, ref value, 0) ? ((value.Flags & 1) != 0 ? "ON" : "OFF") + "\r\n  Acceptance delay: " + value.Wait + " ms; bounce interval: " + value.Bounce + " ms\r\n  Activation shortcut: " + ((value.Flags & 4) != 0 ? "enabled" : "disabled") : "Could not read";
            checks.Text = "Read-only snapshot - click Refresh settings after changing anything.\r\n\r\nFilter Keys: " + filter + "\r\nSticky Keys: " + AccessibilityState(0x3A) + "\r\nToggle Keys: " + AccessibilityState(0x34) + "\r\n\r\nFilter Keys can ignore quick/repeated presses. Sticky Keys changes modifier behavior.\r\nToggle Keys plays lock-key sounds. Enabled shortcuts do not mean a feature is ON.\r\n\r\nRaw input and normal application input follow different paths. OFF here does not prove the keyboard is healthy.\r\n\r\nThis program does not remove apps, modify drivers, change USB settings, or adjust game configuration.";
        }
        private const string Instructions = "1. Single-key test\r\nClick Reset counts and Start test. Slowly tap one problem key exactly 20 times, fully releasing it each time. Compare Down and Up with 20. Repeat for a known-good key. A missing physical press cannot be counted automatically: you provide the expected count.\r\n\r\n2. Hold and release\r\nHold W for a few seconds, then release. Repeats while held are normal. HELD should disappear when you release. Repeated DOWN/UP transitions while you keep steady pressure warrant investigation. Do not switch windows during this test: switching clears held state.\r\n\r\n3. Combination test\r\nHold W, then Shift, then Space. Check that each appears in Held. Release all and try W + A + Shift + Space, then your actual game combinations. Test keys individually too. Missing only in particular combinations suggests a rollover/matrix limitation; this app cannot establish the keyboard's advertised rollover rating. Avoid Windows/system shortcuts.\r\n\r\n4. Compare connections\r\nRepeat using a direct USB port (without a hub). If wireless, compare wired mode if supported. Try the same keyboard on another PC and another keyboard on this PC. Keyboard numbers are assigned when each device first sends input; they are not model names.\r\n\r\n5. Read results carefully\r\nRapid re-press counts release-to-press intervals under 35 ms. It is only a clue: fast intentional taps or event-delivery delays can trigger it too. No physical actuation or end-to-end latency is measured. Raw events cannot locate a fault within the switch, firmware, connection, or driver.\r\n\r\nIf the same individual key misses presses on another PC, inspect the hardware/model-specific repair options. If all tests work here but fail only in Valorant, investigate game bindings and background software. Close this tester before playing.\r\n\r\nNo key log is written to disk. Counts and the last 150 transitions stay in memory until Reset or close. Collection stops when this window loses focus. This is a diagnostic tool, not an input optimizer.";
    }

    internal static class Program
    {
        [STAThread] private static void Main()
        { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new CheckWindow()); }
    }

    internal static class Tests
    {
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        public static int Main()
        {
            try
            {
                var t = new Tracker();
                Require(t.Record("1:W", "W", true, 0) == null, "Orphan release must not create a press");
                t.Record("1:W", "W", false, 100); t.Record("1:W", "W", false, 600);
                Require(t.Keys["1:W"].Presses == 1 && t.Keys["1:W"].Repeats == 1 && t.Keys["1:W"].RapidRepresses == 0, "Hold repeat must not be chatter");
                t.Record("1:W", "W", true, 1000); t.Record("1:W", "W", false, 1010);
                Require(t.Keys["1:W"].RapidRepresses == 1, "Short release-to-press must be flagged");
                t.Record("2:W", "W", false, 1011);
                Require(t.Keys["2:W"].Presses == 1 && t.Keys["2:W"].Repeats == 0 && t.MaxHeld == 2, "Devices must have separate state");
                t.ClearHeld(); t.Record("1:W", "W", false, 1012);
                Require(t.Keys["1:W"].Presses == 3 && t.Keys["1:W"].RapidRepresses == 1 && !t.Keys["2:W"].Held, "Focus reset must clear transient state only");
                t.Record("1:W", "W", true, 2000); t.Record("1:W", "W", false, 2035);
                Require(t.Keys["1:W"].RapidRepresses == 1, "35 ms boundary must not flag");
                Console.WriteLine("PASS: orphan release, held repeat, rapid re-press, device isolation, focus reset, threshold boundary.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        }
    }
}
