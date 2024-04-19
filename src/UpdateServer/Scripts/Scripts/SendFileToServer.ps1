clear
"Start sending the file to server " + $url 

$ErrorActionPreference = 'Stop'
 
#$program = 'WellForce'

#$sourceFilePath  = $path  + "\Publish.zip"
#$installFilePath  = $path  + "\Output\WellForce Install.exe"
#$changelogFilePath  = $path  + "\Input\changelog.txt"
#$fileExePath  = $path + "\Publish\Well Force.exe"
#$login = 'werymag'
#$password = 'vXmmy2X4'

$sourceFilePath  = $currentPath  + $sourceFilePath
$installFilePath  = $currentPath  + $installFilePath 
$changelogFilePath  = $currentPath  + $changelogFilePath
$fileExePath  = $currentPath + $fileExePath

$version = (Get-Item $fileExePath).VersionInfo.FileVersionRaw

Try {
    Add-Type -AssemblyName 'System.Net.Http'
	
    $client = New-Object System.Net.Http.HttpClient
    $content = New-Object System.Net.Http.MultipartFormDataContent
	

    $sorceFileStream = [System.IO.File]::OpenRead($sourceFilePath)    
    $sorceFileName = [System.IO.Path]::GetFileName($sourceFilePath)
    $sorceFileContent = New-Object System.Net.Http.StreamContent($sorceFileStream)
    $sorceFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   	
    $sorceFileContent.Headers.Add('program',$program);    
    $sorceFileContent.Headers.Add('Version',$version);
    $content.Add($sorceFileContent, 'SourceFile', $sorceFileName)      
	
  	$porgramNameContent = [System.Net.Http.StringContent]::new($program,[System.Text.Encoding]::UTF8,'text/csv')
    $versionContent = [System.Net.Http.StringContent]::new($version,[System.Text.Encoding]::UTF8,'text/csv')
    $content.Add($porgramNameContent, 'program') 
    $content.Add($versionContent, 'Version') 


    $installFileStream = [System.IO.File]::OpenRead($installFilePath)    
    $installFileName = [System.IO.Path]::GetFileName($installFilePath)
    $installFileContent = New-Object System.Net.Http.StreamContent($installFileStream)
    $installFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   
    $content.Add($installFileContent, 'InstallFile', $installFileName) 
   
   
    $changelogFileStream = [System.IO.File]::OpenRead($changelogFilePath)    
    $changelogFileName = [System.IO.Path]::GetFileName($changelogFilePath)
    $changelogFileContent = New-Object System.Net.Http.StreamContent($changelogFileStream)
    $changelogFileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");   
    $content.Add($changelogFileContent, 'ChangeLog', $changelogFileName) 

    
    $loginContent = [System.Net.Http.StringContent]::new($login,[System.Text.Encoding]::UTF8,'text/csv')
    $passwordContent = [System.Net.Http.StringContent]::new($password,[System.Text.Encoding]::UTF8,'text/csv')
    $content.Add($loginContent, 'Login') 
    $content.Add($passwordContent, 'Password') 

    
    $result = $client.PostAsync($url, $content).Result
    $result.EnsureSuccessStatusCode()   
}
Catch {
    Read-Host "Error sending the file"
    Write-Error $_

    exit 1
}
Finally {
    if ($client -ne $null) { $client.Dispose() }
    if ($content -ne $null) { $content.Dispose() }
    if ($fileStream -ne $null) { $fileStream.Dispose() }
    if ($fileContent -ne $null) { $fileContent.Dispose() }
}

"Finish sending file"





       # $body = @{
       # Login = 'testLogin'
       # Password = 'testPassword'
       #}  | ConvertTo-Json
       #$stringContent = [System.Net.Http.StringContent]::new(
       #    $body,
       # [System.Text.Encoding]::UTF8,'application/json')
       # $body = @{
       # Login = 'testLogin'
       # Password = 'testPassword'
       #}   | ConvertTo-Json
