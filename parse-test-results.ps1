# Parse TRX files and aggregate test results
$resultsDir = "C:\Users\User\Desktop\SurveyBot\test-stability-results"
$testResults = @{}

for ($i = 1; $i -le 10; $i++) {
    $trxPath = "$resultsDir\run-$i.trx"

    if (Test-Path $trxPath) {
        [xml]$trx = Get-Content $trxPath
        $ns = @{t="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

        $results = Select-Xml -Xml $trx -Namespace $ns -XPath "//t:UnitTestResult"

        foreach ($result in $results) {
            $testName = $result.Node.testName
            $outcome = $result.Node.outcome

            if (-not $testResults.ContainsKey($testName)) {
                $testResults[$testName] = @{
                    Passed = 0
                    Failed = 0
                    Outcomes = @()
                }
            }

            $testResults[$testName].Outcomes += $outcome

            if ($outcome -eq "Passed") {
                $testResults[$testName].Passed++
            } elseif ($outcome -eq "Failed") {
                $testResults[$testName].Failed++
            }
        }
    }
}

# Categorize tests
$alwaysFail = @()
$alwaysPass = @()
$flaky = @()
$other = @()

foreach ($test in $testResults.GetEnumerator()) {
    $passRate = $test.Value.Passed / 10 * 100

    $obj = [PSCustomObject]@{
        TestName = $test.Key
        Passed = $test.Value.Passed
        Failed = $test.Value.Failed
        PassRate = $passRate
        Outcomes = $test.Value.Outcomes -join ","
    }

    if ($test.Value.Failed -eq 10) {
        $alwaysFail += $obj
    } elseif ($test.Value.Passed -eq 10) {
        $alwaysPass += $obj
    } elseif ($test.Value.Failed -gt 0 -and $test.Value.Passed -gt 0) {
        $flaky += $obj
    } else {
        $other += $obj
    }
}

# Output results
Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "TESTS THAT ALWAYS FAIL (0/10 passes):" -ForegroundColor Red
Write-Host "=============================================`n" -ForegroundColor Cyan
$alwaysFail | Sort-Object TestName | ForEach-Object { Write-Host "  - $($_.TestName)" }
Write-Host "`nTotal always failing: $($alwaysFail.Count)" -ForegroundColor Red

Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "FLAKY TESTS (sometimes pass, sometimes fail):" -ForegroundColor Yellow
Write-Host "=============================================`n" -ForegroundColor Cyan
$flaky | Sort-Object PassRate | ForEach-Object {
    Write-Host "  - $($_.TestName)" -ForegroundColor Yellow
    Write-Host "    Pass rate: $($_.PassRate)% ($($_.Passed)/10 passes)" -ForegroundColor Gray
}
Write-Host "`nTotal flaky: $($flaky.Count)" -ForegroundColor Yellow

Write-Host "`n=============================================" -ForegroundColor Cyan
Write-Host "SUMMARY:" -ForegroundColor Green
Write-Host "=============================================`n" -ForegroundColor Cyan
Write-Host "  Total unique tests: $($testResults.Count)"
Write-Host "  Always passing: $($alwaysPass.Count)" -ForegroundColor Green
Write-Host "  Always failing: $($alwaysFail.Count)" -ForegroundColor Red
Write-Host "  Flaky tests:    $($flaky.Count)" -ForegroundColor Yellow
Write-Host "  Other:          $($other.Count)"

# Export to JSON for further analysis
$report = @{
    AlwaysFail = $alwaysFail | Select-Object TestName
    Flaky = $flaky | Select-Object TestName, Passed, Failed, PassRate
    Summary = @{
        TotalTests = $testResults.Count
        AlwaysPassing = $alwaysPass.Count
        AlwaysFailing = $alwaysFail.Count
        FlakyTests = $flaky.Count
    }
}

$report | ConvertTo-Json -Depth 5 | Out-File "$resultsDir\aggregated-results.json"
Write-Host "`nDetailed results saved to: $resultsDir\aggregated-results.json"
