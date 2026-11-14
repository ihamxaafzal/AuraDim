$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$workspaceRoot = Split-Path -Parent $root
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wv2Net = Join-Path $workspaceRoot "wallper-win\lib\wv2\lib\net462"
$wv2Native = Join-Path $workspaceRoot "wallper-win\lib\wv2\build\native\x64\WebView2Loader.dll"

$distDir = Join-Path $root "dist"
$appDir = Join-Path $distDir "LumaDeck"
$uiDest = Join-Path $appDir "ui"
$icoFile = Join-Path $root "src\assets\lumadeck.ico"

if (-not (Test-Path $icoFile)) {
  & (Join-Path $root "make_icon.ps1")
}

if (Test-Path $appDir) { Remove-Item -Recurse -Force $appDir }
New-Item -ItemType Directory -Force -Path $uiDest | Out-Null

# Copy WebView2 dependencies + UI + Icon
Copy-Item (Join-Path $wv2Net "Microsoft.Web.WebView2.Core.dll") $appDir
Copy-Item (Join-Path $wv2Net "Microsoft.Web.WebView2.WinForms.dll") $appDir
Copy-Item $wv2Native $appDir
Copy-Item (Join-Path $root "src\ui\index.html") $uiDest
Copy-Item $icoFile $appDir

$outExe = Join-Path $appDir "LumaDeck.exe"
$refCore = Join-Path $wv2Net "Microsoft.Web.WebView2.Core.dll"
$refWinForms = Join-Path $wv2Net "Microsoft.Web.WebView2.WinForms.dll"
$progCs = Join-Path $root "src\Program.cs"
$instCs = Join-Path $root "src\Installer.cs"

Write-Host "Compiling LumaDeck.exe (with embedded logo icon)..."
& $csc /nologo /target:winexe /platform:x64 /optimize+ `
  "/win32icon:$icoFile" `
  "/out:$outExe" `
  /r:System.dll `
  /r:System.Core.dll `
  /r:System.Drawing.dll `
  /r:System.Windows.Forms.dll `
  /r:System.Management.dll `
  /r:System.Web.Extensions.dll `
  "/r:$refCore" `
  "/r:$refWinForms" `
  "$progCs"

if ($LASTEXITCODE -ne 0) { throw "LumaDeck.exe compilation failed!" }

# Create payload zip
$payloadZip = Join-Path $distDir "LumaDeckPayload.zip"
if (Test-Path $payloadZip) { Remove-Item -Force $payloadZip }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($appDir, $payloadZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Write-Host "Compiling LumaDeckSetup.exe (with embedded logo icon)..."
$setupDist = Join-Path $distDir "LumaDeckSetup.exe"
$setupRoot = Join-Path $workspaceRoot "LumaDeckSetup.exe"

& $csc /nologo /target:winexe /platform:x64 /optimize+ `
  "/win32icon:$icoFile" `
  "/out:$setupDist" `
  "/resource:$payloadZip,LumaDeckPayload.zip" `
  /r:System.dll `
  /r:System.Core.dll `
  /r:System.Drawing.dll `
  /r:System.Windows.Forms.dll `
  /r:System.IO.Compression.dll `
  /r:System.IO.Compression.FileSystem.dll `
  /r:Microsoft.CSharp.dll `
  "$instCs"

if ($LASTEXITCODE -ne 0) { throw "LumaDeckSetup.exe compilation failed!" }

Copy-Item -Force $setupDist $setupRoot
Write-Host "Build succeeded!"
Write-Host "  Portable App : $outExe"
Write-Host "  Setup Wizard : $setupRoot"
