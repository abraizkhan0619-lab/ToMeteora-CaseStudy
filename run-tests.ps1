param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe"
)

$project = $PSScriptRoot
$results = Join-Path $project "TestResults"
New-Item -ItemType Directory -Force $results | Out-Null
$failed = $false

foreach ($platform in "EditMode", "PlayMode") {
    Write-Host "Running $platform tests..."
    $xml = Join-Path $results "$platform-results.xml"
    $log = Join-Path $results "$platform-log.txt"
    $p = Start-Process $Unity -Wait -PassThru -ArgumentList @(
        "-batchmode", "-projectPath", "`"$project`"", "-runTests",
        "-testPlatform", $platform, "-testResults", "`"$xml`"", "-logFile", "`"$log`"")

    if (Test-Path $xml) {
        $run = ([xml](Get-Content $xml)).'test-run'
        Write-Host "  $platform : $($run.passed) passed, $($run.failed) failed (exit code $($p.ExitCode))"
    } else {
        Write-Host "  $platform : no results. Check $log (compile error, or project open in the Editor?)"
    }
    if ($p.ExitCode -ne 0) { $failed = $true }
}

if ($failed) { exit 1 }
