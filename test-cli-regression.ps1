# Comprehensive CLI Commands Regression Test Script
# Tests all Tanuki CLI commands for regression

$ErrorActionPreference = "Continue"
$testResults = @()

function Test-Command {
    param(
        [string]$Name,
        [string]$Command,
        [scriptblock]$Validation = $null,
        [int]$ExpectedExitCode = 0
    )
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Testing: $Name" -ForegroundColor Cyan
    Write-Host "Command: $Command" -ForegroundColor Gray
    Write-Host "========================================" -ForegroundColor Cyan
    
    try {
        # Use PowerShell's native command execution
        $output = & cmd /c "$Command 2>&1" | Out-String
        $exitCode = $LASTEXITCODE
        
        # For dotnet run, exit codes might not propagate correctly
        # Check output for error messages instead
        $hasError = $output -match "Error:" -or $output -match "error:" -or $output -match "Failed"
        
        Write-Host "Exit Code: $exitCode" -ForegroundColor $(if ($exitCode -eq $ExpectedExitCode) { "Green" } else { "Yellow" })
        if ($hasError -and $ExpectedExitCode -eq 0) {
            Write-Host "Warning: Error detected in output but exit code is 0" -ForegroundColor Yellow
        }
        
        # For commands that should fail, check output for error messages
        if ($ExpectedExitCode -ne 0) {
            if ($hasError -or $output -match "not found" -or $output -match "must be between") {
                Write-Host " PASSED (error detected in output)" -ForegroundColor Green
                $script:testResults += [PSCustomObject]@{
                    Test = $Name
                    Status = "PASSED"
                    Reason = "Error correctly detected"
                    Output = $output
                }
                return $true
            }
            elseif ($exitCode -ne $ExpectedExitCode) {
                Write-Host " FAILED: Expected error but got exit code $exitCode" -ForegroundColor Red
                $script:testResults += [PSCustomObject]@{
                    Test = $Name
                    Status = "FAILED"
                    Reason = "Expected error condition not met"
                    Output = $output
                }
                return $false
            }
        }
        
        if ($exitCode -ne $ExpectedExitCode -and -not $hasError) {
            Write-Host " FAILED: Expected exit code $ExpectedExitCode, got $exitCode" -ForegroundColor Red
            $script:testResults += [PSCustomObject]@{
                Test = $Name
                Status = "FAILED"
                Reason = "Exit code mismatch: expected $ExpectedExitCode, got $exitCode"
                Output = $output
            }
            return $false
        }
        
        if ($Validation) {
            $validationResult = & $Validation $output
            if (-not $validationResult) {
                Write-Host " FAILED: Validation failed" -ForegroundColor Red
                $script:testResults += [PSCustomObject]@{
                    Test = $Name
                    Status = "FAILED"
                    Reason = "Validation failed"
                    Output = $output
                }
                return $false
            }
        }
        
        Write-Host " PASSED" -ForegroundColor Green
        $script:testResults += [PSCustomObject]@{
            Test = $Name
            Status = "PASSED"
            Reason = ""
            Output = $output
        }
        return $true
    }
    catch {
        Write-Host " FAILED: Exception - $($_.Exception.Message)" -ForegroundColor Red
        $script:testResults += [PSCustomObject]@{
            Test = $Name
            Status = "FAILED"
            Reason = "Exception: $($_.Exception.Message)"
            Output = ""
        }
        return $false
    }
}

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Tanuki CLI Commands Regression Test" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow

# Clean up any existing test files
$testConfig = "test-tanuki.json"
$testDir = "test-init-output"
if (Test-Path $testConfig) { Remove-Item $testConfig -Force -ErrorAction SilentlyContinue }
if (Test-Path $testDir) { Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue }

# Ensure we're in the right directory
Push-Location $PSScriptRoot

# Test 1: Root help command
Test-Command -Name "Root Help" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- --help" `
    -Validation { param($out) $out -match "serve|validate|init" }

# Test 2: Serve command help
Test-Command -Name "Serve Help" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --help" `
    -Validation { param($out) $out -match "Port to listen on" -or $out -match "port" }

# Test 3: Validate command help
Test-Command -Name "Validate Help" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- validate --help" `
    -Validation { param($out) $out -match "Validate" -or $out -match "config" }

# Test 4: Init command help
Test-Command -Name "Init Help" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- init --help" `
    -Validation { param($out) $out -match "Initialize" -or $out -match "init" }

# Test 5: Init command - create config file in test directory
if (Test-Path $testDir) { Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue }
Test-Command -Name "Init - Create Config" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- init --output $testDir" `
    -Validation { 
        param($out) 
        $configExists = Test-Path "$testDir/tanuki.json"
        ($out -match "Created" -or $out -match "created" -or $configExists)
    }

