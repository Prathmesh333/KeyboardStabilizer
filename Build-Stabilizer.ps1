param(
    [switch]$Test,
    [switch]$NativeSmoke,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin')
)
$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Windows .NET Framework C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$sourcePaths = @('Stabilizer.Engine.cs', 'Stabilizer.Windows.cs', 'Stabilizer.App.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
$applicationPath = Join-Path $OutputDirectory 'KeyboardStabilizer.exe'
& $compilerPath /nologo /target:winexe /main:KeyboardStabilizer.StabilizerProgram /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$applicationPath" @sourcePaths
if ($LASTEXITCODE -ne 0) { throw 'Stabilizer build failed.' }
Write-Output "Built $applicationPath"
if ($Test) {
    $testPath = Join-Path $OutputDirectory 'KeyboardStabilizer.Tests.exe'
    $testSource = Join-Path $PSScriptRoot 'Stabilizer.Tests.cs'
    & $compilerPath /nologo /target:exe /main:KeyboardStabilizer.StabilizerTests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$testPath" @sourcePaths $testSource
    if ($LASTEXITCODE -ne 0) { throw 'Stabilizer test build failed.' }
    & $testPath
    if ($LASTEXITCODE -ne 0) { throw 'Stabilizer tests failed.' }
}
if ($NativeSmoke) {
    $smokePath = Join-Path $OutputDirectory 'KeyboardStabilizer.NativeSmoke.exe'
    $smokeSource = Join-Path $PSScriptRoot 'Stabilizer.NativeSmoke.cs'
    & $compilerPath /nologo /target:exe /main:KeyboardStabilizer.NativeSmoke /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$smokePath" @sourcePaths $smokeSource
    if ($LASTEXITCODE -ne 0) { throw 'Native smoke build failed.' }
    & $smokePath
    if ($LASTEXITCODE -ne 0) { throw 'Native smoke failed. Stop other stabilizer sessions and check that F8 is available.' }
}
