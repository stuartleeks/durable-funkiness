param(
    $functionUrl = "http://localhost:7071/api/HttpStart",
    [Parameter(Mandatory=$true)]
    [string]
    $instanceId,
    [int]
    $count = 5,
    [int]
    $delayInMilliseconds = 0
)
$ErrorActionPreference = "Stop"


for ($i = 0; $i -lt $count; $i++) {
    Write-Host "invoke instanceid $instanceId - id $i..."
    $r = Invoke-RestMethod "$functionUrl`?instanceId=$instanceId&id=$i"
    Start-Sleep -Milliseconds $delayInMilliseconds
}

Write-Host "Done."
