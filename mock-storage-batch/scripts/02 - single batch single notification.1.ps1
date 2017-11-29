param(
    $rootPath = "c:\temp\mock-storage-batch-test",
    $functionUrl = "http://localhost:7071/api/StorageBatches_HttpStart",
    [Parameter(Mandatory=$true)]
    [ValidateSet("cust1", "cust2", "cust3")]
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
    cust1 = @("file1.txt", "file2.txt", "file3.txt", "file4.txt");
    cust2 = @("file1.txt", "file2.txt");
    cust3 = @("file1.txt", "file2.txt", "file3.txt", "file4.txt", "file5.txt", "file6.txt", "file7.txt", "file8.txt", "file9.txt");
}

$customerFiles = $customerFileLookup[$customerId]
if ($customerFiles -eq $null){
    Write-Host "Customer not found - $customerId"
    return
}

$path = "$rootPath\02-single-notification"
if (!(Test-Path $path)){
    New-Item -Path $path -ItemType Directory    
}

# Create all files
$customerFiles | ForEach-Object {
    Set-Content -Path "$path\$($customerId)_$($timestamp)_$($_)" -Value "test"
}
# Invoke trigger for single file
$r = Invoke-RestMethod "$functionUrl`?path=$path\$($customerId)_$($timestamp)_$($customerFiles[0])"

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
