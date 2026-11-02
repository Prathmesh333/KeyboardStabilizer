# Documentation

Start with the [project README](../README.md). Use the [User guide](USER_GUIDE.md) to operate the application. Use [Troubleshooting](TROUBLESHOOTING.md) to interpret observations.

| Reader task | Document |
| --- | --- |
| Understand the purpose and limitations | [FAQ](FAQ.md) |
| Operate preview or live output | [User guide](USER_GUIDE.md) |
| Diagnose missed or interrupted input | [Troubleshooting](TROUBLESHOOTING.md) |
| Review the code design | [Architecture](ARCHITECTURE.md) |
| Build applications and run checks | [Build and test](BUILDING.md) |
| Review game compatibility and data handling | [Compatibility and privacy](COMPATIBILITY_AND_PRIVACY.md) |
| Read related reports | [Research](RESEARCH.md) |
| Use the optional startup scripts | [Startup scripts](STARTUP_SCRIPTS.md) |

## Project terminology

| Term | Meaning in this project |
| --- | --- |
| Physical event | An input event that the stabilizer hook receives without an injected-event flag. It is not a direct measurement of switch position. |
| Filtered event | A press or release produced by the state machine. Preview displays the event. Live output sends the event to Windows. |
| Release grace | The time that Smooth hold waits before it accepts a release. |
| Gap | A reported release followed by another press. |
| Gaps bridged | The number of gaps shorter than the configured grace period that the filter joins. This count does not prove a hardware fault. |
| Target window | The window selected for a live session. The worker checks its window handle and process identifier. |
| Chatter | Unwanted press and release transitions caused by an unstable contact or another input problem. The tools cannot identify the cause from timing alone. |
| Rollover | A keyboard's ability to report simultaneous key presses. Software filtering cannot add missing hardware support. |

## Writing conventions

The project uses controlled technical English as a writing approach. This choice does not establish compliance with ASD-STE100 or another external standard.

The documentation uses US English, explicit conditions, and one principal reader action per numbered step. Interface labels retain their exact text. Times use milliseconds (`ms`) unless a duration states another unit.

Source code defines implementation behavior. Automated results support only the cases that the tests exercise. Research reports provide context, not proof that the same cause affects another computer.
