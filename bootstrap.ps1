# bootstrap.ps1
Add-Type -AssemblyName System.Windows.Forms

# Helper: Folder browser dialog with address bar (using OpenFileDialog technique)
function Select-Folder([string]$description, [string]$initialDirectoryInput = $null) {
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = $description

    $effectiveInitialDirectory = $null
    # Prefer specific initial directory if provided and valid
    if ($null -ne $initialDirectoryInput -and [System.IO.Directory]::Exists($initialDirectoryInput)) {
        $effectiveInitialDirectory = $initialDirectoryInput
    } elseif ($PSScriptRoot -and [System.IO.Directory]::Exists($PSScriptRoot)) {
        # Default to script's own directory (which is the project root in this case)
        $effectiveInitialDirectory = $PSScriptRoot
    } else {
        # Ultimate fallback to Desktop
        $effectiveInitialDirectory = [Environment]::GetFolderPath("Desktop")
    }
    $dialog.InitialDirectory = $effectiveInitialDirectory
    
    $dialog.FileName = "Select Folder" 
    $dialog.Filter = "Folders|*.this.directory" 
    $dialog.CheckFileExists = $false      
    $dialog.CheckPathExists = $true       
    $dialog.ValidateNames = $false        

    $ownerWindow = New-Object System.Windows.Forms.NativeWindow 
    $result = $dialog.ShowDialog($ownerWindow)

    if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
        $selectedPathCandidate = $dialog.FileName
        if ([System.IO.Directory]::Exists($selectedPathCandidate)) {
            return $selectedPathCandidate 
        } else {
            $folderPath = Split-Path -Path $selectedPathCandidate 
            if ($null -ne $folderPath -and [System.IO.Directory]::Exists($folderPath)) {
                return $folderPath
            } else {
                Write-Warning "Could not resolve selected path to an existing folder. Path: '$selectedPathCandidate', Resolved folder: '$folderPath'"
                Write-Host "Operation cancelled due to invalid path selection." -ForegroundColor Yellow
                Exit 1 
            }
        }
    } else {
        Write-Host "Operation cancelled by user." -ForegroundColor Yellow
        Exit 1 
    }
}

Write-Host "=== ScheduleOne Project Setup ===" -ForegroundColor Cyan

# --- The Template Project Path IS the script's current directory ---
$templateDir = $PSScriptRoot
If (-not $templateDir -or -not (Test-Path $templateDir -PathType Container)) {
    # Fallback if $PSScriptRoot is not available or invalid
    $templateDir = Get-Location | Select-Object -ExpandProperty Path
    Write-Warning "PSScriptRoot not available or invalid. Assuming current working directory IS the template project: $templateDir"
}

# Verify essential Unity project folders exist to confirm it's likely a Unity project
if (-not (Test-Path (Join-Path $templateDir "Assets")) -or -not (Test-Path (Join-Path $templateDir "ProjectSettings"))) {
    Write-Error "FATAL: This script appears to NOT be running from inside a Unity project folder (missing 'Assets' or 'ProjectSettings')."
    Write-Error "Please run this script from the root of your 'ScheduleOne_UnityProject' folder."
    Exit 1
}
Write-Host "Template project (current script directory) confirmed: $templateDir" -ForegroundColor Green
# --- End of Template Project Path ---

# Check for default game installation path
$defaultGameInstallPath = "" 
$programFilesX86 = ${env:ProgramFiles(x86)}
if ($null -ne $programFilesX86) {
    $potentialGamePath = Join-Path -Path $programFilesX86 -ChildPath "Steam\steamapps\common\Schedule I"
    if (Test-Path (Join-Path -Path $potentialGamePath -ChildPath "Schedule I.exe")) {
        Write-Host "Detected potential game installation at: $potentialGamePath" -ForegroundColor Green
        $defaultGameInstallPath = $potentialGamePath
    } else {
        Write-Host "Default game installation path for 'Schedule I' not found or 'Schedule I.exe' is missing there." -ForegroundColor Yellow
    }
} else {
    Write-Warning "Could not determine Program Files (x86) directory."
}

