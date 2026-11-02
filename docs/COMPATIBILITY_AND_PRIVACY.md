# Compatibility and privacy

## Supported environment

The project targets Windows 10 and Windows 11 desktop applications on x64 computers. The build scripts require the .NET Framework C# compiler under `Microsoft.NET\Framework64\v4.0.30319`.

The project does not include a Linux driver, macOS application, USB firmware, kernel driver, or polling-rate modification. The desktop build does not establish compatibility with every application.

## Games and permissions

Live output uses a low-level keyboard hook and Windows `SendInput`. A game can use a different input path or reject generated input. A successful `SendInput` return value does not prove that a game accepted the event.

Windows limits input injection across integrity levels. The application does not elevate itself. If an elevated target rejects output, the project does not bypass that restriction.

Valorant compatibility is unverified. Riot approval is not established. Riot's [terms](https://www.riotgames.com/en/terms-of-service) restrict unauthorized automation programs. Riot also publishes a [third-party application policy](https://support.riotgames.com/en-us/riot/events/third-party-applications).

The MIT license grants software rights. It does not grant permission to use automation in a particular game.

## Timing and release behavior

Smooth hold delays real releases as well as unwanted releases. The filter cannot distinguish intent from event timing alone. Intentional quick taps can merge into one hold.

Windows timers are not exact. Scheduling, CPU load, and hook delivery affect output timing. The configured grace is not a measured hardware latency value.

The target check compares the current foreground window with the selected identity. Windows input injection remains global. The check and injection are not atomic. A focus change can redirect an event.

Stop attempts to release generated holds. Cleanup releases can reach the new foreground window after focus changes. A process crash, forced termination, secure-desktop transition, or rejected input can prevent reliable cleanup.

If a key appears held after an error, physically press and release that key.

## Device handling

The stabilizer hook combines keyboard devices. Use one physical keyboard during a test. A release from a second keyboard can change the same virtual-key state.

Keyboard Check keeps device-specific records. Its device numbers are session identifiers. Neither application establishes a keyboard model from its USB identifier.

## Data handled by the applications

| Data | Handling |
| --- | --- |
| Keyboard hook events | Windows delivers keyboard events while the stabilizer session exists. The handler processes selected keys and the stop key. |
| Selected-key output transitions | The stabilizer retains the latest `120` event messages in memory. |
| Diagnostic transitions | Keyboard Check retains the latest `150` transitions in memory. |
| Target window titles | The stabilizer reads visible window titles locally to populate the target list. |
| Accessibility settings | Keyboard Check reads the current Windows settings for display. |

The application code writes no key log to disk. It sends no network requests. Counts and application settings do not persist after closure. Avoid entering private information in a diagnostic window.

## Data handled by the startup scripts

The optional startup scripts read application launch commands from the current user's Windows registry. Apply writes those commands to a local backup before it removes selected startup entries.

Backups can contain user-specific paths and launch arguments. The repository ignores `cleanup-backups/` and `*.clixml`. Do not attach these backups to public issues.

Application logs, screenshots, crash dumps, or terminal output can also reveal private information. Remove private information before you share a report.
