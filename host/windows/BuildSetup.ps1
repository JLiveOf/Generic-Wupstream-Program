[CmdletBinding()]
param(
    [string] $Bin = (Join-Path $PSScriptRoot '../../build/windows'),
    [string] $Output = (Join-Path $PSScriptRoot '../../build/windows/Generic-Wupstream-Program-Setup-win-x64.exe'),
    [string] $MakeNsis = 'makensis.exe'
)

$ErrorActionPreference = 'Stop'
$binPath = (Resolve-Path -LiteralPath $Bin).Path
$installer = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
$script = Join-Path $PSScriptRoot 'Setup.nsi'

& $MakeNsis '/INPUTCHARSET' 'UTF8' "/DWUP_BIN=$binPath" "/DWUP_INSTALLER=$installer" $script
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar el instalador de Windows.' }
