$ErrorActionPreference = "Stop"

$root      = Split-Path -Parent $MyInvocation.MyCommand.Path
$distApp   = Join-Path $root "dist\AuraDim"
$sdkBin    = Join-Path $root ".tools\sdk\bin\10.0.22621.0\x64"
$makeappx  = Join-Path $sdkBin "makeappx.exe"
$makepri   = Join-Path $sdkBin "makepri.exe"
$edge      = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"

if (!(Test-Path (Join-Path $distApp "AuraDim.exe"))) {
    throw "AuraDim.exe not found in $distApp. Run build.ps1 first."
}
if (!(Test-Path $makeappx) -or !(Test-Path $makepri)) {
    throw "Windows SDK BuildTools (makeappx/makepri) not found in $sdkBin."
}

$stageDir  = Join-Path $root "dist\msix_stage"
$assetsDir = Join-Path $stageDir "Assets"
$tempHtml  = Join-Path $root "dist\msix_html"
$msixOut   = Join-Path $root "dist\AuraDim_3.4.0.0_x64.msix"

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
if (Test-Path $tempHtml) { Remove-Item $tempHtml -Recurse -Force }
if (Test-Path $msixOut)  { Remove-Item $msixOut -Force }

New-Item -ItemType Directory -Force -Path $stageDir, $assetsDir, $tempHtml | Out-Null

Write-Host "[1/4] Copying compiled AuraDim x64 payload..."
Copy-Item -Path "$distApp\*" -Destination $stageDir -Recurse -Force

$logoSvg = @'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="100%" height="100%">
  <defs>
    <linearGradient id="bgGrad" x1="15" y1="15" x2="241" y2="241" gradientUnits="userSpaceOnUse">
      <stop offset="0%" stop-color="#0ea5e9"/>
      <stop offset="55%" stop-color="#2563eb"/>
      <stop offset="100%" stop-color="#4f46e5"/>
    </linearGradient>
    <linearGradient id="deckGrad" x1="74" y1="201" x2="182" y2="219" gradientUnits="userSpaceOnUse">
      <stop offset="0%" stop-color="#ffffff"/>
      <stop offset="100%" stop-color="#e0f2fe"/>
    </linearGradient>
  </defs>
  <rect x="15" y="15" width="226" height="226" rx="61" fill="url(#bgGrad)" stroke="rgba(255,255,255,0.42)" stroke-width="5"/>
  <circle cx="128" cy="128" r="87" fill="rgba(10,16,34,0.22)"/>
  <rect x="74" y="201" width="108" height="18" rx="9" fill="url(#deckGrad)"/>
  <circle cx="128" cy="112" r="34" fill="#ffffff"/>
  <g stroke="#ffffff" stroke-width="14" stroke-linecap="round">
    <line x1="128" y1="64"  x2="128" y2="42"/>
    <line x1="128" y1="160" x2="128" y2="181"/>
    <line x1="80"  y1="112" x2="58"  y2="112"/>
    <line x1="176" y1="112" x2="198" y2="112"/>
    <line x1="94"  y1="78"  x2="78.5"  y2="62.5"/>
    <line x1="162" y1="146" x2="177.5" y2="161.5"/>
    <line x1="162" y1="78"  x2="177.5" y2="62.5"/>
    <line x1="94"  y1="146" x2="78.5"  y2="161.5"/>
  </g>
</svg>
'@

function Render-IconPng($outName, $w, $h, $padPct) {
    $padPx = [Math]::Round([Math]::Min($w, $h) * $padPct)
    $iconSz = [Math]::Min($w, $h) - ($padPx * 2)
    $doc = @"
<!DOCTYPE html>
<html><head><meta charset="utf-8" />
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body {
    width: ${w}px; height: ${h}px; overflow: hidden;
    background: transparent;
    display: flex; align-items: center; justify-content: center;
  }
  .icon { width: ${iconSz}px; height: ${iconSz}px; }
</style></head>
<body><div class="icon">$logoSvg</div></body></html>
"@
    $hPath = Join-Path $tempHtml ($outName + ".html")
    $pPath = Join-Path $assetsDir $outName
    [System.IO.File]::WriteAllText($hPath, $doc, [System.Text.Encoding]::UTF8)
    & $edge --headless=new --disable-gpu --default-background-color=00000000 "--window-size=${w},${h}" --force-device-scale-factor=1 --hide-scrollbars --virtual-time-budget=600 "--screenshot=$pPath" ([System.Uri]$hPath).AbsoluteUri | Out-Null
}

