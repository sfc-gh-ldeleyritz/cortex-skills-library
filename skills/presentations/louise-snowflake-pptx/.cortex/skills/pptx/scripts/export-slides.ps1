# export-slides.ps1 -- Export PPTX slides to JPEG images via PowerPoint COM
# Usage: powershell -ExecutionPolicy Bypass -File .cortex\skills\pptx\scripts\export-slides.ps1 -PptxPath output.pptx -OutDir temp\qa
param(
    [Parameter(Mandatory)][string]$PptxPath,
    [Parameter(Mandatory)][Alias('OutputDir')][string]$OutDir
)
$ErrorActionPreference = 'Stop'

# Ensure output directory exists
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# Kill any existing PowerPoint instances to avoid COM startup conflicts
Get-Process powerpnt -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

$pptx   = (Resolve-Path $PptxPath).Path
$outDir = (New-Item -ItemType Directory -Force -Path $OutDir).FullName

$ppt = New-Object -ComObject PowerPoint.Application
try {
    $ppt.Visible = 1  # Use integer 1, not enum (avoids HRESULT 0x80048240)
    Start-Sleep -Seconds 1
    $pres = $ppt.Presentations.Open($pptx)
    Start-Sleep -Seconds 1

    # Export only visible (non-hidden) slides individually
    $slideNum = 0
    foreach ($slide in $pres.Slides) {
        # Skip hidden slides (template/layout slides that shouldn't be exported)
        if ($slide.SlideShowTransition.Hidden -eq $true) { continue }
        $slideNum++
        $fileName = "slide-{0:D2}.jpg" -f $slideNum
        $slide.Export((Join-Path $outDir $fileName), 'JPG', 1280, 720)
        Write-Host "  $fileName"
    }

    $pres.Close()
    Write-Host "Exported $slideNum slides to: $outDir"
} finally {
    $ppt.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($ppt) | Out-Null
}
