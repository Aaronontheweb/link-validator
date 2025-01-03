. "$PSScriptRoot\scripts\getReleaseNotes.ps1"
. "$PSScriptRoot\scripts\bumpVersion.ps1"

######################################################################
# Step 1: Grab release notes and update solution metadata
######################################################################
$releaseNotes = Get-ReleaseNotes -MarkdownFile (Join-Path -Path $PSScriptRoot -ChildPath "RELEASE_NOTES.md")

# inject release notes into Directory.Build.props
$directoryBuildPropsPath = Join-Path -Path $PSScriptRoot -ChildPath "src"
$directoryBuildPropsPath = Join-Path -Path $directoryBuildPropsPath -ChildPath "Directory.Build.props"

UpdateVersionAndReleaseNotes -ReleaseNotesResult $releaseNotes -XmlFilePath $directoryBuildPropsPath

Write-Output "Added release notes $releaseNotes"
