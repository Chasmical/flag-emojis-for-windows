# ┌─────────────────────────────────────────────────────────────────────────┐
# │                                                                         │
# │     You are not meant to run this script file from the repository!      │
# │    It's meant to be processed by a server and accessed from the web.    │
# │                Just run the command below in PowerShell:                │
# │                                                                         │
# │          ┌──────────────────────────────────────────────────┐           │
# │          │    irm https://chsm.dev/get-flag-emojis | iex    │           │
# │          └──────────────────────────────────────────────────┘           │
# │                                                                         │
# └─────────────────────────────────────────────────────────────────────────┘
# See https://github.com/Chasmical/chsm.dev/blob/main/app/(projects)/get-flag-emojis/route.ts
# <THIS-NOTICE-WILL-BE-REMOVED-BY-THE-HOST-WEBSITE>

$ErrorActionPreference = "Stop"

$thisScript = "irm https://chsm.dev/get-flag-emojis | iex"
# $thisScript = "Get-Content -Raw D:\repos\flag-emojis-for-windows\scripts\install-script.ps1 | iex"

# If not running as admin, re-run the script as admin
$admin = [System.Security.Principal.WindowsIdentity]::GetCurrent().Groups -contains "S-1-5-32-544"
if (-not $admin) {
    $pwsh = "pwsh.exe"
    try { $null = Get-Command "pwsh" } catch { $pwsh = "powershell.exe" }

    Write-Host "The PowerShell is not elevated. Restarting as admin..." -ForegroundColor DarkYellow

    Start-Process -FilePath $pwsh -Verb RunAs -ArgumentList @(
        "-NoProfile", "-ExecutionPolicy", "Bypass",
        "-Command", "`$WaitForHost=`$true; $thisScript"
    )
    throw "The PowerShell is not elevated. Restarting as admin..."
}



function FormatBytes($bytes, $format = "{0:N1} {1}") {
    if ($bytes -le 0) { return "0 B" }
    $units = "B", "KiB", "MiB", "GiB", "TiB", "PiB"
    $scale = [Math]::Floor([Math]::Log($bytes, 1024))
    $format -f ($bytes / [Math]::Pow(1024, $scale)), $units[$scale]
}
function DownloadFile($url, $target) {
    try {
        $request = [System.Net.HttpWebRequest]::Create($url)
        $request.Timeout = 15000 # 15s

        $response = $request.GetResponse()
        $totalLength = $response.ContentLength
        $responseStream = $response.GetResponseStream()
        $targetStream = [System.IO.File]::Create($target)

        # 1 MB buffer should be good for a 13 MB font file
        $buffer = [byte[]]::new(1MB)
        $read = $responseStream.Read($buffer, 0, $buffer.Length)
        $totalRead = $read
        $activity = "Downloading $($url.Split('/')[-1])"

        while ($read -gt 0) {
            $targetStream.Write($buffer, 0, $read)
            $read = $responseStream.Read($buffer, 0, $buffer.Length)
            $totalRead += $read

            $progress = @{
                Activity        = $activity
                Status          = "Downloaded ($(FormatBytes $totalRead) of $(FormatBytes $totalLength)):"
                # If Content-Length header was not available, then pass -1 for unknown progress
                PercentComplete = (-1, ($totalRead / [float]$totalLength * 100))[$totalLength -gt 0]
            }
            Write-Progress @progress
        }
        Write-Progress -Activity $activity -Completed
    }
    finally {
        if ($responseStream) { $responseStream.Dispose() }
        if ($targetStream) { $targetStream.Dispose() }
    }
}



# Get the currently installed font file for Segoe UI Emoji
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"
$regName = "Segoe UI Emoji (TrueType)"
$oldFontName = Get-ItemPropertyValue $regPath -Name $regName
$oldFontPath = "C:\Windows\Fonts\$oldFontName"

$fontName = "Segoe.UI.Emoji.with.Twemoji.Flags.ttf"
$fontNameNoExt = $fontName.Replace(".ttf", "")

$zipDownloadUrl = "https://github.com/Chasmical/flag-emojis-for-windows/releases/latest/download/Segoe.UI.Emoji.with.Twemoji.Flags.zip"
$zipArchiveName = $zipDownloadUrl.Split('/')[-1]
$zipArchivePath = Join-Path $env:TEMP $zipArchiveName
$tempFontPath = Join-Path $env:TEMP $fontName



$shouldInstall = $true
$installedSuccessfully = $false

# If it's obviously our modified font, then compare the hashes
if ($oldFontName.StartsWith($fontNameNoExt)) {
    $oldHash = (Get-FileHash $oldFontPath -Algorithm SHA256).Hash
    # See https://github.com/Chasmical/chsm.dev/blob/main/app/(projects)/get-flag-emojis/route.ts
    $newHash = "<THE-LATEST-HASH-WILL-BE-INSERTED-HERE-BY-THE-HOST-WEBSITE>"

    if ($oldHash -eq $newHash) {
        Write-Host "`nAll good! You've already got the latest $fontName.`n" -ForegroundColor Green
        $shouldInstall = $false
    }
}

