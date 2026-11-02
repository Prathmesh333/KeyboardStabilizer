using System;
using System.Collections.Generic;

namespace KeyboardStabilizer
{
    internal enum OutputMode { SmoothHold, RepeatedTaps }

    internal sealed class KeyState
    {
        internal bool PhysicalDown, OutputDown;
        internal double? ReleaseAt;
        internal double NextPulse, PulseUpAt;
    }

    // Pure state machine. All times are monotonic milliseconds supplied by its host.
    internal sealed class StabilizerEngine
    {
        internal readonly Dictionary<int, KeyState> Keys = new Dictionary<int, KeyState>();
        internal readonly OutputMode Mode;
        internal readonly int ReleaseDelay, RepeatInterval, TapDuration;
        internal int PhysicalPresses, PhysicalReleases, BridgedGaps, OutputPresses, OutputReleases;
        private readonly Action<int, bool> emit;
        private double lastTime = double.NegativeInfinity;

        internal StabilizerEngine(IEnumerable<int> keys, OutputMode mode, int delay, int interval, int duration, Action<int, bool> output)
        {
            if (delay < 0 || delay > 100) throw new ArgumentOutOfRangeException("delay");
            if (interval < 20 || interval > 1000 || duration < 1 || duration >= interval) throw new ArgumentException("Invalid tap timing.");
            foreach (int key in keys) Keys.Add(key, new KeyState());
            if (Keys.Count == 0 || output == null) throw new ArgumentException("Select keys and an output handler.");
            Mode = mode; ReleaseDelay = delay; RepeatInterval = interval; TapDuration = duration; emit = output;
        }

        private void SetOutput(int key, KeyState state, bool down)
        {
            if (state.OutputDown == down) return;
            // Commit only after the host successfully emits the transition.
            emit(key, down);
            state.OutputDown = down;
            if (down) OutputPresses++; else OutputReleases++;
        }

        internal bool Input(int key, bool down, double now)
        {
            KeyState state;
            if (!Keys.TryGetValue(key, out state)) return false;
            // Expire an overdue release before processing a new press, even if a UI timer ran late.
            Advance(now);
            if (state.PhysicalDown == down) return true; // Ignore typematic repeats and orphan ups.
            state.PhysicalDown = down;
            if (down)
            {
                PhysicalPresses++;
                bool bridged = state.ReleaseAt.HasValue;
                if (bridged) { BridgedGaps++; state.ReleaseAt = null; }
                if (Mode == OutputMode.SmoothHold) SetOutput(key, state, true);
                else if (!bridged)
                {
                    SetOutput(key, state, true);
                    state.PulseUpAt = now + TapDuration;
                    state.NextPulse = now + RepeatInterval;
                }
            }
            else
            {
                PhysicalReleases++;
                state.ReleaseAt = now + ReleaseDelay;
                if (ReleaseDelay == 0) Advance(now);
            }
            return true;
        }

        internal void Advance(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < lastTime) throw new ArgumentException("Time must be finite and monotonic.");
            lastTime = now;
            foreach (var pair in Keys)
            {
                KeyState state = pair.Value;
                if (state.ReleaseAt.HasValue && now >= state.ReleaseAt.Value)
                {
                    SetOutput(pair.Key, state, false);
                    state.ReleaseAt = null;
                }
                if (Mode != OutputMode.RepeatedTaps) continue;
                if (state.OutputDown && now >= state.PulseUpAt) SetOutput(pair.Key, state, false);
                // Never issue catch-up bursts after a stalled timer. Restart cadence from now.
                if (state.PhysicalDown && !state.OutputDown && now >= state.NextPulse)
                {
                    SetOutput(pair.Key, state, true);
                    state.PulseUpAt = now + TapDuration;
                    state.NextPulse = now + RepeatInterval;
                }
            }
        }

        internal void Stop()
        {
            Exception failure = null;
            foreach (var pair in Keys)
            {
                try { SetOutput(pair.Key, pair.Value, false); }
                catch (Exception ex) { failure = ex; }
                pair.Value.PhysicalDown = false;
                pair.Value.ReleaseAt = null;
            }
            if (failure != null) throw failure;
        }
    }
}
