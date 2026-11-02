# Frequently asked questions

## Why does this project exist?

The initial keyboard missed presses, interrupted holds, and behaved differently with pressure. The D key was the main reported problem. The project separates diagnosis from an experimental input filter.

## Can the software make a faulty keyboard equivalent to a new gaming keyboard?

No. Software cannot repair a switch, socket, solder joint, matrix, cable, or controller. A filter can change events that Windows receives. It cannot recover an event that never arrives.

## What can Smooth hold help with?

Smooth hold can join a reported release followed by another press before the release deadline. The default grace is `12 ms`. The filtered state remains held across that gap.

This behavior is useful only when the observed fault matches that event pattern. Hardware improvement has not been established.

## Why delay releases instead of sending presses continuously?

A normal hold has a press followed later by a release. Repeated taps add releases between presses. Those releases can interrupt movement.

Smooth hold preserves a continuous filtered state across short gaps. Repeated taps exists as a separate experimental comparison.

## Why can the program not continue holding D until my finger leaves the switch?

The keyboard does not expose finger contact to this program. A reported release can mean an intentional release or an unwanted interruption. The filter uses timing because it cannot measure intent.

## Does Smooth hold add delay?

Yes. The filter delays a real release by the configured grace. Windows scheduling can add more delay. The first press does not receive an intentional grace delay, but the software path still has overhead.

## Can the filter swallow intentional quick taps?

Yes. If the new press arrives before the release deadline, the filter joins the sequence. A press at the exact deadline becomes a separate press.

## What does Repeated taps do?

Repeated taps converts a reported hold into separate presses and releases. Its default interval is `80 ms`. The nominal down duration is `10 ms`. It can worsen movement consistency.

## What does preview change?

Preview calculates filtered transitions and displays counters. It does not suppress selected original events or send filtered events. The application reserves F8 while the session exists.

The interface also disables keyboard navigation during the session. Use the mouse for its controls.

## Why start with D?

D was the reported problem key. The interface also supports W, A, S, Space, Left Shift, and Right Shift. Select only the keys needed for a comparison.

## Can I add another key?

The interface has no arbitrary-key field. A code change is required to add another key. Review scan-code behavior and add meaningful tests before you extend the supported selection.

## Is this software approved for Valorant?

No approval is established. Valorant compatibility is also unverified. Read [Compatibility and privacy](COMPATIBILITY_AND_PRIVACY.md) before you consider live output.

## Does this project bypass Vanguard or other anti-cheat systems?

No. The code does not include an anti-cheat bypass. It uses ordinary Windows hooks and `SendInput`.

## Can the software fix ghosting or add N-key rollover?

No. If the keyboard does not report a key combination, this filter cannot reconstruct that combination. Use Keyboard Check to compare individual keys with the failing combination.

## Can I increase the polling rate or change actuation sensitivity?

The project has no polling-rate or actuation adjustment. It does not modify keyboard firmware. A software timing setting does not change the physical actuation point.

## Can cache cleanup or startup cleanup repair missed presses?

Cleanup does not restore a missing keyboard event. Startup changes can reduce optional background activity after sign-in. That result does not establish the cause of a keyboard fault.

The startup scripts are separate from the input applications. Neither application invokes those scripts.

## Is the keyboard model confirmed?

No. A community project associates USB identifier `04D9:A1CD` with CB-GK-37. Shared identifiers do not establish a model with certainty. Confirm the label and manufacturer documentation before a model-specific reset or repair.

## Why does the diagnostic use 35 ms while the filter defaults to 12 ms?

Keyboard Check flags re-press intervals shorter than `35 ms` for inspection. The flag is not a fault diagnosis. The stabilizer uses a separate `12 ms` default grace to test hold filtering.

## Why does a session end after two minutes?

The two-minute limit bounds the live-input experiment. Arming has a separate `30-second` limit. Start another session when needed.

## Does F8 work while I hold Shift?

The physical keyboard hook recognizes F8 with modifiers. The application also registers a global F8 hotkey. A conflicting hotkey prevents session startup.

## Can I use two keyboards at once?

Keyboard Check separates devices. Keyboard Stabilizer combines virtual-key events from devices. Use one keyboard during a stabilizer test to avoid mixed holds and releases.

## Why does a held key type only one character in a text editor?

Smooth hold ignores typematic repeats. The editor is useful for initial input comparison, but Smooth hold is not a repeated-character typing tool.

## Why does automated input not appear in preview?

The stabilizer ignores events with injected-input flags. This rule prevents its output from feeding back into the filter. Many automation tools produce excluded events.

## Are logs or settings saved?

The applications retain event messages and counts in memory. They do not save settings or key logs. The optional startup scripts create registry-value backup files. See [data handling](COMPATIBILITY_AND_PRIVACY.md).

## What do the automated checks prove?

The checks exercise the state machine, tracker behavior, and native structure sizes. The optional native smoke check exercises hook startup and shutdown. These checks do not prove hardware repair, exact timer latency, or game acceptance.

## Which license applies?

The [MIT license](../LICENSE) applies to the original source and documentation in this repository. Referenced projects retain their own licenses. The repository links to research instead of bundling third-party utilities.
