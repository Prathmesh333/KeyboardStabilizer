# User guide

## Select the correct tool

Use **Keyboard Check** to inspect the input that Windows reports. Use **Keyboard Stabilizer** to compare reported events with filtered events. Neither tool measures the physical switch directly.

Build the applications before you use these procedures. See [Build and test](BUILDING.md).

## Run the simulated example

The example always uses D. The selected key checkboxes do not change the example.

1. Open `bin\KeyboardStabilizer.exe`.
2. Select **Smooth hold (start here)**.
3. Set **Release grace (ms)** to `12`.
4. Select **Show glitch example**.

The example supplies a release at `100 ms` and another press at `104 ms`. Smooth hold joins this gap. The output remains held until `512 ms`, after the deliberate release at `500 ms`.

The example sends no system input. Its timestamps come from a deterministic simulation. They are not measurements of Windows timer accuracy.

## Examine a physical hold in preview

Use one physical keyboard during a stabilizer session. The stabilizer does not separate events by keyboard device.

1. Keep **Enable live output** cleared.
2. Select the key that you want to examine. D is selected by default.
3. Select **Smooth hold (start here)**.
4. Set **Release grace (ms)** to `12`.
5. Select **Start preview**.
6. Release all selected keys.
7. Keep the stabilizer window active.
8. Hold the selected key steadily.
9. Examine the physical state, filtered state, and event counts.
10. Release the selected key.
11. Press **F8** to stop the session.

For one uninterrupted physical hold, the physical count is one press and no release during the hold. After release, the count is one press and one release. Repeated operating-system key-down events do not increase the press count.

If extra release and press transitions appear during steady pressure, the filter can join sufficiently short gaps. A joined gap does not establish the cause of the transitions.

Preview lets original selected-key events pass through the hook. The stabilizer interface disables keyboard navigation during a session. Use the mouse for its controls. F8 remains the stop key.

## Test live output in a desktop editor

Live output changes selected-key input. It adds release delay in Smooth hold mode. Test in a disposable desktop editor before you consider another application. Valorant permission and compatibility remain unverified.

1. Stop the current preview session.
2. Open a disposable document in a desktop text editor.
3. Return to Keyboard Stabilizer.
4. Select **Enable live output**.
5. Select **Refresh windows** if the editor is absent from the list.
6. Select the editor window in the target list.
7. Select **Arm live output**.
8. Switch to the selected editor within `30 seconds`.
9. Release all selected keys.
10. Press and hold the selected key.
11. Release the selected key.
12. Press **F8** to stop the session.

The worker activates the session when the target is active and the selected keys are released. In live mode, the hook blocks original selected-key events. The output handler sends filtered scan-code events through `SendInput`.

Smooth hold suppresses typematic repeat events. It does not preserve normal repeated-character typing. A single character in an editor does not indicate failure of held-state filtering.

When focus leaves the target, the worker stops the session. Windows input injection is global. Focus checks cannot provide atomic delivery to a particular window. See [Compatibility and privacy](COMPATIBILITY_AND_PRIVACY.md).

## Compare repeated taps

Repeated taps intentionally interrupts a hold. This behavior can make movement less consistent.

1. Stop the current session.
2. Select **Repeated taps (experimental)**.
3. Set **Tap interval (ms)** to the required interval.
4. Select **Show glitch example** to inspect the simulated events.

The default interval is `80 ms`. Each tap has a nominal down duration of `10 ms`. The interface permits intervals from `20 ms` to `500 ms`. Windows scheduling can lengthen actual intervals and tap durations.

The engine does not generate a burst of overdue taps after a timer delay. A new tap uses the current time as the start of its cadence.

## Adjust the release grace

The interface permits a release grace from `0 ms` to `50 ms`. The default is `12 ms`. A value of `0 ms` accepts a reported release immediately.

Use preview observations to choose a value. A longer grace joins longer gaps but delays deliberate releases. The filter also merges intentional release and press sequences shorter than the grace period.

The engine accepts a new press at the exact release deadline as a separate press. Timer scheduling can delay output beyond the configured grace.

## Stop a session

Press **F8** to stop an armed or active session. F8 also works while Shift or Ctrl is held through the physical keyboard hook. The application requires F8 to be available before it starts.

The **Stop / release keys** button also stops the session. A session ends when focus leaves an activated target. Normal application closure also requests a stop.

An armed session expires after `30 seconds`. An active session ends after `2 minutes`. These limits belong to this experiment. They do not indicate a keyboard failure.

Stop attempts to release each generated hold. If Windows rejects a release or the process terminates unexpectedly, physically press and release the affected key.

## Use Keyboard Check

1. Open `bin\KeyboardCheck.exe`.
2. Select **Reset counts**.
3. Select **Start test** if the test is stopped.
4. Slowly tap a problem key exactly `20` times. Fully release the key between taps.
5. Compare **Down** and **Up** with `20`.
6. Hold the key steadily for several seconds.
7. Release the key.
8. Examine whether **HELD** clears.

Use the **Windows checks** tab to read Filter Keys, Sticky Keys, and Toggle Keys settings. The settings button opens Windows Settings. The diagnostic does not change those settings.

Keyboard Check pauses when its window loses focus. It clears held state but keeps counts. Its device numbers describe discovery order within the session, not model names.
