param(
    [ValidateSet('Inspect', 'Apply', 'Restore')][string]$Mode = 'Inspect',
    [string]$BackupPath
)
$ErrorActionPreference = 'Stop'
$runPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$fixedNames = @('Teams', 'electron.app.BlueStacks Services', 'RobloxPlayerBeta')
function Test-SelectedStartupName([string]$Name) {
    return $Name -in $fixedNames -or $Name -match '^GoogleChromeAutoLaunch_[A-Fa-f0-9]+$' -or $Name -match '^MicrosoftEdgeAutoLaunch_[A-Fa-f0-9]+$'
}
$runKey = Get-Item -LiteralPath $runPath
if ($Mode -eq 'Restore') {
    if (-not $BackupPath) { throw 'Specify -BackupPath with the saved startup backup.' }
    $backup = Import-Clixml -LiteralPath $BackupPath
    if ($backup.RegistryPath -ne $runPath) { throw 'Unexpected registry path in backup.' }
    foreach ($entry in $backup.Entries) {
        if (-not (Test-SelectedStartupName $entry.Name) -or $entry.Kind -ne 'String' -or $entry.Value -isnot [string]) {
            throw 'Unexpected startup entry in backup.'
        }
    }
    foreach ($entry in $backup.Entries) {
        if ($runKey.GetValueNames() -contains $entry.Name) {
            if ($runKey.GetValue($entry.Name) -eq $entry.Value) { Write-Output "Already restored: $($entry.Name)"; continue }
            Write-Warning "Skipped $($entry.Name): a different entry now exists."
            continue
        }
        New-ItemProperty -LiteralPath $runPath -Name $entry.Name -Value $entry.Value -PropertyType String | Out-Null
        if ($runKey.GetValue($entry.Name) -ne $entry.Value) { throw "Restore verification failed: $($entry.Name)" }
        Write-Output "Restored: $($entry.Name)"
    }
    return
}
$entries = @($runKey.GetValueNames() | Where-Object { Test-SelectedStartupName $_ } | ForEach-Object {
    if ($runKey.GetValueKind($_).ToString() -ne 'String') { throw "Unsupported registry value type: $_" }
    [pscustomobject]@{ Name = $_; Kind = 'String'; Value = $runKey.GetValue($_) }
})
if ($Mode -eq 'Inspect') {
    $entries | Select-Object Name, Kind
    return
}
if ($entries.Count -eq 0) { Write-Output 'No selected startup entries remain.'; return }
$backupDirectory = Join-Path $PSScriptRoot 'cleanup-backups'
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
$BackupPath = Join-Path $backupDirectory ('startup-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '.clixml')
$backup = [pscustomobject]@{ RegistryPath = $runPath; Entries = $entries }
$backup | Export-Clixml -LiteralPath $BackupPath
$saved = Import-Clixml -LiteralPath $BackupPath
if ($saved.Entries.Count -ne $entries.Count) { throw 'Backup verification failed; nothing changed.' }
for ($i = 0; $i -lt $entries.Count; $i++) {
    if ($saved.Entries[$i].Name -ne $entries[$i].Name -or $saved.Entries[$i].Value -cne $entries[$i].Value) {
        throw 'Backup round-trip verification failed; nothing changed.'
    }
}
Write-Output "Backup: $BackupPath"
$removed = @()
try {
    foreach ($entry in $entries) {
        if ($runKey.GetValue($entry.Name) -cne $entry.Value) { throw "Entry changed during cleanup: $($entry.Name)" }
        Remove-ItemProperty -LiteralPath $runPath -Name $entry.Name
        $removed += $entry
        if ($runKey.GetValueNames() -contains $entry.Name) { throw "Removal verification failed: $($entry.Name)" }
        Write-Output "Disabled automatic startup: $($entry.Name)"
    }
}
catch {
    foreach ($entry in $removed) {
        if ($runKey.GetValueNames() -notcontains $entry.Name) {
            New-ItemProperty -LiteralPath $runPath -Name $entry.Name -Value $entry.Value -PropertyType String | Out-Null
        }
    }
    throw
}
Write-Output 'Verified. Installed apps and currently running processes were not changed. These startup entries may be recreated by app updates or settings.'
