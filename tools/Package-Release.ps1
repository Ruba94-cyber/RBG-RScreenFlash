[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$SignerDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'release'
}
if ([string]::IsNullOrWhiteSpace($SignerDirectory)) {
    $SignerDirectory = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'FirmaApp-PaoloRubagotti'
}
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

Write-Host 'Building RScreenFlash Release x64...'
& $msbuild $project /t:Rebuild /restore /p:Configuration=Release /p:Platform=x64 /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Release x64 build failed (exit $LASTEXITCODE)." }

$exe = Join-Path $root 'bin\x64\Release\RBG - RScreenFlash.exe'
if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Release executable is missing: $exe" }
$version = '1.2.2'
$info = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
if ($info.FileVersion -ne '1.2.2.0' -or $info.ProductVersion -ne $version) {
    throw "Unexpected executable version metadata: file=$($info.FileVersion), product=$($info.ProductVersion)"
}
if ($info.CompanyName -ne 'Paolo Rubagotti' -or
    $info.FileDescription -notlike '*Paolo Rubagotti*' -or
    $info.FileDescription -notlike '*paolo.ruba.1913@gmail.com*') {
    throw "Executable author/contact metadata is incomplete: $($info.FileDescription)"
}

$certificatePath = Join-Path $root 'certificates\Paolo-Rubagotti-CodeSigning.cer'
$fingerprintPath = Join-Path $root 'certificates\CERTIFICATE-FINGERPRINT-SHA256.txt'
$signerScript = Join-Path $SignerDirectory 'Firma-file.ps1'
foreach ($required in @($certificatePath, $fingerprintPath, $signerScript)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) { throw "Signing release input is missing: $required" }
}
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
$fingerprintMatch = [regex]::Match((Get-Content -LiteralPath $fingerprintPath -Raw), '(?m)^[ \t]*([A-Fa-f0-9]{64})[ \t\r]*$')
$expectedFingerprint = $fingerprintMatch.Groups[1].Value.ToUpperInvariant()
$actualFingerprint = (Get-FileHash -LiteralPath $certificatePath -Algorithm SHA256).Hash.ToUpperInvariant()
if (-not $expectedFingerprint -or $actualFingerprint -ne $expectedFingerprint) {
    throw 'The bundled public certificate does not match its published SHA-256 fingerprint.'
}
if ($certificate.Subject -notlike '*CN=Paolo Rubagotti*' -or $certificate.Subject -notlike '*paolo.ruba.1913@gmail.com*') {
    throw "Unexpected code-signing certificate subject: $($certificate.Subject)"
}

Write-Host 'Signing the Release executable with the personal Authenticode certificate...'
& $signerScript -FilePath $exe
$signature = Get-AuthenticodeSignature -FilePath $exe
if (-not $signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) {
    throw 'Release executable is not signed by the bundled Paolo Rubagotti certificate.'
}
if (-not $signature.TimeStamperCertificate) { throw 'Release executable is missing its RFC 3161 timestamp.' }
Write-Host "Authenticode signer: $($signature.SignerCertificate.Subject) | trust status on this PC: $($signature.Status)"

$output = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$runId = [Guid]::NewGuid().ToString('N')
$stage = Join-Path $tempRoot ('RBG-RScreenFlash-Release-' + $runId)
$archiveTemp = Join-Path $tempRoot ("RBG-RScreenFlash-$runId.zip")
$archiveName = "RBG-RScreenFlash-$version-windows-x64.zip"
New-Item -ItemType Directory -Path $stage | Out-Null

try {
    Copy-Item -LiteralPath $exe -Destination (Join-Path $stage 'RBG - RScreenFlash.exe')
    foreach ($name in @('README.md', 'CHANGELOG.md', 'LICENSE')) {
        $source = Join-Path $root $name
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Required release file is missing: $source" }
        Copy-Item -LiteralPath $source -Destination (Join-Path $stage $name)
    }
    Copy-Item -LiteralPath $certificatePath -Destination (Join-Path $stage 'Paolo-Rubagotti-CodeSigning.cer')
    Copy-Item -LiteralPath $fingerprintPath -Destination (Join-Path $stage 'CERTIFICATE-FINGERPRINT-SHA256.txt')
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archiveTemp -CompressionLevel Optimal

    $archive = Join-Path $output $archiveName
    Move-Item -LiteralPath $archiveTemp -Destination $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    Set-Content -LiteralPath "$archive.sha256" -Value "$hash  $archiveName" -Encoding ascii
    Write-Host "Package: $archive"
    Write-Host "SHA-256: $hash"
    Write-Host "Executable: $($info.FileDescription) | $($info.CompanyName) | $($info.ProductVersion)"
}
finally {
    $stageFull = [System.IO.Path]::GetFullPath($stage)
    if (-not $stageFull.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove staging path outside the system temp directory: $stageFull"
    }
    if (Test-Path -LiteralPath $stageFull) { Remove-Item -LiteralPath $stageFull -Recurse -Force }
    if (Test-Path -LiteralPath $archiveTemp) { Remove-Item -LiteralPath $archiveTemp -Force }
}
