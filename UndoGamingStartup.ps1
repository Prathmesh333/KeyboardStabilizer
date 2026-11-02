param([Parameter(Mandatory = $true)][string]$BackupPath)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'GamingStartup.ps1') -Mode Restore -BackupPath $BackupPath
