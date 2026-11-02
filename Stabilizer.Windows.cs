using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KeyboardStabilizer
{
    internal static class Win
    {
        internal delegate IntPtr HookCallback(int code, IntPtr message, IntPtr data);
        internal delegate bool WindowCallback(IntPtr handle, IntPtr parameter);
        [StructLayout(LayoutKind.Sequential)] internal struct HookKey
        { internal uint VirtualKey, Scan, Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct KeyboardInput
        { internal ushort VirtualKey, Scan; internal uint Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseInput
        { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr Extra; }
        [StructLayout(LayoutKind.Explicit)] internal struct InputUnion
        { [FieldOffset(0)] internal MouseInput Mouse; [FieldOffset(0)] internal KeyboardInput Keyboard; }
        [StructLayout(LayoutKind.Sequential)] internal struct Input
        { internal uint Type; internal InputUnion Data; }
        [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, HookCallback callback, IntPtr module, uint thread);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string module);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] internal static extern uint MapVirtualKey(uint code, uint mode);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
    }

    internal sealed class WindowChoice
    {
        internal IntPtr Handle;
        internal uint ProcessId;
        internal string Title;
        public override string ToString() { return Title; }
    }

    internal sealed class RunConfig
    {
        internal int[] Keys;
        internal OutputMode Mode;
        internal int Delay, Interval;
        internal bool Live;
        internal WindowChoice Target;
    }

    internal sealed class InputRunner
    {
        internal readonly ConcurrentQueue<string> Events = new ConcurrentQueue<string>();
        internal volatile bool Running, Finished, StopRequested;
        internal volatile string Status = "Starting...", Summary = "";
        private Thread thread;
        internal void Log(string message)
        {
            Events.Enqueue(message);
            string removed;
            while (Events.Count > 120) Events.TryDequeue(out removed);
        }
        internal void Start(RunConfig config)
        {
            thread = new Thread(delegate()
            {
                try
                {
                    using (var context = new HookContext(this, config)) Application.Run(context);
                }
                catch (Exception ex) { Status = "Stopped: " + ex.Message; Log(Status); }
                finally { Running = false; Finished = true; }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        internal void RequestStop() { StopRequested = true; }
        internal void WaitForStop() { RequestStop(); if (thread != null) thread.Join(1000); }
    }

    internal sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        internal Action Emergency;
        internal HotkeyWindow() { CreateHandle(new CreateParams { Caption = "Keyboard Stabilizer worker", Parent = new IntPtr(-3) }); }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0312 && message.WParam.ToInt32() == 1 && Emergency != null) Emergency();
            base.WndProc(ref message);
        }
        public void Dispose() { DestroyHandle(); }
    }

    // A dedicated message-loop thread keeps hooks and timers independent of UI rendering.
    internal sealed class HookContext : ApplicationContext
    {
        private readonly InputRunner owner;
        private readonly RunConfig config;
        private readonly StabilizerEngine engine;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Win.HookCallback callback;
        private readonly Dictionary<int, ushort> scans = new Dictionary<int, ushort>();
        private readonly Dictionary<int, bool> extended = new Dictionary<int, bool>();
        private readonly System.Windows.Forms.Timer timer;
        private readonly HotkeyWindow window;
        private IntPtr hook;
        private bool active, stopped, registered;
        private double armedAt, activatedAt;

        internal HookContext(InputRunner owner, RunConfig config)
        {
            this.owner = owner; this.config = config;
            engine = new StabilizerEngine(config.Keys, config.Mode, config.Delay, config.Interval, 10, Emit);
            window = new HotkeyWindow();
            window.Emergency = delegate { Stop("Stopped with F8."); };
            callback = OnKey;
            timer = new System.Windows.Forms.Timer { Interval = 5 };
            timer.Tick += Tick;
            try
            {
                if (!Win.RegisterHotKey(window.Handle, 1, 0x4000, 0x77)) throw new Win32Exception(Marshal.GetLastWin32Error(), "F8 stop key is unavailable. Close the other stabilizer instance or free F8.");
                registered = true;
                hook = Win.SetWindowsHookEx(13, callback, Win.GetModuleHandle(null), 0);
                if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not observe keyboard input.");
                owner.Running = true;
                owner.Status = "Armed. Focus the selected window and release the selected keys. F8 stops.";
                armedAt = clock.Elapsed.TotalMilliseconds;
                timer.Start();
            }
            catch { Cleanup(); throw; }
        }

        private bool TargetIsForeground()
        {
            uint process;
            return Win.GetForegroundWindow() == config.Target.Handle && Win.IsWindow(config.Target.Handle)
                && Win.GetWindowThreadProcessId(config.Target.Handle, out process) != 0 && process == config.Target.ProcessId;
        }
        private bool SelectedKeysReleased()
        {
            foreach (int key in config.Keys) if ((Win.GetAsyncKeyState(key) & 0x8000) != 0) return false;
            return true;
        }
        private void Tick(object sender, EventArgs args)
        {
            try
            {
                double now = clock.Elapsed.TotalMilliseconds;
                if (owner.StopRequested) { Stop("Stopped."); return; }
                if (!active)
                {
                    if (now - armedAt > 30000) { Stop("Arming timed out. Start again when ready."); return; }
                    if (!TargetIsForeground() || !SelectedKeysReleased()) return;
                    active = true; activatedAt = now;
                    owner.Status = config.Live ? "LIVE output active for the selected window. F8 stops." : "Preview active. No input is changed. F8 stops.";
                    owner.Log(owner.Status);
                }
                if (!TargetIsForeground()) { Stop("Stopped because focus left the selected window."); return; }
                // Bound experimentation; no unattended or indefinite key synthesis.
                if (now - activatedAt >= 120000) { Stop("Two-minute test finished. Start again to continue."); return; }
                engine.Advance(now);
                Publish();
            }
            catch (Exception ex) { Stop("Stopped: " + ex.Message); }
        }
        private IntPtr OnKey(int code, IntPtr message, IntPtr data)
        {
            if (code < 0 || stopped) return Win.CallNextHookEx(hook, code, message, data);
            try
            {
                var key = (Win.HookKey)Marshal.PtrToStructure(data, typeof(Win.HookKey));
                // Never reprocess synthesized events, including output from this program.
                if ((key.Flags & 0x12) != 0) return Win.CallNextHookEx(hook, code, message, data);
                // Also recognize F8 through the hook so it works while Shift or Ctrl is held.
                if (key.VirtualKey == 0x77 && (message.ToInt32() == 0x100 || message.ToInt32() == 0x104))
                { Stop("Stopped with F8."); return new IntPtr(1); }
                if (active && !TargetIsForeground()) { Stop("Stopped because focus changed."); return Win.CallNextHookEx(hook, code, message, data); }
                if (!active || !engine.Keys.ContainsKey((int)key.VirtualKey)) return Win.CallNextHookEx(hook, code, message, data);
                int kind = message.ToInt32();
                if (kind != 0x100 && kind != 0x101 && kind != 0x104 && kind != 0x105) return Win.CallNextHookEx(hook, code, message, data);
                bool down = kind == 0x100 || kind == 0x104;
                scans[(int)key.VirtualKey] = (ushort)key.Scan;
                extended[(int)key.VirtualKey] = (key.Flags & 1) != 0;
                engine.Input((int)key.VirtualKey, down, clock.Elapsed.TotalMilliseconds);
                return config.Live ? new IntPtr(1) : Win.CallNextHookEx(hook, code, message, data);
            }
            catch (Exception ex) { Stop("Stopped after input error: " + ex.Message); return Win.CallNextHookEx(IntPtr.Zero, code, message, data); }
        }
        private void Emit(int key, bool down)
        {
            if (config.Live)
            {
                // During normal operation no output may start in an unrelated window.
                if (down && !TargetIsForeground()) throw new InvalidOperationException("Target lost focus.");
                ushort scan;
                if (!scans.TryGetValue(key, out scan) || scan == 0) scan = (ushort)Win.MapVirtualKey((uint)key, 0);
                if (scan == 0) throw new InvalidOperationException("No scan code for selected key.");
                bool isExtended;
                extended.TryGetValue(key, out isExtended);
                var input = new Win.Input { Type = 1, Data = new Win.InputUnion { Keyboard = new Win.KeyboardInput {
                    Scan = scan, Flags = (uint)(8 | (down ? 0 : 2) | (isExtended ? 1 : 0)), Extra = new UIntPtr(0x4B535442)
                } } };
                if (Win.SendInput(1, new Win.Input[] { input }, Marshal.SizeOf(typeof(Win.Input))) != 1)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows did not accept input. Target permissions or input handling may differ.");
            }
            owner.Log(clock.Elapsed.TotalSeconds.ToString("F3") + "  " + (config.Live ? "OUTPUT " : "PREVIEW ") + ((Keys)key) + (down ? " DOWN" : " UP"));
        }
        private void Publish()
        {
            var physical = new List<string>(); var logical = new List<string>();
            foreach (var pair in engine.Keys)
            {
                if (pair.Value.PhysicalDown) physical.Add(((Keys)pair.Key).ToString());
                if (pair.Value.OutputDown) logical.Add(((Keys)pair.Key).ToString());
            }
            owner.Summary = "Physical: " + (physical.Count == 0 ? "released" : string.Join(" + ", physical.ToArray()))
                + "     Filtered: " + (logical.Count == 0 ? "released" : string.Join(" + ", logical.ToArray()))
                + "\r\nPhysical down/up: " + engine.PhysicalPresses + "/" + engine.PhysicalReleases
                + "     Output down/up: " + engine.OutputPresses + "/" + engine.OutputReleases
                + "     Gaps bridged: " + engine.BridgedGaps;
        }
        private void Stop(string reason)
        {
            if (stopped) return;
            stopped = true; active = false;
            // Attempt every outstanding release even if one SendInput call fails.
            try { engine.Stop(); }
            catch (Exception ex) { reason += " Release could not be confirmed: " + ex.Message + " Press and release the affected keys manually."; }
            Publish(); owner.Status = reason; owner.Log(reason); owner.Running = false;
            Cleanup(); ExitThread();
        }
        private void Cleanup()
        {
            timer.Stop();
            if (hook != IntPtr.Zero) { Win.UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
            if (registered) { Win.UnregisterHotKey(window.Handle, 1); registered = false; }
        }
        protected override void Dispose(bool disposing)
        {
            if (!stopped) Stop("Stopped.");
            Cleanup(); timer.Dispose(); window.Dispose();
            base.Dispose(disposing);
        }
    }
}
