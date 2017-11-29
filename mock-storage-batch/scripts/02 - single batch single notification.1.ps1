param(
    $rootPath = "c:\temp\mock-storage-batch-test",
    $functionUrl = "http://localhost:7071/api/StorageBatches_HttpStart",
    [Parameter(Mandatory=$true)]
    [string]
    $invocationId # used to generate unique paths, otherwise the function just digs up an old instance!
)

$ErrorActionPreference = "Stop"

$path = "$rootPath\02-single-notification-$invocationId"
if (!(Test-Path $path)){
    New-Item -Path $path -ItemType Directory    
}

Set-Content -Path "$path\file1.txt" -Value "test"
Set-Content -Path "$path\file2.txt" -Value "test"
Set-Content -Path "$path\file3.txt" -Value "test"
Set-Content -Path "$path\file4.txt" -Value "test"
$r = Invoke-RestMethod "$functionUrl`?path=$path\file4.txt"

while ($true){
    $status = Invoke-RestMethod $r.statusQueryGetUri
    if ($status.runtimeStatus -eq "Completed"){
        Write-Host "Function completed"
        break
    }
    Write-Host "Status: $($status.runtimeStatus) - last updated $($status.lastUpdatedTime) ..."
    Start-Sleep -Seconds 3
}