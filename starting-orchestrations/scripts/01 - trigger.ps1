param(
    $functionUrl = "http://localhost:7071/api/HttpStart",
    [Parameter(Mandatory=$true)]
    [string]
    $instanceId
)
$ErrorActionPreference = "Stop"

$r = Invoke-RestMethod "$functionUrl`?instanceId=$instanceId"