# Wrapped in a try-finally to delete temporary files
try {
    if ($shouldInstall) {
        # Download the archive to a temporary location
        try {
            DownloadFile $zipDownloadUrl $zipArchivePath
            Write-Host "Successfully downloaded $zipArchiveName."
        }
        catch {
            Write-Error "Failed to download the file from $($zipDownloadUrl):" -ErrorAction Continue
            throw $_
        }

        # Extract the font file from the ZIP archive
        try {
            Write-Host "Extracting $fontName from the ZIP archive..."
            Expand-Archive -Path $zipArchivePath -DestinationPath $env:TEMP -Force
            # The font file should have been extracted to $tempFontPath
        }
        catch {
            Write-Error "Failed to extract the font file from the ZIP archive:" -ErrorAction Continue
            throw $_
        }

        # Find a filename that's not taken (usually should be unmodified or _0, if old files are cleaned up okay)
        try {
            $newFontPath = "C:\Windows\Fonts\$fontName"
            $counter = 0
            while (Test-Path $newFontPath) {
                $newFontPath = "C:\Windows\Fonts\{0}_{1:X}.ttf" -f $fontNameNoExt, $counter
                $counter++
            }
            $newFontName = Split-Path $newFontPath -Leaf
        }
        catch {
            Write-Error "Failed to find an available filename???:" -ErrorAction Continue
            throw $_
        }

        # Copy the downloaded font file to the specified path
        # (It has to be copied specifically. I don't know why, but a moved font file just doesn't work!?)
        try {
            Write-Host "Copying $newFontName to C:\Windows\Fonts..."
            Copy-Item -Path $tempFontPath -Destination $newFontPath -Force
        }
        catch {
            Write-Error "Failed to move the file to $($newFontPath)???:" -ErrorAction Continue
            throw $_
        }

        # Define the FontApi class with some methods needed for proper registration of the font
        $fontApiSource = @"
using System;
using System.Runtime.InteropServices;

public static class FontApi {
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    public static extern int AddFontResourceExW(string lpFileName, uint fl, IntPtr pdv);
    [DllImport("user32.dll")]
    public static extern int SendMessageW(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
"@
        Add-Type -TypeDefinition $fontApiSource

        # Call AddFontResourceExW with 0x10 (FR_PRIVATE) flags
        # (I have no idea why this is needed or how it works, but it works)
        Write-Host "Registering the font through the Windows GDI API..."
        $errcode = [FontApi]::AddFontResourceExW($newFontPath, 0x10, [IntPtr]::Zero)
        if ($errcode -eq 0) {
            Write-Error "Failed to register the font through the Windows GDI API: $errcode" -ErrorAction Stop
        }

        # Set Segoe UI Emoji's filename in the font registry to the new one
        try {
            Write-Host "Writing to Segoe UI Emoji's font registry entry..."
            $null = New-ItemProperty -Path $regPath -Name $regName -Value $newFontName -PropertyType String -Force
        }
        catch {
            Write-Error "Failed to modify Segoe UI Emoji's font registry entry:" -ErrorAction Continue
            throw $_
        }

        # Call SendMessageW with 0xFFFF (all top-level windows), 0x001D (WM_FONTCHANGE)
        Write-Host "Notifying applications of a font change through Windows API..."
        $errcode = [FontApi]::SendMessageW([IntPtr]0xFFFF, 0x001D, [IntPtr]::Zero, [IntPtr]::Zero)
        if ($errcode -eq 0) {
            Write-Error "Failed to notify applications of a font change through Windows API: $errcode" -ErrorAction Continue
        }

        Write-Host "`nThe $fontName was installed successfully!" -ForegroundColor Green
        Write-Host "Restart Windows to apply the changes everywhere (you can do it later)`n" -ForegroundColor DarkGreen
        $installedSuccessfully = $true
    }
}
catch {
    Write-Error $_ -ErrorAction Continue
    if ($WaitForHost) { Write-Host "Press any key to exit..."; [System.Console]::ReadKey() }
    exit 1
}
finally {
    # Delete temporary files (downloaded zip, extracted font; will always run)
    foreach ($temp in ($zipArchivePath, $tempFontPath)) {
        try {
            if (Test-Path $temp) { Remove-Item $temp -Force }
        }
        catch {
            Write-Error "An error occured when deleting the temporary file $($temp):`n$_" -ErrorAction Continue
        }
    }
}



# Remove old installations (will always run, so this script can also be used for cleanup)
try {
    # Find all files like "Font.ttf", "Font_1.ttf" and "Font (2).ttf"
    $files = Get-ChildItem "C:\Windows\Fonts\$($fontNameNoExt)*"

    foreach ($file in $files) {
        # Don't delete the previously installed font and the one we just installed
        if ($file.Name -in ($oldFontName, $newFontName)) { continue }

        try {
            Remove-Item $file -Force
            Write-Host "Cleaned up old $($file.Name)."
        }
        catch {
            Write-Error "Failed to clean up old $($file.Name):`n$_" -ErrorAction Continue
        }
    }
}
catch {
    Write-Error "An error occured when deleting old font files:`n$_" -ErrorAction Continue
}

# Attempt to remove the old version of this font (might fail, which is okay)
try {
    # Careful, don't delete the original seguiemj.ttf!
    if ($installedSuccessfully -and $oldFontName.StartsWith($fontNameNoExt)) {
        Remove-Item $oldFontPath -Force
        Write-Host "Cleaned up old $oldFontName."
    }
} catch {
}



if ($WaitForHost) { Write-Host "Press any key to exit..."; [System.Console]::ReadKey() }
