# Related reports and references

This project uses reports to compare symptoms and identify testable explanations. A similar symptom does not prove a shared cause. Research does not establish game approval.

## Related GitHub reports

| Source | Relevant observation | Limit of the evidence |
| --- | --- | --- |
| [PowerToys #4291](https://github.com/microsoft/PowerToys/issues/4291) | An older report describes Valorant input delay. Comments describe continued movement after key release and recovery after PowerToys exits. | The report concerns particular PowerToys versions and systems. It does not identify the cause of every held-key fault. |
| [PowerToys #13620](https://github.com/microsoft/PowerToys/issues/13620) | A proposal discusses disabling Keyboard Manager for full-screen applications because of input delay reports. | A proposal is not proof that another computer has this conflict. |
| [PowerToys #46693](https://github.com/microsoft/PowerToys/issues/46693) | A specific remapping sequence leaves Ctrl logically held. The report identifies an AltGr-related state problem. | The sequence requires Keyboard Manager behavior. It does not explain an unidentified keyboard hardware fault. |
| [KeyboardChatterBlocker](https://github.com/FreneticLLC/KeyboardChatterBlocker) | The project filters duplicate presses with per-key thresholds. Its documentation discusses game compatibility. | Duplicate-press filtering cannot recover an initial press that never arrives. |
| [KeyboardChatterBlocker #12](https://github.com/FreneticLLC/KeyboardChatterBlocker/issues/12) | Reports describe held movement keys that stop registering when filtering blocks a chatter press. The discussion includes release-delay tradeoffs. | The documented workaround includes disabling the blocker during games. The discussion is not a verified Valorant repair. |
| [CosmicRGB](https://github.com/TangledDaunT/artemis-cosmicrgb) | The community project lists `04D9:A1CD` for CB-GK-37. It controls lighting and macros on macOS. | Shared identifiers do not confirm a model. This macOS utility is not a Windows missed-press repair. |

The repository does not bundle these utilities or copy their filtering implementations. The state machine and diagnostic source are contained in this repository.

## Windows documentation

- [Keyboard Input Overview](https://learn.microsoft.com/en-us/windows/win32/inputdev/about-keyboard-input) describes key-down, key-up, and typematic behavior.
- [LowLevelKeyboardProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc) describes hook callbacks, message loops, and timeout behavior.
- [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput) describes generated input and integrity restrictions.
- [KEYBDINPUT](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput) describes scan codes and injected keyboard events.
- [Using Raw Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/using-raw-input) describes the separate diagnostic input path.
- [RAWKEYBOARD](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawkeyboard) describes scan information and keyboard overrun reports.
- [Windows accessibility settings](https://support.microsoft.com/en-gb/accessibility/windows/make-your-mouse-keyboard-and-other-input-devices-easier-to-use) describes Filter Keys and modifier assistance.

## Game and hardware guidance

- [Riot keyboard and mouse troubleshooting](https://support.riotgames.com/en-us/riot/performance/troubleshooting-keyboardmouse-issues/) includes peripheral comparisons and background-application isolation.
- [Riot third-party application policy](https://support.riotgames.com/en-us/riot/events/third-party-applications) describes concerns about automated actions and competitive integrity.
- [Riot terms](https://www.riotgames.com/en/terms-of-service) control use of unauthorized programs with Riot services.
- [ASUS anti-ghosting explanation](https://www.asus.com/us/support/faq/1038401/) describes simultaneous-key design limits.

Policies and external project behavior can change. Check current primary sources before a compatibility decision.
