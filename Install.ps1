# Windows PowerShell 5.1, x64. Run as the same non-elevated user as Explorer.
[CmdletBinding()]
param([string]$Cli, [string[]]$Extensions = @('.stl','.stp','.step','.obj','.3mf','.sc','.solidcraft'), [string[]]$SampleFiles = @(), [switch]$PreviewOnly, [switch]$ThumbnailOnly, [switch]$StaticPreview)
$ErrorActionPreference = 'Stop'
if ($PreviewOnly -and $ThumbnailOnly) { throw 'Choose just one diagnostic mode.' }
if (-not [Environment]::Is64BitProcess) { throw 'Use 64-bit Windows PowerShell.' }
$root = Join-Path $env:LOCALAPPDATA 'SolidPreview'
$state = Join-Path $root 'restore.json'
if (Test-Path $state) { throw 'Already installed. Run Update-SolidPreview.bat.' }
if (-not $Cli) {
    $p = Get-Process solidcraft -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p -and $p.Path) { $Cli = Join-Path (Split-Path $p.Path) 'solidcraft-cli.exe' }
}
if (-not $Cli -or -not (Test-Path -LiteralPath $Cli -PathType Leaf)) {
    $c = Get-Command solidcraft-cli.exe -ErrorAction SilentlyContinue
    if ($c) { $Cli = $c.Source }
}
if (-not $Cli -or -not (Test-Path -LiteralPath $Cli -PathType Leaf)) { $Cli = (Read-Host 'Full path to solidcraft-cli.exe').Trim('"') }
$Cli = (Resolve-Path -LiteralPath $Cli).ProviderPath
if (-not (Test-Path -LiteralPath $Cli -PathType Leaf)) { throw 'CLI not found.' }
& $Cli --version
if ($LASTEXITCODE -ne 0) { throw 'CLI startup failed.' }
$all = Get-Content -Raw (Join-Path $PSScriptRoot 'formats.json') | ConvertFrom-Json
$wanted = @(foreach ($format in $all) {
    if ($Extensions -contains $format.ext) { $format }
})
if (-not $wanted.Count) { throw 'No supported extensions requested.' }
New-Item -ItemType Directory -Path $root -Force | Out-Null
$versionDir = Join-Path $root 'v1.0.18'
New-Item -ItemType Directory -Path $versionDir -Force | Out-Null
$dll = Join-Path $versionDir 'SolidPreview.dll'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:library /platform:x64 /optimize+ "/out:$dll" /r:System.Drawing.dll /r:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'Handler.cs')
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $dll)) { throw 'C# compile failed. Registry unchanged.' }
[void][Reflection.Assembly]::LoadFrom($dll)
$test = Join-Path $env:TEMP ('SolidPreview-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $test | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Invoke-Cli([string[]]$Arguments, [string]$CaptureFile) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $Cli
    $psi.Arguments = (($Arguments | ForEach-Object { '"' + ($_ -replace '"','\"') + '"' }) -join ' ')
    $psi.UseShellExecute = $false; $psi.CreateNoWindow = $true; $psi.WorkingDirectory = $test
    $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $p = [Diagnostics.Process]::Start($psi)
    try {
        $stdout = $p.StandardOutput.ReadToEndAsync(); $stderr = $p.StandardError.ReadToEndAsync()
        if (-not $p.WaitForExit(20000)) { $p.Kill(); $p.WaitForExit(); return $false }
        if ($p.ExitCode -ne 0) { Write-Host $stderr.Result -ForegroundColor DarkYellow; return $false }
        if ($CaptureFile) { [IO.File]::WriteAllText($CaptureFile,$stdout.Result,$utf8) }
        return $true
    } finally { $p.Dispose() }
}
$good = @()
$orbit = $false
$orbitFormats = @()
$liveFormats = @()
try {
    $box = Join-Path $test 'box.json'
    [IO.File]::WriteAllText($box,'{"commands":[{"command":"solid.box","params":{"length":20,"width":10,"height":5}}]}',$utf8)
    if (-not $ThumbnailOnly) {
        $sized = Join-Path $test 'portrait.png'
        if (-not (Invoke-Cli @('snapshot',$box,'--out',$sized,'--width','320','--height','480','--no-grid','--no-axes'))) { throw 'Adaptive preview size test failed.' }
        Add-Type -AssemblyName System.Drawing
        $im = New-Object Drawing.Bitmap($sized)
        try { if ($im.Width -ne 320 -or $im.Height -ne 480) { throw 'Adaptive preview size mismatch.' } } finally { $im.Dispose() }
        Write-Host 'PASS adaptive preview size 320x480' -ForegroundColor Green
    }
    if (-not $ThumbnailOnly -and -not $StaticPreview) {
        # Obtain the camera centre/scale from the same eval parser used by the handler.
        $a = Join-Path $test 'orbit-a.png'; $b = Join-Path $test 'orbit-b.png'
        $measure = Join-Path $test 'orbit-measure.json'
        $frame = $null
        if (Invoke-Cli @('eval',$box) $measure) {
            try { $frame = [SolidPreview.CameraFrame]::Parse([IO.File]::ReadAllText($measure)) }
            catch { Write-Warning ("Orbit bounds test failed: " + $_.Exception.Message) }
        }
        if ($frame) {
        $argsA = @('snapshot',$box,'--out',$a,'--no-grid','--no-axes','--camera',$frame.ToJson(0.1,0.2))
        $argsB = @('snapshot',$box,'--out',$b,'--no-grid','--no-axes','--camera',$frame.ToJson(1.1,-0.4))
        if ((Invoke-Cli $argsA) -and (Invoke-Cli $argsB) -and (Test-Path $a) -and (Test-Path $b)) {
            Add-Type -AssemblyName System.Drawing
            $ia = $null; $ib = $null
            try {
                $ia = New-Object Drawing.Bitmap($a); $ib = New-Object Drawing.Bitmap($b)
                # Require visible content and a different pixel pattern, rather than just different PNG metadata.
                $different = 0; $contentA = 0; $contentB = 0
                if ($ia.Width -eq $ib.Width -and $ia.Height -eq $ib.Height) {
                    $bgA = $ia.GetPixel(0,0).ToArgb(); $bgB = $ib.GetPixel(0,0).ToArgb()
                    for ($y=0; $y -lt $ia.Height; $y+=8) { for ($x=0; $x -lt $ia.Width; $x+=8) {
                        $pa=$ia.GetPixel($x,$y).ToArgb(); $pb=$ib.GetPixel($x,$y).ToArgb()
                        if ($pa -ne $bgA) { $contentA++ }; if ($pb -ne $bgB) { $contentB++ }; if ($pa -ne $pb) { $different++ }
                    }}
                }
                $orbit = $different -gt 20 -and $contentA -gt 20 -and $contentB -gt 20
            } catch { Write-Warning $_ } finally { if($ia){$ia.Dispose()}; if($ib){$ib.Dispose()} }
        }
        }
        if ($orbit) { Write-Host 'PASS orbit camera' -ForegroundColor Green }
        else { Write-Warning 'Orbit camera test failed; installing static preview instead.' }
    }
    foreach ($f in $wanted) {
        $ext = $f.ext; $model = Join-Path $test ('model'+$ext)
        $sample = $SampleFiles | Where-Object { [IO.Path]::GetExtension($_) -ieq $ext } | Select-Object -First 1
        if ($sample) { Copy-Item -LiteralPath $sample -Destination $model }
        elseif ($ext -in '.sc','.solidcraft') { if (-not (Invoke-Cli @('run',$box,'--save',$model))) { continue } }
        elseif ($ext -in '.igs','.iges') { Write-Warning "Provide -SampleFiles for $ext"; continue }
        else {
            $export = if ($ext -eq '.stp') { Join-Path $test 'fixture.step' } else { $model }
            if (-not (Invoke-Cli @('run',$box,'--out',$export))) { continue }
            if ($export -ne $model) { Copy-Item -LiteralPath $export -Destination $model }
        }
        if (-not (Test-Path -LiteralPath $model)) { continue }
        $inputFile = $model
        if ($ext -in '.stl','.obj','.3mf','.igs','.iges') {
            $inputFile = Join-Path $test ('import'+$ext+'.json')
            $command = if ($ext -in '.igs','.iges') { 'doc.open' } else { 'file.insert_mesh' }
            $json = @{commands=@(@{command=$command;params=@{path=$model}})} | ConvertTo-Json -Depth 8 -Compress
            [IO.File]::WriteAllText($inputFile,$json,$utf8)
        }
        $png = Join-Path $test ('shot'+$ext+'.png')
        if ((Invoke-Cli @('snapshot',$inputFile,'--out',$png,'--no-grid','--no-axes')) -and (Test-Path -LiteralPath $png)) {
            Add-Type -AssemblyName System.Drawing
            try { $image = [Drawing.Image]::FromFile($png); $image.Dispose(); $good += $f; Write-Host "PASS $ext" -ForegroundColor Green } catch { Write-Warning "Invalid PNG: $ext" }
        } else { Write-Warning "Skipping $ext (render test failed)" }
        if ($orbit -and ($good -contains $f)) {
            $measure = Join-Path $test ('measure'+$ext+'.json')
            $rotated = Join-Path $test ('orbit'+$ext+'.png')
            $im = $null
            try {
                if (-not (Invoke-Cli @('eval',$inputFile) $measure)) { throw 'eval failed' }
                $frame = [SolidPreview.CameraFrame]::Parse([IO.File]::ReadAllText($measure))
                $camera = $frame.ToJson(1.1,-0.4)
                if (-not (Invoke-Cli @('snapshot',$inputFile,'--out',$rotated,'--no-grid','--no-axes','--camera',$camera)) -or -not (Test-Path $rotated)) { throw 'camera snapshot failed' }
                $im = New-Object Drawing.Bitmap($rotated)
                $bg = $im.GetPixel(0,0).ToArgb(); $content = 0
                for ($y=0; $y -lt $im.Height; $y+=8) { for ($x=0; $x -lt $im.Width; $x+=8) {
                    if ($im.GetPixel($x,$y).ToArgb() -ne $bg) { $content++ }
                }}
                if ($content -le 20) { throw 'camera image appears empty' }
                $orbitFormats += $ext
                Write-Host "PASS orbit $ext" -ForegroundColor Green
                try {
                    $ms = [SolidPreview.LiveProbe]::Test($Cli,$inputFile,$test,$frame.ToJson(0.1,0.2),$frame.ToJson(1.1,-0.4))
                    $liveFormats += $ext
                    Write-Host "PASS live $ext (2 frames: ${ms}ms)" -ForegroundColor Green
                } catch { Write-Warning ("Live MCP unavailable for $ext : " + $_.Exception.Message + '; using continuous snapshot fallback.') }
            } catch { Write-Warning ("Static preview only for $ext : " + $_.Exception.Message) }
            finally { if ($im) { $im.Dispose() } }
        }

    }
} finally { Remove-Item -LiteralPath $test -Recurse -Force -ErrorAction SilentlyContinue }
if (-not $good.Count) { throw 'No formats passed the CLI smoke test. Registry unchanged.' }
$cu = [Microsoft.Win32.Registry]::CurrentUser
$backup = New-Object System.Collections.ArrayList
$appid = '{FAE94A3B-12E6-48B3-936E-A8C32E70B6F5}'
$previewType = '{8895B1C6-B41F-4C1C-A562-0D564250836F}'
$thumbType = '{E357FCCD-A995-4576-B01F-234630154E96}'
function Set-Recorded([string]$Path,[string]$Name,[string]$Value) {
    $k = $cu.OpenSubKey($Path)
    $exists = $false; $old = $null; $kind = 'String'
    if ($k) { try { $exists = $k.GetValueNames() -contains $Name; if ($exists) { $old = $k.GetValue($Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames); $kind = $k.GetValueKind($Name).ToString() } } finally { $k.Dispose() } }
    [void]$backup.Add([pscustomobject]@{path=$Path;name=$Name;existed=$exists;old=$old;kind=$kind;installed=$Value})
    # Save the rollback journal before every individual change.
    [IO.File]::WriteAllText($state, (ConvertTo-Json -InputObject @($backup.ToArray()) -Depth 8), $utf8)
    $k = $cu.CreateSubKey($Path); try { $k.SetValue($Name,$Value,[Microsoft.Win32.RegistryValueKind]::String) } finally { $k.Dispose() }
}
function Register-Class($id,$class,[bool]$preview) {
    $base = "Software\Classes\CLSID\$id"
    Set-Recorded $base '' $class
    if ($preview) { Set-Recorded $base 'AppID' $appid }
    $inproc = "$base\InprocServer32"
    Set-Recorded $inproc '' 'mscoree.dll'
    Set-Recorded $inproc 'ThreadingModel' 'Apartment'
    Set-Recorded $inproc 'Class' $class
    Set-Recorded $inproc 'Assembly' 'SolidPreview, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
    Set-Recorded $inproc 'RuntimeVersion' 'v4.0.30319'
    Set-Recorded $inproc 'CodeBase' ([uri]::new($dll).AbsoluteUri)
}
try {
    Set-Recorded 'Software\SolidPreview' 'Cli' $Cli
    Set-Recorded 'Software\SolidPreview' 'Orbit' $(if ($orbitFormats.Count) {'1'} else {'0'})
    Set-Recorded 'Software\SolidPreview' 'OrbitFormats' ($orbitFormats -join ';')
    Set-Recorded 'Software\SolidPreview' 'LiveFormats' ($liveFormats -join ';')
    Set-Recorded "Software\Classes\AppID\$appid" '' 'SolidCraft Preview Host'
    Set-Recorded "Software\Classes\AppID\$appid" 'DllSurrogate' 'Prevhost.exe'
    foreach ($f in $good) {
        if (-not $ThumbnailOnly) {
            Register-Class $f.preview $f.previewClass $true
            Set-Recorded 'Software\Microsoft\Windows\CurrentVersion\PreviewHandlers' $f.preview 'SolidCraft Preview'
        }
        if (-not $PreviewOnly) { Register-Class $f.thumb $f.thumbClass $false }
        # Direct extension and stable SystemFileAssociations; do not change defaults or UserChoice.
        foreach ($base in @("Software\Classes\$($f.ext)","Software\Classes\SystemFileAssociations\$($f.ext)")) {
            if (-not $ThumbnailOnly) { Set-Recorded "$base\shellex\$previewType" '' $f.preview }
            if (-not $PreviewOnly) { Set-Recorded "$base\shellex\$thumbType" '' $f.thumb }
        }
    }
} catch { Write-Warning 'Registration incomplete. Run Uninstall.ps1 to roll back.'; throw }
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class ShellNotify { [DllImport("shell32.dll")] public static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b); }'
[ShellNotify]::SHChangeNotify(0x08000000,0,[IntPtr]::Zero,[IntPtr]::Zero)
Write-Host ('Registered: '+ (($good | ForEach-Object ext) -join ', ')) -ForegroundColor Green
Write-Host 'Installed handler v1.0.18. Restart Explorer and prevhost if the previous version is still loaded. Use Alt+P and Large icons.'
if ($orbitFormats.Count) { Write-Host ('Orbit enabled for: '+($orbitFormats -join ', ')); Write-Host 'Preview: left/right drag to orbit, wheel to zoom, middle drag to pan; ViewCube navigation resets framing.' }

if ($liveFormats.Count) { Write-Host ('Live MCP enabled for: '+($liveFormats -join ', ')) }
