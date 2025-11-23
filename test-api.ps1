# Test Script for Kitten Remote Control API
# PowerShell version

Write-Host "=== Kitten Remote Control API Test Script ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:8080"

# Test function
function Test-Endpoint {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body = $null,
        [string]$Description
    )
    
    Write-Host "Testing: $Description" -ForegroundColor Yellow
    Write-Host "  $Method $Path" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = "$baseUrl$Path"
            Method = $Method
            ContentType = "application/json"
            ErrorAction = "Stop"
        }
        
        if ($Body) {
            $params.Body = $Body
            Write-Host "  Body: $Body" -ForegroundColor Gray
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "  ✓ Success!" -ForegroundColor Green
        Write-Host "  Response: $($response | ConvertTo-Json -Compress)" -ForegroundColor Green
        Write-Host ""
        return $true
    }
    catch {
        Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# Wait for server to be ready
Write-Host "Checking if server is running..." -ForegroundColor Cyan
try {
    $null = Invoke-RestMethod -Uri "$baseUrl/telemetry/totalMass" -Method Get -TimeoutSec 2
    Write-Host "✓ Server is running!" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "✗ Server is not responding. Please start the game with the mod loaded." -ForegroundColor Red
    Write-Host ""
    exit 1
}

# === CONTROL ENDPOINTS ===
Write-Host "=== Testing Control Endpoints ===" -ForegroundColor Cyan
Write-Host ""

# Test throttle GET
Test-Endpoint -Method "GET" -Path "/control/throttle" -Description "Get current throttle"

# Test throttle SET with JSON
Test-Endpoint -Method "PUT" -Path "/control/throttle" -Body '{"throttle":0.5}' -Description "Set throttle to 0.5 (JSON)"

# Test throttle SET with plain value
Test-Endpoint -Method "PUT" -Path "/control/throttle" -Body "0.75" -Description "Set throttle to 0.75 (plain)"

# Test throttle GET again
Test-Endpoint -Method "GET" -Path "/control/throttle" -Description "Get throttle after change"

# Test engine on/off
Test-Endpoint -Method "GET" -Path "/control/engineOn" -Description "Get engine state"
Test-Endpoint -Method "PUT" -Path "/control/engineOn" -Body '{"engineOn":true}' -Description "Turn engine on"
Test-Endpoint -Method "GET" -Path "/control/engineOn" -Description "Get engine state after change"

# Test reference frames
Test-Endpoint -Method "GET" -Path "/control/referenceFrames" -Description "List all reference frames"
Test-Endpoint -Method "GET" -Path "/control/referenceFrame" -Description "Get current reference frame"

# === TELEMETRY ENDPOINTS ===
Write-Host "=== Testing Telemetry Endpoints ===" -ForegroundColor Cyan
Write-Host ""

Test-Endpoint -Method "GET" -Path "/telemetry/totalMass" -Description "Get total mass"
Test-Endpoint -Method "GET" -Path "/telemetry/propellantMass" -Description "Get propellant mass"
Test-Endpoint -Method "GET" -Path "/telemetry/orbitalSpeed" -Description "Get orbital speed"
Test-Endpoint -Method "GET" -Path "/telemetry/apoapsis" -Description "Get apoapsis"
Test-Endpoint -Method "GET" -Path "/telemetry/periapsis" -Description "Get periapsis"
Test-Endpoint -Method "GET" -Path "/telemetry/apoapsis_elevation" -Description "Get apoapsis elevation"
Test-Endpoint -Method "GET" -Path "/telemetry/periapsis_elevation" -Description "Get periapsis elevation"
Test-Endpoint -Method "GET" -Path "/telemetry/orbitingBody/meanRadius" -Description "Get orbiting body mean radius"

# === FLIGHT COMPUTER ENDPOINTS ===
Write-Host "=== Testing Flight Computer Endpoints ===" -ForegroundColor Cyan
Write-Host ""

Test-Endpoint -Method "GET" -Path "/control/flightComputer/attitudeModes" -Description "List attitude modes"
Test-Endpoint -Method "GET" -Path "/control/flightComputer/attitudeMode" -Description "Get current attitude mode"

Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tip: You can also test individual endpoints with Invoke-RestMethod:" -ForegroundColor Yellow
Write-Host '  Invoke-RestMethod -Uri "http://localhost:8080/telemetry/totalMass" -Method Get' -ForegroundColor Gray
Write-Host '  Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" -Method Put -Body ''{"throttle":0.5}'' -ContentType "application/json"' -ForegroundColor Gray
Write-Host ""

