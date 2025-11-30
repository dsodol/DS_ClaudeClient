Add-Type -AssemblyName System.Drawing

$pngPath = "C:\Users\dsodo\project\built_with_ai\DS_ClaudeClient\DS_ClaudeClient_32x32.png"
$icoPath = "C:\Users\dsodo\project\built_with_ai\DS_ClaudeClient\DS_ClaudeClient\Resources\app.ico"

$png = [System.Drawing.Image]::FromFile($pngPath)
$bmp = New-Object System.Drawing.Bitmap $png
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
$icon.Dispose()
$bmp.Dispose()
$png.Dispose()

Write-Host "Icon created successfully at $icoPath"
