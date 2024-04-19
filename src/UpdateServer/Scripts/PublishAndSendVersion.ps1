$currentPath = $MyInvocation.MyCommand.Path | Split-Path -Parent
Get-Content .\Input\Variables.txt | Foreach-Object{
   $var = $_.Split('=')
   New-Variable -Name $var[0] -Value $var[1]  
}

.\Scripts\RemoveFiles.ps1 
timeout /t 4
.\Scripts\Publish.ps1
timeout /t 4
.\Scripts\Archive.ps1
timeout /t 4
Start-Process  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\installer.iss   -Wait
timeout /t 4
.\Scripts\SendFileToServer.ps1 

Read-Host "Program pushed to server"