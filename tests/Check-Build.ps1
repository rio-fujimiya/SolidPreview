# No CLI, registry changes or shell process termination. Windows PowerShell x64.
$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitProcess) { throw '64-bit PowerShell is required.' }
$root = Split-Path -Parent $PSScriptRoot
foreach ($script in (Get-ChildItem -LiteralPath $root -Filter '*.ps1' -Recurse)) {
    $tokens = $null; $errors = $null
    [void][System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$errors)
    if ($errors.Count) { throw ("PowerShell parse error in " + $script.Name + ': ' + ($errors -join '; ')) }
}
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$windowsBase = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll'
if (-not (Test-Path -LiteralPath $csc)) { throw 'Framework C# compiler not found.' }
$tmp = Join-Path $env:TEMP ('SolidPreview-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmp | Out-Null
try {
    $dll = Join-Path $tmp 'SolidPreview.dll'
    & $csc /nologo /target:library /platform:x64 /optimize+ "/out:$dll" /r:System.Drawing.dll /r:System.Web.Extensions.dll "/r:$windowsBase" (Join-Path $root 'Handler.cs')
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $dll)) { throw 'C# compile failed.' }
    # Load bytes so the test does not lock its temporary DLL file.
    $assembly = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($dll))
    $formats = Get-Content -Raw -LiteralPath (Join-Path $root 'formats.json') | ConvertFrom-Json
    $seen = @{}; $extensions = @{}
    foreach ($format in $formats) {
        if ($format.ext -notmatch '^\.[a-z0-9]+$' -or $extensions.ContainsKey($format.ext)) { throw 'Invalid or duplicate extension.' }
        $extensions[$format.ext] = $true
        foreach ($kind in @('preview','thumb')) {
            $className = $format.($kind + 'Class')
            $guid = [guid]$format.$kind
            if ($seen.ContainsKey($guid)) { throw "Duplicate COM GUID: $guid" }
            $seen[$guid] = $true
            $type = $assembly.GetType($className, $true)
            if ($type.GUID -ne $guid -or -not $type.IsPublic -or $type.IsAbstract) { throw "Invalid COM type: $className" }
            $expected = if ($kind -eq 'preview') {'SolidPreview.IPreviewHandler'} else {'SolidPreview.IThumbnailProvider'}
            if (-not $type.GetInterface($expected) -or -not $type.GetInterface('SolidPreview.IInitializeWithStream')) { throw "Missing interface: $className" }
        }
    }
    if ($extensions.Count -eq 0) { throw 'No format metadata found.' }
    Write-Host "PASS source build and COM metadata ($($extensions.Count) extensions, $($seen.Count) classes)."
    # Exercise the same pure camera math used for both CLI and MCP renderers.
    function Assert-Near([double]$Actual,[double]$Expected,[string]$Name) {
        if ([double]::IsNaN($Actual) -or [Math]::Abs($Actual-$Expected) -gt 0.00000001) { throw "Camera test failed: $Name ($Actual != $Expected)" }
    }
    $frame = [SolidPreview.CameraFrame]::Parse('{"bbox":{"min":[10,20,30],"max":[30,50,70]}}')
    $standard = $frame.ToJson(0.0,0.0,1.0) | ConvertFrom-Json
    $zoomed = $frame.ToJson(0.0,0.0,1.0,2.0,0.0,0.0,0.0) | ConvertFrom-Json
    $diag = [Math]::Sqrt(2900.0)
    Assert-Near $standard.target[0] 20 'standard center X'
    Assert-Near $standard.target[1] 35 'standard center Y'
    Assert-Near $standard.target[2] 50 'standard center Z'
    Assert-Near $zoomed.distance ($standard.distance/2) 'zoom scale'
    Assert-Near ([SolidPreview.CameraNavigation]::WheelZoom(1.0,120)) 1.2 'wheel in'
    Assert-Near ([SolidPreview.CameraNavigation]::WheelZoom(1.2,-120)) 1.0 'wheel out'
    Assert-Near ([SolidPreview.CameraNavigation]::WheelZoom(12.0,120)) 12.0 'upper zoom bound'
    Assert-Near ([SolidPreview.CameraNavigation]::WheelZoom(0.2,-120)) 0.2 'lower zoom bound'
    foreach ($aspect in @(0.5,1.0,2.0)) {
        $height = 400; $width = [int]($height*$aspect)
        foreach ($yaw in @(-45.0,0.0,90.0,180.0)) {
            foreach ($pitch in @(-90.0,-35.264389682754654,0.0,35.264389682754654,90.0)) {
                $a = $yaw*[Math]::PI/180; $b = $pitch*[Math]::PI/180
                $d = [SolidPreview.CameraNavigation]::PanDelta($yaw,$pitch,2.0,$width,$height,60,-40)
                $scale = [SolidPreview.CameraNavigation]::DistanceScale($aspect)*0.8/2/$height
                $right = $d[0]*[Math]::Cos($a)-$d[1]*[Math]::Sin($a)
                $up = $d[0]*[Math]::Sin($a)*[Math]::Sin($b)+$d[1]*[Math]::Cos($a)*[Math]::Sin($b)+$d[2]*[Math]::Cos($b)
                Assert-Near $right (-60*$scale) 'screen pan X'
                Assert-Near $up (-40*$scale) 'screen pan Y'
                $pan = $frame.ToJson($a,$b,$aspect,2.0,$d[0],$d[1],$d[2]) | ConvertFrom-Json
                $orbit = $frame.ToJson(($a+0.7),($b-0.1),$aspect,2.0,$d[0],$d[1],$d[2]) | ConvertFrom-Json
                Assert-Near $pan.distance $orbit.distance 'orbit preserves zoom'
                for ($i=0; $i -lt 3; $i++) {
                    Assert-Near $pan.target[$i] ($standard.target[$i]+$diag*$d[$i]) 'world pan target'
                    Assert-Near $pan.target[$i] $orbit.target[$i] 'orbit preserves target'
                }
                $reset = $frame.ToJson($a,$b,$aspect) | ConvertFrom-Json
                for ($i=0; $i -lt 3; $i++) { Assert-Near $reset.target[$i] $standard.target[$i] 'reset center' }
                Assert-Near $reset.distance ($pan.distance*2) 'reset zoom'
            }
        }
    }
    Write-Host 'PASS camera zoom, screen-space pan, orbit preservation and reset math (60 orientations/aspects).'

} finally { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }
