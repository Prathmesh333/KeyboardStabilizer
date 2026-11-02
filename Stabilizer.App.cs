using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace KeyboardStabilizer
{
    internal sealed class StabilizerWindow : Form
    {
        private readonly ComboBox mode = new ComboBox(), target = new ComboBox();
        private readonly NumericUpDown delay = new NumericUpDown(), interval = new NumericUpDown();
        private readonly CheckBox live = new CheckBox();
        private readonly FlowLayoutPanel keyOptions = new FlowLayoutPanel();
        private readonly Button start = new Button(), stop = new Button(), refresh = new Button(), demo = new Button();
        private readonly Label status = new Label(), summary = new Label();
        private readonly ListBox log = new ListBox();
        private readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        private InputRunner runner;
        private bool running;

        internal StabilizerWindow()
        {
            Text = "Keyboard Stabilizer - D key experiment";
            ClientSize = new Size(1030, 790); MinimumSize = new Size(900, 730);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10);
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs args) { if (running) { args.Handled = true; args.SuppressKeyPress = true; } };
            KeyUp += delegate(object sender, KeyEventArgs args) { if (running) { args.Handled = true; args.SuppressKeyPress = true; } };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 10, ColumnCount = 1 };
            int[] heights = { 56, 62, 48, 48, 48, 60, 50, 42, 62 };
            foreach (int height in heights) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Text = "Keep a faulty hold steady", Font = new Font("Segoe UI", 22, FontStyle.Bold), Dock = DockStyle.Fill }, 0, 0);
            layout.Controls.Add(new Label { Text = "Start with D in preview. Smooth hold bridges brief release/re-press gaps; repeated taps tries your macro idea.\r\nNeither mode can recover a press the keyboard never sends. This is an experiment, not a hardware repair.", Dock = DockStyle.Fill }, 0, 1);
            keyOptions.Dock = DockStyle.Fill;
            keyOptions.Controls.Add(new Label { Text = "Keys:", AutoSize = true, Padding = new Padding(0, 6, 8, 0) });
            AddKey("W", Keys.W); AddKey("A", Keys.A); AddKey("S", Keys.S); AddKey("D", Keys.D);
            AddKey("Space", Keys.Space); AddKey("Left Shift", Keys.LShiftKey); AddKey("Right Shift", Keys.RShiftKey);
            layout.Controls.Add(keyOptions, 0, 2);
            var options = new FlowLayoutPanel { Dock = DockStyle.Fill };
            options.Controls.Add(new Label { Text = "Mode", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
            mode.DropDownStyle = ComboBoxStyle.DropDownList; mode.Width = 220;
            mode.Items.AddRange(new object[] { "Smooth hold (start here)", "Repeated taps (experimental)" }); mode.SelectedIndex = 0;
            options.Controls.Add(mode);
            options.Controls.Add(new Label { Text = "Release grace (ms)", AutoSize = true, Padding = new Padding(12, 5, 0, 0) });
            delay.Minimum = 0; delay.Maximum = 50; delay.Value = 12; delay.Width = 65; options.Controls.Add(delay);
            options.Controls.Add(new Label { Text = "Tap interval (ms)", AutoSize = true, Padding = new Padding(12, 5, 0, 0) });
            interval.Minimum = 20; interval.Maximum = 500; interval.Value = 80; interval.Width = 65; interval.Enabled = false;
            options.Controls.Add(interval);
            mode.SelectedIndexChanged += delegate { interval.Enabled = !running && mode.SelectedIndex == 1; };
            layout.Controls.Add(options, 0, 3);
            var targeting = new FlowLayoutPanel { Dock = DockStyle.Fill };
            live.Text = "Enable live output"; live.AutoSize = true; live.Padding = new Padding(0, 4, 10, 0);
            live.CheckedChanged += delegate { target.Enabled = live.Checked && !running; refresh.Enabled = live.Checked && !running; };
            targeting.Controls.Add(live);
            target.DropDownStyle = ComboBoxStyle.DropDownList; target.Width = 520; target.Enabled = false;
            targeting.Controls.Add(target);
            refresh.Text = "Refresh windows"; refresh.AutoSize = true; refresh.Enabled = false;
            refresh.Click += delegate { RefreshTargets(); }; targeting.Controls.Add(refresh);
            layout.Controls.Add(targeting, 0, 4);
            layout.Controls.Add(new Label { Text = "Live output replaces selected keys in ONE selected window with synthesized input. F8 stops.\r\nIt adds release delay, may not work with raw-input games, and is not verified or approved for Valorant. Test outside the game first.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(130, 55, 15) }, 0, 5);
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
            start.Text = "Start preview"; start.AutoSize = true; start.Click += delegate { StartRun(); };
            stop.Text = "Stop / release keys"; stop.AutoSize = true; stop.Enabled = false; stop.Click += delegate { if (runner != null) runner.RequestStop(); };
            demo.Text = "Show glitch example"; demo.AutoSize = true; demo.Click += delegate { ShowExample(); };
            live.CheckedChanged += delegate { start.Text = live.Checked ? "Arm live output" : "Start preview"; };
            actions.Controls.Add(start); actions.Controls.Add(stop); actions.Controls.Add(demo);
            layout.Controls.Add(actions, 0, 6);
            status.Text = "Stopped. D is selected. Preview does not change keyboard input."; status.Dock = DockStyle.Fill;
            layout.Controls.Add(status, 0, 7);
            summary.Text = "Physical: released     Filtered: released"; summary.Dock = DockStyle.Fill; layout.Controls.Add(summary, 0, 8);
            log.Dock = DockStyle.Fill; log.Font = new Font("Consolas", 10); log.HorizontalScrollbar = true;
            layout.Controls.Add(log, 0, 9);
            Shown += delegate { RefreshTargets(); };
            FormClosing += delegate { if (runner != null) runner.WaitForStop(); };
            uiTimer.Interval = 100; uiTimer.Tick += delegate { UpdateRun(); }; uiTimer.Start();
        }
        private void AddKey(string name, Keys key)
        {
            keyOptions.Controls.Add(new CheckBox { Text = name, Tag = (int)key, Checked = key == Keys.D, AutoSize = true, Padding = new Padding(0, 4, 8, 0) });
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        { if (running) return true; return base.ProcessCmdKey(ref message, keyData); }
        protected override bool ProcessDialogKey(Keys keyData)
        { if (running) return true; return base.ProcessDialogKey(keyData); }

        private void RefreshTargets()
        {
            WindowChoice previous = target.SelectedItem as WindowChoice;
            target.Items.Clear();
            Win.EnumWindows(delegate(IntPtr handle, IntPtr parameter)
            {
                if (!Win.IsWindowVisible(handle)) return true;
                uint process; Win.GetWindowThreadProcessId(handle, out process);
                if (process == (uint)Process.GetCurrentProcess().Id) return true;
                var text = new StringBuilder(512); Win.GetWindowText(handle, text, text.Capacity);
                if (text.Length == 0) return true;
                var item = new WindowChoice { Handle = handle, ProcessId = process, Title = text.ToString() };
                target.Items.Add(item);
                if (previous != null && previous.Handle == handle && previous.ProcessId == process) target.SelectedItem = item;
                return true;
            }, IntPtr.Zero);
            // Deliberately leave the target unselected until the user chooses a window.
        }
        private void StartRun()
        {
            var keys = new List<int>();
            foreach (Control control in keyOptions.Controls)
            {
                var check = control as CheckBox;
                if (check != null && check.Checked) keys.Add((int)check.Tag);
            }
            if (keys.Count == 0) { status.Text = "Select at least one key."; return; }
            WindowChoice choice = live.Checked ? target.SelectedItem as WindowChoice : new WindowChoice { Handle = Handle, ProcessId = (uint)Process.GetCurrentProcess().Id, Title = Text };
            if (choice == null || !Win.IsWindow(choice.Handle)) { status.Text = "Choose a target window for live output."; return; }
            log.Items.Clear();
            runner = new InputRunner();
            var config = new RunConfig { Keys = keys.ToArray(), Mode = mode.SelectedIndex == 0 ? OutputMode.SmoothHold : OutputMode.RepeatedTaps,
                Delay = (int)delay.Value, Interval = (int)interval.Value, Live = live.Checked, Target = choice };
            SetRunning(true); status.Text = "Starting..."; runner.Start(config);
        }
        private void SetRunning(bool value)
        {
            running = value;
            start.Enabled = !value; demo.Enabled = !value; stop.Enabled = value;
            mode.Enabled = !value; delay.Enabled = !value; interval.Enabled = !value && mode.SelectedIndex == 1;
            live.Enabled = !value; target.Enabled = !value && live.Checked; refresh.Enabled = !value && live.Checked;
            keyOptions.Enabled = !value;
        }
        private void UpdateRun()
        {
            if (runner == null) return;
            string message;
            while (runner.Events.TryDequeue(out message)) AddLog(message);
            status.Text = runner.Status; summary.Text = runner.Summary;
            if (runner.Finished) SetRunning(false);
        }
        private void AddLog(string message)
        {
            log.Items.Add(message);
            while (log.Items.Count > 120) log.Items.RemoveAt(0);
            log.TopIndex = Math.Max(0, log.Items.Count - 1);
        }
        private void ShowExample()
        {
            log.Items.Clear(); double now = 0;
            var engine = new StabilizerEngine(new int[] { (int)Keys.D }, mode.SelectedIndex == 0 ? OutputMode.SmoothHold : OutputMode.RepeatedTaps,
                (int)delay.Value, (int)interval.Value, 10, delegate(int key, bool down) { AddLog(now.ToString("F0").PadLeft(4) + " ms  OUTPUT D " + (down ? "DOWN" : "UP")); });
            AddLog("SIMULATED EXAMPLE ONLY - no system input sent.");
            AddLog("0 ms: press D. 100 ms: brief false release. 104 ms: D returns.");
            AddLog("500 ms: deliberately release D. Output release waits for the grace period.");
            for (int t = 0; t <= 550; t++)
            {
                now = t;
                if (t == 0 || t == 104) engine.Input((int)Keys.D, true, now);
                else if (t == 100 || t == 500) engine.Input((int)Keys.D, false, now);
                else engine.Advance(now);
            }
            summary.Text = "Example: physical down/up " + engine.PhysicalPresses + "/" + engine.PhysicalReleases
                + "; output down/up " + engine.OutputPresses + "/" + engine.OutputReleases + "; gaps bridged " + engine.BridgedGaps + ".";
            status.Text = "Example finished. Change mode to compare smoothing with repeated taps.";
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { if (runner != null) runner.WaitForStop(); uiTimer.Dispose(); }
            base.Dispose(disposing);
        }
    }
    internal static class StabilizerProgram
    {
        [STAThread] private static void Main()
        {
            bool first;
            using (var mutex = new System.Threading.Mutex(true, "Local\\KeyboardStabilizer.SingleInstance", out first))
            {
                if (!first) { MessageBox.Show("Keyboard Stabilizer is already open. Use that window; F8 stops its active test."); return; }
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new StabilizerWindow());
            }
        }
    }
}