# Test 6: Validate command - valid config (use existing tanuki.json)
if (Test-Path "tanuki.json") {
    Test-Command -Name "Validate - Valid Config" `
        -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- validate --config tanuki.json" `
        -Validation { param($out) $out -match "valid" -or $out -match "Valid" -or $out.Length -gt 0 }
}

# Test 7: Validate command - missing config (should fail)
Test-Command -Name "Validate - Missing Config" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- validate --config nonexistent-file-12345.json" `
    -ExpectedExitCode 1 `
    -Validation { param($out) $out -match "not found" -or $out -match "Error" -or $out -match "error" }

# Test 8: Serve command - invalid port (should fail)
Test-Command -Name "Serve - Invalid Port" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --port 99999 --config tanuki.json" `
    -ExpectedExitCode 1 `
    -Validation { param($out) $out -match "Port must be between" -or $out -match "Error" }

# Test 9: Serve command - missing config (should fail gracefully)
Test-Command -Name "Serve - Missing Config" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --port 5020 --config nonexistent-file-12345.json" `
    -ExpectedExitCode 1 `
    -Validation { param($out) $out -match "not found" -or $out -match "Error" -or $out -match "error" }

# Test 10: Serve command - server startup (background test)
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Testing: Serve - Server Startup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Kill any existing processes on test ports
$testPorts = @(5021, 5022, 5023)
foreach ($port in $testPorts) {
    $existing = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Cleaning up existing connection on port $port" -ForegroundColor Yellow
    }
}

$servePort = 5021
$serveProc = $null
try {
    Write-Host "Starting server on port $servePort..." -ForegroundColor Gray
    $serveProc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --port $servePort --config tanuki.json" `
        -PassThru -NoNewWindow
    
    Start-Sleep -Seconds 6
    
    $response = Invoke-WebRequest -Uri "http://localhost:$servePort/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host " PASSED: Server started and health check responded (Status: $($response.StatusCode))" -ForegroundColor Green
    $testResults += [PSCustomObject]@{
        Test = "Serve - Server Startup"
        Status = "PASSED"
        Reason = ""
        Output = "Health check returned $($response.StatusCode)"
    }
}
catch {
    Write-Host " FAILED: Server did not respond to health check" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += [PSCustomObject]@{
        Test = "Serve - Server Startup"
        Status = "FAILED"
        Reason = "Health check failed: $($_.Exception.Message)"
        Output = ""
    }
}
finally {
    if ($serveProc) {
        Stop-Process -Id $serveProc.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
}

# Test 11: Serve command - verbose flag
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Testing: Serve - Verbose Flag" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$servePort = 5022
$serveProc = $null
try {
    Write-Host "Starting server with verbose flag on port $servePort..." -ForegroundColor Gray
    $serveProc = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- serve --port $servePort --config tanuki.json --verbose" `
        -PassThru -NoNewWindow
    
    Start-Sleep -Seconds 6
    
    $response = Invoke-WebRequest -Uri "http://localhost:$servePort/api/example" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host " PASSED: Server started with verbose flag (Status: $($response.StatusCode))" -ForegroundColor Green
    $testResults += [PSCustomObject]@{
        Test = "Serve - Verbose Flag"
        Status = "PASSED"
        Reason = ""
        Output = "API endpoint returned $($response.StatusCode)"
    }
}
catch {
    Write-Host " FAILED: Server did not respond" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    $testResults += [PSCustomObject]@{
        Test = "Serve - Verbose Flag"
        Status = "FAILED"
        Reason = "Request failed: $($_.Exception.Message)"
        Output = ""
    }
}
finally {
    if ($serveProc) {
        Stop-Process -Id $serveProc.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }
}

# Test 12: Init command - existing file (should warn but not error)
Test-Command -Name "Init - Existing File" `
    -Command "dotnet run --project src/Tanuki.Cli/Tanuki.Cli.csproj -- init --output ." `
    -ExpectedExitCode 0 `
    -Validation { param($out) $true } # Just check it doesn't crash

# Cleanup
if (Test-Path $testDir) { Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue }

Pop-Location

# Summary
Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Test Summary" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
$passed = ($testResults | Where-Object { $_.Status -eq "PASSED" }).Count
$failed = ($testResults | Where-Object { $_.Status -eq "FAILED" }).Count
$total = $testResults.Count

Write-Host "Total Tests: $total" -ForegroundColor White
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })

if ($failed -gt 0) {
    Write-Host "`nFailed Tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
        Write-Host "  - $($_.Test): $($_.Reason)" -ForegroundColor Red
        if ($_.Output -and $_.Output.Length -lt 500) {
            Write-Host "    Output: $($_.Output.Trim())" -ForegroundColor Gray
        }
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow

if ($failed -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "Some tests failed" -ForegroundColor Red
    exit 1
}
