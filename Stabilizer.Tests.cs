using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KeyboardStabilizer
{
    internal static class StabilizerTests
    {
        private static int passed;
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); passed++; }
        private static StabilizerEngine Make(List<string> output, OutputMode mode = OutputMode.SmoothHold, int delay = 12)
        { return new StabilizerEngine(new int[] { 68, 87 }, mode, delay, 80, 10, (key, down) => output.Add(key + (down ? "+" : "-"))); }
        public static int Main()
        {
            try
            {
                var output = new List<string>(); var engine = Make(output);
                engine.Input(68, true, 0); engine.Input(68, false, 100); engine.Input(68, true, 104);
                Check(output.Count == 1 && engine.BridgedGaps == 1, "A short false release must preserve the hold.");
                engine.Input(68, false, 500); engine.Advance(511);
                Check(output.Count == 1, "Real release must wait through grace period.");
                engine.Advance(512);
                Check(output.Count == 2 && output[1] == "68-" && !engine.Keys[68].OutputDown, "Real release must eventually reach output.");
                output.Clear(); engine = Make(output); engine.Input(68, true, 0); engine.Input(68, true, 100);
                Check(engine.PhysicalPresses == 1 && output.Count == 1, "Typematic repeats must not become new presses.");
                engine.Input(87, true, 110); engine.Input(68, false, 120); engine.Advance(132);
                Check(engine.Keys[87].OutputDown && !engine.Keys[68].OutputDown, "Selected keys must have independent holds.");
                engine.Stop(); engine.Stop();
                Check(output.Count == 4 && output[3] == "87-", "Stop must release held keys exactly once.");
                output.Clear(); engine = Make(output); engine.Input(68, false, 0);
                Check(output.Count == 0, "An orphan release must not create output.");
                engine.Input(68, true, 1); engine.Input(68, false, 10); engine.Input(68, true, 22);
                Check(output.Count == 3 && engine.BridgedGaps == 0, "The exact release deadline must separate intentional taps.");
                output.Clear(); engine = Make(output); engine.Input(68, true, 0); engine.Input(68, false, 10); engine.Input(68, true, 100);
                Check(output.Count == 3 && engine.BridgedGaps == 0, "Late timer must not swallow a new press after deadline.");
                output.Clear(); engine = Make(output, OutputMode.SmoothHold, 0); engine.Input(68, true, 0); engine.Input(68, false, 1);
                Check(output.Count == 2, "Zero delay must release immediately.");
                output.Clear(); engine = Make(output, OutputMode.RepeatedTaps); engine.Input(68, true, 0); engine.Advance(10); engine.Advance(80); engine.Advance(90);
                Check(string.Join(",", output) == "68+,68-,68+,68-", "Repeat mode must produce separated taps.");
                engine.Input(68, false, 100); engine.Advance(1000);
                Check(output.Count == 4, "Repeat mode must not continue after release.");
                output.Clear(); engine = Make(output, OutputMode.RepeatedTaps); engine.Input(68, true, 0); engine.Advance(10000);
                Check(output.Count == 3, "Timer stalls must not cause catch-up bursts.");
                engine.Stop(); Check(output.Count == 4 && output[3] == "68-", "Repeat stop must release an active pulse.");
                Check(!engine.Input(65, true, 10001), "Unselected keys must pass through.");
                bool backwardsRejected = false;
                try { engine.Advance(1); } catch (ArgumentException) { backwardsRejected = true; }
                Check(backwardsRejected, "Non-monotonic time must be rejected.");
                int attempts = 0;
                engine = new StabilizerEngine(new int[] {68, 87}, OutputMode.SmoothHold, 12, 80, 10,
                    delegate(int key, bool down) { if (!down) { attempts++; if (key == 68) throw new Exception("simulated output failure"); } });
                engine.Input(68, true, 0); engine.Input(87, true, 1);
                try { engine.Stop(); } catch (Exception) { }
                Check(attempts == 2 && !engine.Keys[87].OutputDown, "Release failure on one key must not prevent other releases.");
                Check(Marshal.SizeOf(typeof(Win.Input)) == (IntPtr.Size == 8 ? 40 : 28), "Native INPUT layout must match Windows ABI.");
                Check(Marshal.SizeOf(typeof(Win.HookKey)) == (IntPtr.Size == 8 ? 24 : 20), "Native keyboard hook layout must match Windows ABI.");
                Console.WriteLine("PASS: " + passed + " checks (glitch bridging, real releases, repeats, independent keys, stop cleanup, late timers, output failure, native layouts).");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL: " + ex.Message); return 1; }
        }
    }
}
