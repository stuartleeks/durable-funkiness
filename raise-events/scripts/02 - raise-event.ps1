param(
    $functionUrl = "http://localhost:7071/api",

)
$ErrorActionPreference = "Stop"


Write-Host "invoking function ..."
$r = Invoke-RestMethod "$functionUrl/"
$r




