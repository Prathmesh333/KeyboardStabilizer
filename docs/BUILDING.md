# Build and test

## Requirements

- Windows 10 or Windows 11 desktop environment on x64.
- .NET Framework 4.x runtime and its C# compiler.
- PowerShell capable of running local scripts.
- Git, if you clone the repository.

The scripts use this compiler path:

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

The compiler directory name does not identify the installed .NET Framework release. The build uses Windows Forms, System.Drawing, and framework libraries. It has no NuGet dependencies.

## Build the applications

Run these commands from the repository directory:

```powershell
.\Build-Stabilizer.ps1 -Test
.\Build.ps1 -Test
```

The scripts produce `bin\KeyboardStabilizer.exe` and `bin\KeyboardCheck.exe`. The `-Test` option also builds and runs console test executables.

The application executable is portable within a compatible Windows environment. It does not require an installer or administrator privileges. Target input restrictions still apply.

## Keep an existing application open during a build

Windows can lock a running executable. Use another output directory instead of replacing the active executable:

```powershell
.\Build-Stabilizer.ps1 -Test -OutputDirectory .\artifacts\local-check
.\Build.ps1 -Test -OutputDirectory .\artifacts\local-check
```

The diagnostic build also accepts `-OutputName` for an alternate executable filename. These parameters change generated output paths. They do not change application behavior.

## Run the optional native smoke check

This check requires an interactive Windows desktop. It briefly registers F8 and installs the input hook. It uses no active target and sends no generated keyboard input.

1. Stop other stabilizer sessions so F8 is available.
2. Run the command below.

```powershell
.\Build-Stabilizer.ps1 -Test -NativeSmoke -OutputDirectory .\artifacts\native-check
```

The expected result includes `Initialized: True` and `finished: True`. A hotkey conflict or unavailable desktop can prevent startup. The native smoke check does not exercise live output into another application.

## Automated coverage

The stabilizer suite currently has `19` checks. Coverage includes:

- Short release gaps and real release deadlines.
- Repeated key-down suppression and orphan releases.
- Independent state for selected keys.
- Stop behavior and release failures.
- Deadline boundaries and late timers.
- Repeated-tap cadence without catch-up bursts.
- Unselected keys and non-monotonic timestamps.
- Native `INPUT` and hook structure sizes.

The diagnostic suite covers six tracker scenarios: orphan release, held repeat, rapid re-press, device separation, focus reset, and the threshold boundary.

Neither suite runs a game, measures physical actuation, or validates hardware improvement.

## GitHub Actions

The [Windows workflow](../.github/workflows/windows.yml) builds both applications and runs their deterministic suites on `windows-latest`. Pushes, pull requests, and manual dispatches trigger the workflow.

Successful runs upload `keyboard-tools-windows` with both application executables. Artifacts expire after `14 days`. GitHub can require sign-in to download an artifact.

The workflow does not run native hook smoke checks, alter startup settings, inject live input, or create a GitHub Release.

## Common build failures

| Failure | Resolution |
| --- | --- |
| Compiler not found | Confirm the framework compiler exists at the documented path. The scripts do not substitute a .NET SDK compiler. |
| Output executable is locked | Close that application or select a different output directory. |
| PowerShell blocks scripts | Follow the execution policy for your computer. Do not bypass an organization policy. |
| Native smoke cannot register F8 | Stop other sessions or resolve the hotkey conflict. |

Generated files belong in `bin/` or `artifacts/`. Both directories are excluded from Git.
