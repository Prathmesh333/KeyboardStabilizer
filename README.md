# Keyboard Stabilizer

Windows tools to diagnose unreliable keyboard input and test two software workarounds: **Smooth hold** and **Repeated taps**.

The project began with a Cosmic Byte keyboard that missed presses and interrupted held movement keys. The D key was the main problem. The tools help examine the input that Windows receives. They cannot repair a switch or recover an event that the keyboard never sends.

**Status: experimental.** Hardware improvement and Valorant compatibility are not verified. Live output starts off. The project has no Riot approval or endorsement.

## Tools

| Tool | Purpose | Changes keyboard input? |
| --- | --- | --- |
| Keyboard Stabilizer | Compare physical events with filtered events. Optionally send filtered input to a selected window. | Only when you enable live output and start a session. |
| Keyboard Check | Count presses and releases. Examine held keys, key combinations, and Windows accessibility settings. | No. |
| Startup scripts | Inspect or change a small list of optional startup entries. | No. These separate scripts can change the Windows registry. |

## Build and run

Use Windows 10 or Windows 11 on an x64 computer with .NET Framework 4.x and its C# compiler. The build uses no NuGet packages. A .NET SDK is not required.

```powershell
git clone https://github.com/Prathmesh333/KeyboardStabilizer.git
cd KeyboardStabilizer
.\Build-Stabilizer.ps1 -Test
.\Build.ps1 -Test
.\bin\KeyboardStabilizer.exe
```

If PowerShell blocks the script, follow your local execution policy. Do not change a managed policy to run this project. See [Build and test](docs/BUILDING.md).

The repository contains source files. Executables are generated locally and excluded from Git. Successful [Windows workflow runs](https://github.com/Prathmesh333/KeyboardStabilizer/actions/workflows/windows.yml) also produce a downloadable artifact.

## Test D in preview

1. Open `bin\KeyboardStabilizer.exe`.
2. Keep **D** selected.
3. Keep **Smooth hold (start here)** selected.
4. Keep **Release grace (ms)** at `12`.
5. Keep **Enable live output** cleared.
6. Select **Start preview**.
7. Release D to let the session activate.
8. Hold D steadily.
9. Examine the physical counts, filtered counts, and **Gaps bridged** value.
10. Press **F8** to stop the session.

Preview does not replace your input. The **Show glitch example** button uses a simulated D-key sequence. See the [User guide](docs/USER_GUIDE.md) before you enable live output.

## How the filter works

**Smooth hold** sends the first press immediately through the output handler. When a release arrives, the filter waits for the grace period. If another press arrives before the deadline, the filter keeps the output held.

With a `12 ms` grace period, the built-in example produces this result:

```text
Time        Physical event                Filtered event
0 ms        D down                        D down
100 ms      D up                          Wait for the release deadline
104 ms      D down                        Keep D held
500 ms      D up                          Wait for the release deadline
512 ms      No new press                  D up
```

The same delay also affects a deliberate release. The filter can merge intentional quick taps. Windows scheduling adds timing variation.

**Repeated taps** turns a reported hold into separate presses and releases. The default interval is `80 ms`. The nominal tap duration is `10 ms`. This mode can interrupt continuous movement. It does not reconstruct missing physical input.

## Operating limits

- The interface supports W, A, S, D, Space, Left Shift, and Right Shift. D is the default selection.
- A session requires released selected keys before activation.
- Arming expires after `30 seconds`.
- An active test lasts at most `2 minutes`.
- F8, the Stop button, focus loss, and normal closure stop a session.
- Stop attempts to release generated holds. A crash or rejected input can prevent cleanup.
- Live output uses Windows hooks and `SendInput`. Some games and elevated applications handle this input differently.

Riot restricts unauthorized automation. This project does not establish that live output is permitted in Valorant. Read [Compatibility and privacy](docs/COMPATIBILITY_AND_PRIVACY.md) and [Riot's terms](https://www.riotgames.com/en/terms-of-service).

## Documentation

| Document | Content |
| --- | --- |
| [Documentation index](docs/README.md) | Reading order, terminology, and writing conventions |
| [User guide](docs/USER_GUIDE.md) | Preview, live output, settings, and stop behavior |
| [Diagnosis and troubleshooting](docs/TROUBLESHOOTING.md) | Missed presses, false releases, rollover, and application problems |
| [Architecture](docs/ARCHITECTURE.md) | State machine, worker thread, input handling, and source map |
| [Frequently asked questions](docs/FAQ.md) | Why the project exists, what it can change, and what remains unverified |
| [Build and test](docs/BUILDING.md) | Requirements, commands, automated checks, and CI artifacts |
| [Research](docs/RESEARCH.md) | Related GitHub reports and primary references |
| [Startup scripts](docs/STARTUP_SCRIPTS.md) | Optional registry changes, backup, and restoration |
| [Contributing](CONTRIBUTING.md) | Change scope, checks, and bug reports |

## License

[MIT](LICENSE). Keep the copyright and permission notice when you reuse the software.
