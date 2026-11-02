# Diagnosis and troubleshooting

## Separate the symptoms

| Observation | Possible explanation | Useful comparison |
| --- | --- | --- |
| One key misses slow, complete presses | Switch, socket, solder joint, connection, firmware, or input path problem | Test the same keyboard on another computer. |
| A held key reports extra releases | Chatter or another source of interrupted input | Compare physical and filtered transitions in preview. |
| Keys work alone but fail in specific combinations | Keyboard matrix, rollover limit, mode, or application behavior | Test each key alone, then repeat the exact combination. |
| Input works in desktop applications but fails in one game | Game bindings, background software, or a different input path | Compare another keyboard and isolate background applications. |
| Pressing harder changes the result | Possible contact or mechanical problem | Compare the same key across computers before changing Windows settings. |

These observations do not identify a component with certainty. Raw Input and low-level hook events are software observations.

## Count deliberate presses

Use the `20`-tap procedure in the [User guide](USER_GUIDE.md). Supply the expected count yourself. The diagnostic cannot count a physical press that produces no event.

If both counters show fewer than `20`, repeat the test with a known-good key. A low count alone does not prove that the switch is defective.

## Examine an interrupted hold

Hold D steadily in stabilizer preview. Examine whether the physical count includes extra releases and presses.

If the physical sequence contains short gaps, **Gaps bridged** can increase. The filtered output then contains fewer transitions. This result demonstrates the filter's behavior. It does not prove that live output improves a game.

If the initial press never appears, neither mode can reconstruct that press. If no gap appears during the problem, the release filter has no matching event to change.

## Test simultaneous keys

Use Keyboard Check for this comparison. It tracks device and scan information separately.

1. Test W alone.
2. Test A alone.
3. Test Shift alone.
4. Test Space alone.
5. Hold W.
6. While you hold W, press Shift.
7. While you hold W and Shift, press Space.
8. Release all keys.
9. Repeat the exact key combination that fails in the application.

A failure limited to a combination suggests a rollover or matrix limit. The diagnostic does not certify the keyboard's advertised rollover rating. The maximum-held count includes every keyboard used in the session.

## Interpret rapid re-press counts

Keyboard Check flags release-to-press intervals shorter than `35 ms`. A rapid intentional tap or delayed event delivery can also trigger this flag.

The `35 ms` diagnostic threshold is separate from the stabilizer's `12 ms` default release grace. A diagnostic flag does not mean that the stabilizer should use `35 ms`.

## Check Windows accessibility settings

Filter Keys can ignore brief or repeated presses. Sticky Keys changes modifier behavior. Toggle Keys produces lock-key sounds. An enabled activation shortcut does not mean that its feature is on.

Use **Refresh settings** after a manual Windows setting change. Settings that are off do not establish that the keyboard is healthy.

## Compare the connection

Compare a direct USB port with the current connection. If the keyboard supports wired operation, compare wired operation with wireless operation. Test another keyboard on the same computer.

Avoid model-specific reset shortcuts until you confirm the exact model and its manufacturer instructions. Do not assume that a shared USB identifier establishes the product model.

## Resolve stabilizer startup problems

| Message or behavior | Explanation | Next action |
| --- | --- | --- |
| F8 is unavailable | Another application or stabilizer session owns the hotkey. | Stop the other session or resolve the hotkey conflict. |
| The status remains armed | The target is not active, or a selected key remains down. | Focus the target and release the selected keys. |
| Arming times out | Activation conditions were not met within `30 seconds`. | Start a new session when the target is ready. |
| The session stops after an application switch | Focus loss ends an activated session. | Start a new session for the intended target. |
| The session stops after `2 minutes` | The experimental session limit has expired. | Start another test if needed. |
| Windows does not accept input | Permissions or target input handling differ. | Stop the session and compare in an ordinary desktop editor. |
| An editor produces one character during a hold | Smooth hold removes typematic repeats. | Evaluate held state rather than repeated text. |

If a key remains logically held after an error, physically press and release that key. Forced termination cannot guarantee release cleanup.

## Avoid unrelated cleanup

The startup scripts affect future sign-ins. They do not repair switches, add rollover, or establish lower input latency. Clearing caches does not restore an input event that the keyboard never reports.

Use the [related research](RESEARCH.md) to identify a matching cause. Do not apply a workaround only because another report also mentions a keyboard.
