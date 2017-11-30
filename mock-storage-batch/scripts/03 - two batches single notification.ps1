param(
    $rootPath = "c:\temp\mock-storage-batch-test",
    $functionUrl = "http://localhost:7071/api/StorageBatches_HttpStart",
    [string]
    $timestamp # used to generate unique paths, otherwise the function just digs up an old instance!
)
$ErrorActionPreference = "Stop"

if ($timestamp -eq "") {
    $timestamp = Get-Date -f "yyyyMMdd_HHmmss" # scenario was just hhmm at the end, but added ss for ease of dev
    Write-Host "Using timestamp of $timestamp"
    Write-Host
}

$customerFiles1 = @("file1.txt", "file2.txt", "file3.txt", "file4.txt")
$customerFiles2 = @("file1.txt", "file2.txt")


$path = "$rootPath\02-single-notification"
if (!(Test-Path $path)) {
    New-Item -Path $path -ItemType Directory    
}

# Create all files
$customerFiles1 | ForEach-Object {
    Set-Content -Path "$path\cust1_$($timestamp)_$($_)" -Value "test"
}
$customerFiles2 | ForEach-Object {
    Set-Content -Path "$path\cust2_$($timestamp)_$($_)" -Value "test"
}

#cust1 - file1
$r1 = Invoke-RestMethod "$functionUrl`?path=$path\cust1_$($timestamp)_$($customerFiles1[0])"
#cust2 - file1
$r2 = Invoke-RestMethod "$functionUrl`?path=$path\cust2_$($timestamp)_$($customerFiles2[0])"

function WaitForCompletion($statusQueryGetUri) {
    while ($true) {
        $status = Invoke-RestMethod $statusQueryGetUri
        if ($status.runtimeStatus -eq "Completed") {
            Write-Host "Function completed"
            break
        }
        Write-Host "Status: $($status.runtimeStatus) - last updated $($status.lastUpdatedTime) ..."
        Start-Sleep -Seconds 3
    }

    Invoke-RestMethod $statusQueryGetUri
}

WaitForCompletion $r1.statusQueryGetUri
WaitForCompletion $r2.statusQueryGetUri
