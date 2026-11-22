[CmdletBinding()]
param([switch]$ClearCache)
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:LOCALAPPDATA 'SolidPreview'
$state = Join-Path $root 'restore.json'
if (Test-Path -LiteralPath $state) {
    $decoded = Get-Content -Raw -LiteralPath $state | ConvertFrom-Json
    $entries = @(foreach ($entry in $decoded) { $entry })
    [array]::Reverse($entries)
    $cu = [Microsoft.Win32.Registry]::CurrentUser
    foreach ($e in $entries) {
        $k = $cu.OpenSubKey($e.path,$true)
        if (-not $k) { continue }
        try {
            # Do not overwrite changes made after this installation.
            if ($k.GetValue($e.name,$null) -ne $e.installed) { continue }
            if ($e.existed) {
                $kind = [Microsoft.Win32.RegistryValueKind]$e.kind; $v = $e.old
                switch ($e.kind) { 'Binary' {$v=[byte[]]$v}; 'MultiString' {$v=[string[]]$v}; 'DWord' {$v=[int]$v}; 'QWord' {$v=[long]$v} }
                $k.SetValue($e.name,$v,$kind)
            } else { $k.DeleteValue($e.name,$false) }
            $empty = ($k.ValueCount -eq 0 -and $k.SubKeyCount -eq 0)
        } finally { $k.Dispose() }
        if ($empty) { $cu.DeleteSubKey($e.path,$false) }
    }
    Remove-Item -LiteralPath $state -Force
}
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class ShellNotify { [DllImport("shell32.dll")] public static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b); }'
[ShellNotify]::SHChangeNotify(0x08000000,0,[IntPtr]::Zero,[IntPtr]::Zero)
if ($ClearCache) { Remove-Item -LiteralPath (Join-Path (Split-Path $env:LOCALAPPDATA) 'LocalLow\SolidPreview\cache-v1') -Recurse -Force -ErrorAction SilentlyContinue }
Write-Host 'Registry restored. Sign out/in; then remove %LOCALAPPDATA%\SolidPreview if desired.'