# Prompt for remaining paths
# For $exportedProj, start the dialog one level up from the template/script directory for convenience
$parentOfTemplateDir = Split-Path $templateDir
$gameInstallPath = Select-Folder "Select the folder where your game 'Schedule I' is installed" $defaultGameInstallPath
$exportedProj     = Select-Folder "Select the AssetRipper 'ExportedProject' folder for Schedule I" $parentOfTemplateDir 

# Create an exclusion file for xcopy
$exclFile = Join-Path $env:TEMP "exclude.txt"
@"
\Scripts\
\Plugins\
\Texture2D\
\Sprite\
\Mesh\
"@ | Out-File -Encoding ASCII -FilePath $exclFile

Write-Host "`nCopying exported Assets..." -ForegroundColor Cyan
Write-Host "Attempting to copy from '$exportedProj\Assets\*' to (Join-Path $templateDir 'Assets')\*"
Write-Host "Excluding folders: Scripts, Plugins, Texture2D, Sprite, Mesh"
if (Test-Path $exclFile) {
    cmd.exe /c echo N \| xcopy /D /E /I /Y /EXCLUDE:"$exclFile" "$exportedProj\Assets\*" "$(Join-Path $templateDir 'Assets')\"
} else {
    Write-Warning "Exclusion file '$exclFile' not found. xcopy will not exclude files."
    cmd.exe /c echo N \| xcopy /D /E /I /Y "$exportedProj\Assets\*" "$(Join-Path $templateDir 'Assets')\"
}

# Run your original DropManagedFolderHere.bat
Write-Host "`nRunning DropManagedFolderHere.bat to configure managed assemblies..." -ForegroundColor Cyan
$dropBat = Join-Path $templateDir "Assets\Plugins\ScheduleOne\DropManagedFolderHere.bat" # Path relative to $templateDir

if (-not (Test-Path $dropBat)) {
    Write-Warning "DropManagedFolderHere.bat not found at '$dropBat'. Please ensure it exists in your template project at that subpath."
} else {
    $managedPathForBat = Join-Path $gameInstallPath "Schedule I_Data\Managed" 
    
    if (-not (Test-Path $managedPathForBat -PathType Container)) {
         Write-Warning "The game's Managed folder was not found at '$managedPathForBat'. Batch script execution might fail or do nothing."
    }
    Write-Host "Executing batch script, providing it the game's Managed folder path: '$managedPathForBat'"
    $batchCommand = "call `"$dropBat`" `"$managedPathForBat`""
    cmd.exe /c "$batchCommand < NUL"
}

# Launch Unity, open project, and (implicitly via editor script) execute method
Write-Host "`nLaunching Unity editor for the current project..." -ForegroundColor Cyan
Write-Host "The Unity editor will open this project ('$templateDir')."
Write-Host "Once Unity opens and compiles, 'ScheduleOneAutomation.FixShadersInAll' should run automatically (if configured via RunAutomationOnLoad.cs)."
Write-Host "Check the Unity Console window (Ctrl+Shift+C or Window > General > Console) for logs or errors."

# --- ENSURE THIS IS YOUR CORRECT UNITY VERSION AND PATH ---
$unityExe = "C:\Program Files\Unity\Hub\Editor\2022.3.32f1\Editor\Unity.exe" 

if (Test-Path $unityExe) {
    Write-Host "Attempting to launch Unity from: $unityExe"
    
    $unityArgs = @(
        "-projectPath", "`"$templateDir`"" 
    )
    
    Write-Host "Unity arguments: $unityArgs" 
    & $unityExe $unityArgs
    
} else {
    Write-Warning "Unity executable not found at '$unityExe'. Please update the path in the script."
}

Write-Host "`nPowerShell script has launched Unity. The project '$templateDir' should be open." -ForegroundColor Green
Write-Host "You may need to close Unity manually."