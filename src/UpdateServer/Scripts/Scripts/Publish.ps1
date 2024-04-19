clear
"Start publish"
$publishProfilePath = $currentPath  + $publishProfilePath
dotnet publish $publishProfilePath  -p:PublishProfile=FolderProfile
"Finish publish"
#Read-Host $currentPath + "asdasda"
