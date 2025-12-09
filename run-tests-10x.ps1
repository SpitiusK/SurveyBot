# PowerShell script to run tests 10 times and collect results
$ErrorActionPreference = "Continue"
$resultsDir = "C:\Users\User\Desktop\SurveyBot\test-stability-results"

# Create results directory
if (Test-Path $resultsDir) {
    Remove-Item -Recurse -Force $resultsDir
}
New-Item -ItemType Directory -Path $resultsDir | Out-Null

# Run tests 10 times
for ($i = 1; $i -le 10; $i++) {
    Write-Host "========== Running test iteration $i/10 ==========" -ForegroundColor Cyan

    $trxPath = "$resultsDir\run-$i.trx"

    # Run tests with TRX logger
    dotnet test "C:\Users\User\Desktop\SurveyBot\tests\SurveyBot.Tests" `
        --no-build `
        --logger "trx;LogFileName=$trxPath" `
        2>&1 | Out-Null

    Write-Host "Completed iteration $i" -ForegroundColor Green
}

Write-Host "`n========== All 10 runs completed ==========" -ForegroundColor Yellow
Write-Host "Results saved to: $resultsDir" -ForegroundColor Yellow
