Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap 32,32
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::Transparent)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Printer body (blue-grey box)
$bodyBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 70, 130, 180))
$g.FillRectangle($bodyBrush, 3, 11, 26, 13)

# Top dark bar (feed slot)
$darkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 40, 90, 140))
$g.FillRectangle($darkBrush, 3, 11, 26, 4)

# Paper coming out the top
$paperBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$g.FillRectangle($paperBrush, 8, 4, 16, 9)

# Paper coming out the bottom
$g.FillRectangle($paperBrush, 8, 19, 16, 9)

# Lines on bottom paper (text lines)
$lineBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 150, 180, 210))
$g.FillRectangle($lineBrush, 10, 22, 12, 2)
$g.FillRectangle($lineBrush, 10, 26, 8,  2)

# Green indicator light
$greenBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 80, 200, 80))
$g.FillEllipse($greenBrush, 21, 14, 4, 4)

$g.Dispose()

# Save bitmap to PNG bytes in memory
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$pngBytes = $ms.ToArray()
$ms.Dispose()

# Write a valid ICO file (1 image, 32x32, PNG-compressed)
$icoPath = 'C:\Users\root\source\repos\Printer\app.ico'
$ico = New-Object System.IO.FileStream $icoPath, 'Create'
$w   = New-Object System.IO.BinaryWriter $ico

# ICO header
$w.Write([uint16]0)          # reserved
$w.Write([uint16]1)          # type = ICO
$w.Write([uint16]1)          # image count

# Directory entry
$w.Write([byte]32)           # width
$w.Write([byte]32)           # height
$w.Write([byte]0)            # palette count
$w.Write([byte]0)            # reserved
$w.Write([uint16]1)          # color planes
$w.Write([uint16]32)         # bits per pixel
$w.Write([uint32]$pngBytes.Length)
$w.Write([uint32]22)         # offset to image data (6 header + 16 dir entry)

# Image data
$w.Write($pngBytes)

$w.Close()
$ico.Close()

Write-Host "Icon saved to $icoPath"
