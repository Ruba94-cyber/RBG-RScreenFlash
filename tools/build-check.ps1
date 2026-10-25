# Build the Release x64 executable for CI without signing or packaging it.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'RScreenFlash.csproj'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $msbuild) {
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) { $msbuild = $command.Source }
}
if (-not $msbuild) { throw 'MSBuild was not found. Install Visual Studio Build Tools with the .NET Framework 4.8 targeting pack.' }

& $msbuild $project /t:Rebuild /restore /p:Configuration=Release /p:Platform=x64 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Release x64 build failed (exit $LASTEXITCODE)." }

$exe = Join-Path $root 'bin\x64\Release\RBG - RScreenFlash.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Build completed without the expected executable.' }
$info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($info.FileVersion -ne '1.2.3.0' -or $info.ProductVersion -ne '1.2.3') {
    throw "Unexpected executable version metadata: file=$($info.FileVersion), product=$($info.ProductVersion)"
}
if ($info.CompanyName -ne 'Paolo Rubagotti' -or
    $info.FileDescription -notlike '*Paolo Rubagotti*' -or
    $info.FileDescription -notlike '*paolo.ruba.1913@gmail.com*') {
    throw "Executable author/contact metadata is incomplete: $($info.FileDescription)"
}
Write-Host "Release x64 build OK: $exe"
