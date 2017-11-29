param(
    $rootPath = "c:\temp\mock-storage-batch-test",
    $functionUrl = "http://localhost:7071/api/StorageBatches_HttpStart",
    [Parameter(Mandatory=$true)]
    [string]
    $customerId,
    [string]
    $timestamp # used to generate unique paths, otherwise the function just digs up an old instance!

)
$ErrorActionPreference = "Stop"

if ($timestamp -eq ""){
    $timestamp = Get-Date -f "yyyyMMdd_HHmmss" # scenario was just hhmm at the end, but added ss for ease of dev
    Write-Host "Using timestamp of $timestamp"
    Write-Host
}

$customerFileLookup = @{
    cust1 = @("file1.txt", "file2.txt", "file3.txt", "file4.txt")
}

$customerFiles = $customerFileLookup[$customerId]
if ($customerFiles -eq $null){
    Write-Host "Customer not found - $customerId"
    return
}

$path = "$rootPath\01-simple"
if (!(Test-Path $path)){
    New-Item -Path $path -ItemType Directory    
}

$customerFiles | ForEach-Object {
    # Create file
    Set-Content -Path "$path\$($customerId)_$($timestamp)_$($_)" -Value "test"
    # Invoke trigger for each file
    $r = Invoke-RestMethod "$functionUrl`?path=$path\$($customerId)_$($timestamp)_$($customerFiles[0])"
}

while ($true){
    $status = Invoke-RestMethod $r.statusQueryGetUri
    if ($status.runtimeStatus -eq "Completed"){
        Write-Host "Function completed"
        break
    }
    Write-Host "Status: $($status.runtimeStatus) - last updated $($status.lastUpdatedTime) ..."
    Start-Sleep -Seconds 3
}

Invoke-RestMethod $r.statusQueryGetUri


while ($true){
    $status = Invoke-RestMethod $r.statusQueryGetUri
    if ($status.runtimeStatus -eq "Completed"){
        Write-Host "Function completed"
        break
    }
    Write-Host "Status: $($status.runtimeStatus) - last updated $($status.lastUpdatedTime) ..."
    Start-Sleep -Seconds 3
}