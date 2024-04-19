clear
"Start of file archiving"

$folderPath =$currentPath + "\Publish\*"
$archivePath = $currentPath  + "\Publish.zip"

$archiveExist = Test-Path -Path $archivePath

if ( $archiveExist  )
{
    Remove-Item $archivePath
}


Compress-Archive -Path $folderPath -DestinationPath $archivePath

"Finish of file archiving"