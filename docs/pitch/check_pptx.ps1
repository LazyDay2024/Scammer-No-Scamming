# Opens the built deck in PowerPoint, exports each slide to PNG (for checking) and the deck to PDF.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$pptx = Join-Path $here 'Scammer_No_Scamming_Pitch.pptx'
$pngDir = Join-Path $env:TEMP 'snc_pptx_check'
New-Item -ItemType Directory -Force -Path $pngDir | Out-Null
$app = New-Object -ComObject PowerPoint.Application
try {
    $pres = $app.Presentations.Open($pptx, -1, 0, 0)
    foreach ($s in $pres.Slides) { $s.Export((Join-Path $pngDir ("s{0:D2}.png" -f $s.SlideIndex)), 'PNG', 960, 540) }
    $pres.SaveAs((Join-Path $here 'Scammer_No_Scamming_Pitch.pdf'), 32)
    $pres.Close()
} finally { $app.Quit() }
Write-Output $pngDir
