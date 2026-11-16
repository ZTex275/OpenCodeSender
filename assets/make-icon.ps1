# Draws the extension icon (used for the VSIX manifest and the marketplace listing).
# Output: assets/icon-128.png, assets/icon-256.png, assets/preview-200.png
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

Add-Type -AssemblyName System.Drawing

function New-RoundedPath {
    param([float]$X, [float]$Y, [float]$Width, [float]$Height, [float]$Radius)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2
    $path.AddArc($X, $Y, $d, $d, 180, 90)
    $path.AddArc($X + $Width - $d, $Y, $d, $d, 270, 90)
    $path.AddArc($X + $Width - $d, $Y + $Height - $d, $d, $d, 0, 90)
    $path.AddArc($X, $Y + $Height - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$Size = 512)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = [float]$Size / 512.0

    # Tile with a diagonal indigo -> violet gradient.
    $tile = New-RoundedPath (14 * $s) (14 * $s) (484 * $s) (484 * $s) (104 * $s)
    $rect = New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)
    $c1 = [System.Drawing.Color]::FromArgb(255, 30, 42, 90)
    $c2 = [System.Drawing.Color]::FromArgb(255, 107, 78, 255)
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 45.0)
    $g.FillPath($gradient, $tile)

    # Soft highlight fading from the top of the tile.
    $highlight = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)),
        [System.Drawing.Color]::FromArgb(42, 255, 255, 255),
        [System.Drawing.Color]::FromArgb(0, 255, 255, 255),
        90.0)
    $g.FillPath($highlight, $tile)

    # Speech bubble.
    $bubble = New-RoundedPath (78 * $s) (112 * $s) (356 * $s) (252 * $s) (72 * $s)
    $tail = New-Object System.Drawing.Drawing2D.GraphicsPath
    $tail.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(130 * $s, 344 * $s),
        [System.Drawing.PointF]::new(126 * $s, 428 * $s),
        [System.Drawing.PointF]::new(224 * $s, 352 * $s)
    ))
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $g.FillPath($white, $tail)
    $g.FillPath($white, $bubble)

    # Code brackets inside the bubble.
    $font = New-Object System.Drawing.Font("Consolas", (138 * $s), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $textBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.RectangleF((100 * $s), (150 * $s), (320 * $s), (200 * $s))),
        [System.Drawing.Color]::FromArgb(255, 45, 45, 140),
        [System.Drawing.Color]::FromArgb(255, 96, 70, 235),
        35.0)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    $bubbleCenter = New-Object System.Drawing.RectangleF(0, (0 * $s), $Size, $Size)
    $g.DrawString("</>", $font, $textBrush, $bubbleCenter, $format)

    $format.Dispose()
    $textBrush.Dispose()
    $font.Dispose()
    $white.Dispose()
    $tail.Dispose()
    $bubble.Dispose()
    $highlight.Dispose()
    $gradient.Dispose()
    $tile.Dispose()
    $g.Dispose()

    return $bitmap
}

function New-CommandIconBitmap {
    param([int]$Size = 16)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = [float]$Size / 16.0

    $tile = New-RoundedPath 0 0 ([float]$Size) ([float]$Size) (3.5 * $s)
    $rect = New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 40, 50, 120),
        [System.Drawing.Color]::FromArgb(255, 107, 78, 255),
        45.0)
    $g.FillPath($gradient, $tile)

    # "Send" arrow.
    $arrow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $arrow.AddPolygon([System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new(2.0 * $s, 2.0 * $s),
        [System.Drawing.PointF]::new(14.5 * $s, 8.0 * $s),
        [System.Drawing.PointF]::new(2.0 * $s, 14.0 * $s),
        [System.Drawing.PointF]::new(2.0 * $s, 9.4 * $s),
        [System.Drawing.PointF]::new(8.6 * $s, 8.0 * $s),
        [System.Drawing.PointF]::new(2.0 * $s, 6.6 * $s)
    ))
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillPath($white, $arrow)

    $white.Dispose()
    $arrow.Dispose()
    $gradient.Dispose()
    $tile.Dispose()
    $g.Dispose()

    return $bitmap
}

function Save-Resized {
    param([System.Drawing.Bitmap]$Source, [int]$Size, [string]$Path)

    $target = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($target)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.DrawImage($Source, (New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)))
    $graphics.Dispose()

    $target.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $target.Dispose()
}

$master = New-IconBitmap -Size 1024
$assets = Join-Path $Root "assets"
New-Item -ItemType Directory -Force -Path $assets | Out-Null

Save-Resized -Source $master -Size 256 -Path (Join-Path $assets "icon-256.png")
Save-Resized -Source $master -Size 128 -Path (Join-Path $assets "icon-128.png")
Save-Resized -Source $master -Size 200 -Path (Join-Path $assets "preview-200.png")
$master.Dispose()

$command = New-CommandIconBitmap -Size 16
$command.Save((Join-Path $assets "command-16.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$command.Dispose()

Write-Output ("created: " + (Join-Path $assets "icon-128.png"))
