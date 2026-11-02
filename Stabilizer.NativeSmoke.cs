using System;
using System.Diagnostics;
using System.Threading;

namespace KeyboardStabilizer
{
    // Optional interactive-desktop check. It never activates a target or sends input.
    internal static class NativeSmoke
    {
        public static int Main()
        {
            var runner = new InputRunner();
            runner.Start(new RunConfig {
                Keys = new int[] { 68 }, Mode = OutputMode.SmoothHold, Delay = 12, Interval = 80,
                Live = false, Target = new WindowChoice { Handle = IntPtr.Zero, ProcessId = 0, Title = "No target" }
            });
            var clock = Stopwatch.StartNew();
            while (!runner.Running && !runner.Finished && clock.ElapsedMilliseconds < 3000) Thread.Sleep(10);
            bool initialized = runner.Running;
            runner.WaitForStop();
            Console.WriteLine("Initialized: " + initialized + "; finished: " + runner.Finished + "; status: " + runner.Status);
            return initialized && runner.Finished ? 0 : 1;
        }
    }
}
