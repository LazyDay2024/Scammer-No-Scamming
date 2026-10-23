# Builds Scammer_No_Scamming_Pitch.pptx (editable text over the key art, fade transitions, auto timings).
# Run export_pptx_data.py first. Needs PowerPoint installed.
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$slides = Get-Content -Raw -Encoding UTF8 (Join-Path $here 'pptx_slides.json') | ConvertFrom-Json
$out = Join-Path $here 'Scammer_No_Scamming_Pitch.pptx'

function Rgb($hex) { $r = [Convert]::ToInt32($hex.Substring(0,2),16); $g = [Convert]::ToInt32($hex.Substring(2,2),16); $b = [Convert]::ToInt32($hex.Substring(4,2),16); return $r + 256*$g + 65536*$b }

function AddText($slide, $text, $left, $top, $width, $size, $color, $bold) {
    $box = $slide.Shapes.AddTextbox(1, $left, $top, $width, 40)
    $tf = $box.TextFrame
    $tf.WordWrap = -1
    $tf.AutoSize = 1
    $tf.MarginLeft = 0; $tf.MarginRight = 0; $tf.MarginTop = 0; $tf.MarginBottom = 0
    $tf.TextRange.Text = $text
    # TextFrame2 sets the complex-script (Thai) font too; the old TextRange only styles Latin text.
    $f = $box.TextFrame2.TextRange.Font
    $f.Name = 'Leelawadee UI'
    $f.NameComplexScript = 'Leelawadee UI'
    $f.NameFarEast = 'Leelawadee UI'
    $f.Size = [single]$size
    $f.Bold = [int]$bold
    $f.Fill.ForeColor.RGB = (Rgb $color)
    return $box
}

$app = New-Object -ComObject PowerPoint.Application
$pres = $app.Presentations.Add(0)
$pres.PageSetup.SlideWidth = 960
$pres.PageSetup.SlideHeight = 540

$index = 1
foreach ($s in $slides) {
    $slide = $pres.Slides.Add($index, 12)  # blank layout
    $null = $slide.Shapes.AddPicture($s.background, 0, -1, 0, 0, 960, 540)

    # kicker label
    $k = AddText $slide $s.kicker 65 62 400 13 '11191D' -1
    $k.Fill.Visible = -1; $k.Fill.ForeColor.RGB = (Rgb 'E5B96E')
    $k.TextFrame.MarginLeft = 9; $k.TextFrame.MarginRight = 9; $k.TextFrame.MarginTop = 3; $k.TextFrame.MarginBottom = 2
    $k.TextFrame.WordWrap = 0

    $titleSize = 54
    $titleColor = 'FFF4DC'
    if ($s.layout -eq 'title') { $titleSize = 64 }
    if ($s.layout -eq 'stat') { $titleSize = 80; $titleColor = 'E5B96E' }
    $t = AddText $slide $s.title 65 92 560 $titleSize $titleColor -1
    $y = $t.Top + $t.Height + 14

    $bar = $slide.Shapes.AddShape(1, 65, $y, 60, 5)
    $bar.Fill.ForeColor.RGB = (Rgb 'D97843'); $bar.Line.Visible = 0
    $y = $y + 20

    $lineSize = 22
    if ($s.layout -eq 'title') { $lineSize = 20 }
    foreach ($l in $s.lines) {
        $size = $lineSize
        $color = 'EDE2C8'
        if (($s.layout -eq 'title' -or $s.layout -eq 'stat') -and $l -eq $s.lines[0]) { $size = 30; $color = 'E5B96E'; if ($s.layout -eq 'stat') { $color = 'FFF4DC' } }
        $box = AddText $slide $l 65 $y 520 $size $color 0
        $y = $box.Top + $box.Height + 8
    }

    if ($s.source) { $null = AddText $slide $s.source 65 448 560 11 'C8B894' 0 }

    # caption bar
    $cap = AddText $slide $s.caption 120 490 720 18 'FFFFFF' 0
    $cap.TextFrame.TextRange.ParagraphFormat.Alignment = 2
    $cap.Fill.Visible = -1; $cap.Fill.ForeColor.RGB = (Rgb '090E11'); $cap.Fill.Transparency = 0.22
    $cap.TextFrame.MarginLeft = 12; $cap.TextFrame.MarginRight = 12; $cap.TextFrame.MarginTop = 5; $cap.TextFrame.MarginBottom = 5
    $cap.Top = 515 - $cap.Height

    $slide.NotesPage.Shapes.Placeholders(2).TextFrame.TextRange.Text = $s.caption
    $slide.SlideShowTransition.EntryEffect = 1793   # ppEffectFade
    $slide.SlideShowTransition.AdvanceOnTime = -1
    $slide.SlideShowTransition.AdvanceTime = $s.seconds
    $index++
}

$pres.SaveAs($out)
$pres.Close()
$app.Quit()
Write-Output $out
