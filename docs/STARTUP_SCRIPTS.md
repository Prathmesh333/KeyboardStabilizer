# Optional startup scripts

These scripts are separate maintenance tools. Neither input application calls them. Startup cleanup is not a demonstrated keyboard repair.

## Scope

`GamingStartup.ps1` reads the current user's `Run` registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

The script selects these entry names:

- `Teams`
- `electron.app.BlueStacks Services`
- `RobloxPlayerBeta`
- `GoogleChromeAutoLaunch_` followed by hexadecimal characters
- `MicrosoftEdgeAutoLaunch_` followed by hexadecimal characters

Apply removes matching values after it saves and checks a backup. This change affects those entries at future sign-ins. Apps and updates can recreate entries. Other startup mechanisms are outside the script's scope.

The script does not uninstall applications, stop current processes, clear caches, change drivers, or alter game configuration.

## Inspect without changes

```powershell
.\GamingStartup.ps1 -Mode Inspect
```

Inspect is the default mode. It displays matching entry names and value types.

## Apply the selected cleanup

Apply changes startup behavior for the listed applications. Backups can contain private paths and launch arguments. Keep the backup outside public reports.

1. Run the command below.
2. Retain the backup path printed by the script.

```powershell
.\GamingStartup.ps1 -Mode Apply
```

The script writes a timestamped `.clixml` file under `cleanup-backups/`. It verifies the saved values before it removes registry entries. If removal fails, the script attempts to restore entries already removed by that invocation.

## Restore a backup

Use the exact backup path printed during Apply. Replace the example filename below with your backup filename.

```powershell
.\UndoGamingStartup.ps1 -BackupPath .\cleanup-backups\startup-YYYYMMDD-HHMMSS-fff.clixml
```

The equivalent command is:

```powershell
.\GamingStartup.ps1 -Mode Restore -BackupPath .\cleanup-backups\startup-YYYYMMDD-HHMMSS-fff.clixml
```

Restore validates the registry path, entry names, and value types. If a different value already exists, Restore skips that entry and prints a warning. It does not overwrite a subsequent change.

Only restore a backup that you trust. Restored commands can run at a later sign-in. The public repository includes no personal backup files.
