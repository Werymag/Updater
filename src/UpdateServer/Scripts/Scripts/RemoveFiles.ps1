#$currentPath = $MyInvocation.MyCommand.Path | Split-Path -Parent
clear
"Start removing archiving"

$folderPath = $currentPath + "\Publish\*.*"
$archivePath = $currentPath + "\Publish.zip"


Remove-Item $folderPath
Remove-Item $archivePath

"Finish removing archiving"