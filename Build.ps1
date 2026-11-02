param(
    [switch]$Test,
    [ValidatePattern('^[\w.-]+\.exe$')][string]$OutputName = 'KeyboardCheck.exe',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin')
)
$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'The built-in .NET Framework C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$sourcePath = Join-Path $PSScriptRoot 'KeyboardCheck.cs'
$applicationPath = Join-Path $OutputDirectory $OutputName
& $compilerPath /nologo /target:winexe /main:KeyboardCheck.Program /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$applicationPath" $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output "Built $applicationPath"
if ($Test) {
    $testPath = Join-Path $OutputDirectory 'KeyboardCheck.Tests.exe'
    & $compilerPath /nologo /target:exe /main:KeyboardCheck.Tests /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$testPath" $sourcePath
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & $testPath
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
