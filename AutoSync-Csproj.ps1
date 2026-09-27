# ============================================================
# AutoSync-Csproj.ps1
# Watches the TourFYP project directory for new .cs and .cshtml
# files and automatically adds them to TourwebsiteFYP.csproj.
#
# HOW TO USE:
#   Run this script ONCE when you open Visual Studio.
#   Leave the PowerShell window open in the background.
#   Every time Antigravity creates a new file, it will be added
#   to the .csproj automatically within 2 seconds.
#   When Visual Studio detects the .csproj change it will show
#   "Reload All" — click it and the file appears instantly.
# ============================================================

$projectRoot = "d:\My websites\TourFYP\TourFYP"
$csprojPath  = "$projectRoot\TourwebsiteFYP.csproj"

# ── Helper: add a single file path to the .csproj if not present ─────────
function Add-FileToCsproj {
    param([string]$absoluteFilePath)

    # Derive the relative path (relative to the .csproj folder)
    $relativePath = $absoluteFilePath.Substring($projectRoot.Length).TrimStart('\')

    # Skip files inside obj / bin / .git folders
    if ($relativePath -match '^(obj|bin|\.git|\.vs)\\') { return }

    # Load the project XML
    [xml]$proj = Get-Content -Raw $csprojPath
    $nsUri     = $proj.Project.NamespaceURI

    $ext = [System.IO.Path]::GetExtension($absoluteFilePath).ToLower()

    if ($ext -eq ".cs") {
        # Check if already included as a Compile item
        $exists = $proj.Project.ItemGroup.Compile |
                  Where-Object { $_.Include -eq $relativePath }
        if ($exists) {
            Write-Host "[AutoSync] Already in project: $relativePath" -ForegroundColor Gray
            return
        }

        # Find or create an ItemGroup and append the Compile element
        $ig = $proj.CreateElement('ItemGroup', $nsUri)
        $proj.Project.AppendChild($ig) | Out-Null
        $el = $proj.CreateElement('Compile', $nsUri)
        $el.SetAttribute('Include', $relativePath)
        $ig.AppendChild($el) | Out-Null

        $proj.Save($csprojPath)
        Write-Host "[AutoSync] Added to Compile: $relativePath" -ForegroundColor Green
    }
    elseif ($ext -in @(".cshtml", ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".json", ".html")) {
        # Check if already included as a Content item
        $exists = $proj.Project.ItemGroup.Content |
                  Where-Object { $_.Include -eq $relativePath }
        if ($exists) {
            Write-Host "[AutoSync] Already in project: $relativePath" -ForegroundColor Gray
            return
        }

        $ig = $proj.CreateElement('ItemGroup', $nsUri)
        $proj.Project.AppendChild($ig) | Out-Null
        $el = $proj.CreateElement('Content', $nsUri)
        $el.SetAttribute('Include', $relativePath)
        $ig.AppendChild($el) | Out-Null

        $proj.Save($csprojPath)
        Write-Host "[AutoSync] Added to Content: $relativePath" -ForegroundColor Cyan
    }
    else {
        Write-Host "[AutoSync] Skipped (unrecognized extension): $relativePath" -ForegroundColor Gray
    }
}

# ── Helper: remove a single file path from the .csproj if present ────────
function Remove-FileFromCsproj {
    param([string]$absoluteFilePath)

    $relativePath = $absoluteFilePath.Substring($projectRoot.Length).TrimStart('\')
    
    if ($relativePath -match '^(obj|bin|\.git|\.vs)\\') { return }

    [xml]$proj = Get-Content -Raw $csprojPath
    $nsUri     = $proj.Project.NamespaceURI

    $ext = [System.IO.Path]::GetExtension($absoluteFilePath).ToLower()
    $removed = $false

    if ($ext -eq ".cs") {
        $nodes = $proj.Project.ItemGroup.Compile | Where-Object { $_.Include -eq $relativePath }
        foreach ($node in $nodes) {
            $node.ParentNode.RemoveChild($node) | Out-Null
            $removed = $true
        }
    }
    elseif ($ext -in @(".cshtml", ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".json", ".html")) {
        $nodes = $proj.Project.ItemGroup.Content | Where-Object { $_.Include -eq $relativePath }
        foreach ($node in $nodes) {
            $node.ParentNode.RemoveChild($node) | Out-Null
            $removed = $true
        }
    }

    if ($removed) {
        $proj.Save($csprojPath)
        Write-Host "[AutoSync] Removed from project: $relativePath" -ForegroundColor Magenta
    }
}

# ── Set up the FileSystemWatcher ──────────────────────────────────────────
$watcher                     = New-Object System.IO.FileSystemWatcher
$watcher.Path                = $projectRoot
$watcher.IncludeSubdirectories = $true
$watcher.NotifyFilter        = [System.IO.NotifyFilters]::FileName
$watcher.Filter              = "*.*"
$watcher.EnableRaisingEvents = $true

Write-Host ""
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "  NEXVOY .csproj Auto-Sync is RUNNING"    -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Yellow
Write-Host "  Watching: $projectRoot"
Write-Host "  Project : $csprojPath"
Write-Host "  Press Ctrl+C to stop."
Write-Host "-----------------------------------------"
Write-Host ""

# Event handler: fires when a new file is created
$onCreated = Register-ObjectEvent $watcher Created -Action {
    $filePath = $Event.SourceEventArgs.FullPath
    Start-Sleep -Milliseconds 800
    try { Add-FileToCsproj -absoluteFilePath $filePath }
    catch { Write-Host "[AutoSync] ERROR processing $filePath : $_" -ForegroundColor Red }
}

# Event handler: fires when a file is deleted
$onDeleted = Register-ObjectEvent $watcher Deleted -Action {
    $filePath = $Event.SourceEventArgs.FullPath
    Start-Sleep -Milliseconds 800
    try { Remove-FileFromCsproj -absoluteFilePath $filePath }
    catch { Write-Host "[AutoSync] ERROR processing $filePath : $_" -ForegroundColor Red }
}

# Keep the script alive until the user presses Ctrl+C
try {
    while ($true) { Start-Sleep -Seconds 2 }
}
finally {
    Unregister-Event -SourceIdentifier $onCreated.Name
    Unregister-Event -SourceIdentifier $onDeleted.Name
    $watcher.Dispose()
    Write-Host "`n[AutoSync] Stopped." -ForegroundColor Yellow
}
