[CmdletBinding()]
param(
    [string] $Output = (Join-Path $PSScriptRoot '../../build/windows'),
    [string] $ToolchainBin,
    [string] $GitBash = (Join-Path $env:ProgramFiles 'Git/bin/bash.exe')
)

$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
$standard = Join-Path $framework 'netstandard.dll'
if (!(Test-Path $compiler) -or !(Test-Path $standard)) {
    throw 'Se necesita Windows x64 con .NET Framework 4.8.'
}
if (!(Test-Path -LiteralPath $GitBash)) { throw 'Indica la ruta de Git Bash con -GitBash.' }
if ($ToolchainBin) { $ToolchainBin = (Resolve-Path -LiteralPath $ToolchainBin).Path }
$savedPath = $env:Path
$savedNativeDestination = $env:WUPSTREAM_NATIVE_DEST

$temp = Join-Path ([IO.Path]::GetTempPath()) ('WupPad-' + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $package = Join-Path $temp 'vigem.zip'
    $url = 'https://api.nuget.org/v3-flatcontainer/nefarius.vigem.client/1.21.256/nefarius.vigem.client.1.21.256.nupkg'
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $package
    $hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
    if ($hash -ne 'FF460031B33E0882CD1166FE2D072D0FE97578480C61ED85DCCFA6C930FDB55E') {
        throw 'El paquete de ViGEm no coincide con la suma SHA-256 esperada.'
    }
    Expand-Archive -LiteralPath $package -DestinationPath $temp
    $dll = Join-Path $temp 'lib\netstandard2.0\Nefarius.ViGEm.Client.dll'
    $destination = (New-Item -ItemType Directory -Path $Output -Force).FullName
    if ($ToolchainBin) { $env:Path = "$ToolchainBin;$env:Path" }
    if (!(Get-Command gcc.exe -ErrorAction SilentlyContinue) -or !(Get-Command make.exe -ErrorAction SilentlyContinue)) {
        throw 'Añade MinGW-w64 y Make al PATH o indica su carpeta bin con -ToolchainBin.'
    }
    $env:WUPSTREAM_NATIVE_DEST = Join-Path $destination 'WupStreamNative.dll'
    $native = (Join-Path $PSScriptRoot 'build-native.sh').Replace('\','/')
    & $GitBash $native
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el motor nativo de WUP Stream.' }

    $exe = Join-Path $destination 'Generic Wupstream Program.exe'
    $files = (Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs').FullName
    $icon = Join-Path $PSScriptRoot '../assets/app.ico'
    & $compiler /nologo /codepage:65001 /warnaserror /optimize+ /target:winexe /platform:x64 "/win32icon:$icon" "/r:$standard" "/r:$dll" "/out:$exe" $files
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la aplicación.' }
    Copy-Item -LiteralPath $dll -Destination $destination
}
finally {
    $env:Path = $savedPath
    $env:WUPSTREAM_NATIVE_DEST = $savedNativeDestination
    # The directory is created above with a unique absolute path.
    $resolved = (Resolve-Path -LiteralPath $temp).Path
    if ($resolved -eq $temp) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}
