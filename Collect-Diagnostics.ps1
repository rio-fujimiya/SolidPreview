# Read-only diagnostics. Run after selecting a CAD file with Alt+P enabled.
$ErrorActionPreference = 'Continue'
Write-Host '=== SolidPreview settings ==='
Get-ItemProperty -Path 'HKCU:\Software\SolidPreview' -ErrorAction SilentlyContinue |
    Select-Object Cli,Orbit,OrbitFormats | Format-List
$previewType = '{8895B1C6-B41F-4C1C-A562-0D564250836F}'
$cu = [Microsoft.Win32.Registry]::CurrentUser
$cr = [Microsoft.Win32.Registry]::ClassesRoot
Write-Host '=== Effective preview registrations ==='
foreach ($ext in @('.stp','.step','.stl','.obj','.3mf','.3dm','.sc','.solidcraft','.igs','.iges')) {
    foreach ($base in @($ext,"SystemFileAssociations\$ext")) {
        $k = $cr.OpenSubKey("$base\shellex\$previewType")
        if (-not $k) { continue }
        try { $id = $k.GetValue('') } finally { $k.Dispose() }
        if (-not $id) { continue }
        $c = $cr.OpenSubKey("CLSID\$id\InprocServer32")
        $code = if ($c) { try { $c.GetValue('CodeBase') } finally { $c.Dispose() } }
        Write-Host "$base : $id : $code"
    }
}
Write-Host '=== Preview host processes ==='
Get-Process prevhost,explorer -ErrorAction SilentlyContinue | Select-Object ProcessName,Id,StartTime | Format-Table -AutoSize
$cache = Join-Path (Split-Path $env:LOCALAPPDATA) 'LocalLow\SolidPreview\cache-v1'
Write-Host '=== Most recent host logs ==='
Get-ChildItem -LiteralPath $cache -Filter 'host-*.log' -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 6 | ForEach-Object {
        Write-Host "--- $($_.Name) $($_.LastWriteTime) ---"
        Get-Content -LiteralPath $_.FullName -Encoding UTF8 -Tail 80
    }