function Render-WideTile($outName, $w, $h) {
    $iconSz = [Math]::Round($h * 0.56)
    $titlePx = [Math]::Round($h * 0.19)
    $subPx = [Math]::Round($h * 0.088)
    $gapPx = [Math]::Round($w * 0.045)
    $doc = @"
<!DOCTYPE html>
<html><head><meta charset="utf-8" />
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  html, body {
    width: ${w}px; height: ${h}px; overflow: hidden;
    font-family: "Segoe UI", system-ui, sans-serif;
    background: linear-gradient(135deg, #070c18 0%, #0e1a33 100%);
    display: flex; align-items: center; justify-content: center; gap: ${gapPx}px;
    color: #ffffff;
  }
  .icon { width: ${iconSz}px; height: ${iconSz}px; flex-shrink: 0; }
  .txt { display: flex; flex-direction: column; }
  .t1 { font-size: ${titlePx}px; font-weight: 800; letter-spacing: -0.5px; line-height: 1.08; }
  .t2 { font-size: ${subPx}px; font-weight: 600; color: #7dd3fc; margin-top: 4px; }
</style></head>
<body>
  <div class="icon">$logoSvg</div>
  <div class="txt">
    <div class="t1">AuraDim</div>
    <div class="t2">Monitor Brightness Control</div>
  </div>
</body></html>
"@
    $hPath = Join-Path $tempHtml ($outName + ".html")
    $pPath = Join-Path $assetsDir $outName
    [System.IO.File]::WriteAllText($hPath, $doc, [System.Text.Encoding]::UTF8)
    & $edge --headless=new --disable-gpu "--window-size=${w},${h}" --force-device-scale-factor=1 --hide-scrollbars --virtual-time-budget=600 "--screenshot=$pPath" ([System.Uri]$hPath).AbsoluteUri | Out-Null
}

Write-Host "[2/4] Rendering Store & Windows Tile PNG assets..."
Render-IconPng "StoreLogo.scale-100.png"                              50   50   0.04
Render-IconPng "StoreLogo.scale-200.png"                             100  100   0.04
Render-IconPng "Square44x44Logo.scale-100.png"                        44   44   0.04
Render-IconPng "Square44x44Logo.scale-200.png"                        88   88   0.04
Render-IconPng "Square44x44Logo.targetsize-48.png"                    48   48   0.04
Render-IconPng "Square44x44Logo.altform-unplated_targetsize-48.png"   48   48   0.04
Render-IconPng "Square44x44Logo.altform-lightunplated_targetsize-48.png" 48 48 0.04
Render-IconPng "Square44x44Logo.targetsize-256.png"                  256  256   0.04
Render-IconPng "Square44x44Logo.altform-unplated_targetsize-256.png" 256  256   0.04
Render-IconPng "Square150x150Logo.scale-100.png"                     150  150   0.14
Render-IconPng "Square150x150Logo.scale-200.png"                     300  300   0.14
Render-WideTile "Wide310x150Logo.scale-100.png"                      310  150
Render-WideTile "Wide310x150Logo.scale-200.png"                      620  300

Write-Host "[3/4] Writing AppxManifest.xml and compiling resources.pri..."
$manifestXml = @'
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="iBlueApps.AuraDim"
    Publisher="CN=A3621FCC-E2BE-437C-8282-BC11897ED32F"
    Version="3.4.0.0"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>AuraDim: Monitor Brightness Control</DisplayName>
    <PublisherDisplayName>iBlue Apps</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.22621.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
  </Resources>

  <Applications>
    <Application Id="AuraDim" Executable="AuraDim.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="AuraDim"
        Description="Precision multi-display brightness, contrast, color warmth, and profile manager for Windows PCs and laptops."
        BackgroundColor="#0b1326"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
'@
$manifestPath = Join-Path $stageDir "AppxManifest.xml"
[System.IO.File]::WriteAllText($manifestPath, $manifestXml, [System.Text.UTF8Encoding]::new($false))

$priConfig = Join-Path $tempHtml "priconfig.xml"
$priOut    = Join-Path $stageDir "resources.pri"
& $makepri createconfig /cf $priConfig /dq en-US /o | Out-Host
if ($LASTEXITCODE -ne 0) { throw "makepri createconfig failed ($LASTEXITCODE)" }

& $makepri new /pr $stageDir /cf $priConfig /mn $manifestPath /of $priOut /o | Out-Host
if ($LASTEXITCODE -ne 0) { throw "makepri new failed ($LASTEXITCODE)" }

Write-Host "[4/4] Packing MSIX via makeappx.exe..."
& $makeappx pack /d $stageDir /p $msixOut /o | Out-Host
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed ($LASTEXITCODE)" }

if (Test-Path $tempHtml) { Remove-Item $tempHtml -Recurse -Force }

$fi = Get-Item $msixOut
Write-Host ""
Write-Host ("SUCCESS: Built " + $fi.FullName + " (" + [Math]::Round($fi.Length / 1KB, 1) + " KB)")
