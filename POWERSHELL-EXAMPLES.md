# PowerShell API Examples

This file contains PowerShell-specific examples for using the Kitten Remote Control API.

## Important Note

In PowerShell, `curl` is an alias for `Invoke-WebRequest`. The syntax is different from the Unix `curl` command.

## Quick Test

Run the provided test script:
```powershell
.\test-api.ps1
```

## Basic Syntax

### Using Invoke-RestMethod (Recommended)

`Invoke-RestMethod` automatically parses JSON responses:

```powershell
# GET request
$response = Invoke-RestMethod -Uri "http://localhost:8080/telemetry/totalMass" -Method Get
Write-Host $response.totalMass

# PUT request with JSON body
$body = @{
    throttle = 0.5
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
Write-Host $response.throttle
```

### Using Invoke-WebRequest

`Invoke-WebRequest` gives you more control but requires manual JSON parsing:

```powershell
# GET request
$response = Invoke-WebRequest -Uri "http://localhost:8080/telemetry/totalMass" -Method Get
$data = $response.Content | ConvertFrom-Json
Write-Host $data.totalMass

# PUT request
$body = '{"throttle":0.5}'
$response = Invoke-WebRequest -Uri "http://localhost:8080/control/throttle" `
    -Method Put `
    -Body $body `
    -ContentType "application/json"
$data = $response.Content | ConvertFrom-Json
Write-Host $data.throttle
```

## Common Examples

### Control Endpoints

#### Set Throttle
```powershell
# JSON body
Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" `
    -Method Put `
    -Body '{"throttle":0.75}' `
    -ContentType "application/json"

# Plain value
Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" `
    -Method Put `
    -Body "0.75" `
    -ContentType "text/plain"
```

#### Get Throttle
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" -Method Get
Write-Host "Current throttle: $($response.throttle)"
```

#### Set Engine On/Off
```powershell
# Turn on (JSON)
Invoke-RestMethod -Uri "http://localhost:8080/control/engineOn" `
    -Method Put `
    -Body '{"engineOn":true}' `
    -ContentType "application/json"

# Turn off (plain value)
Invoke-RestMethod -Uri "http://localhost:8080/control/engineOn" `
    -Method Put `
    -Body "0" `
    -ContentType "text/plain"
```

#### Get Engine State
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/engineOn" -Method Get
Write-Host "Engine is on: $($response.engineOn)"
```

#### Set Reference Frame
```powershell
# By name
Invoke-RestMethod -Uri "http://localhost:8080/control/referenceFrame" `
    -Method Put `
    -Body '{"frame":"LVLH"}' `
    -ContentType "application/json"

# By ID
Invoke-RestMethod -Uri "http://localhost:8080/control/referenceFrame" `
    -Method Put `
    -Body '{"frame":0}' `
    -ContentType "application/json"

# Plain value
Invoke-RestMethod -Uri "http://localhost:8080/control/referenceFrame" `
    -Method Put `
    -Body "LVLH" `
    -ContentType "text/plain"
```

#### List Available Reference Frames
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/referenceFrames" -Method Get
$response.frames | ForEach-Object {
    Write-Host "$($_.name) = $($_.value)"
}
```

### Telemetry Endpoints

#### Get All Telemetry
```powershell
$baseUrl = "http://localhost:8080"

Write-Host "=== Vessel Telemetry ==="
$mass = Invoke-RestMethod -Uri "$baseUrl/telemetry/totalMass" -Method Get
Write-Host "Total Mass: $($mass.totalMass) kg"

$propellant = Invoke-RestMethod -Uri "$baseUrl/telemetry/propellantMass" -Method Get
Write-Host "Propellant Mass: $($propellant.propellantMass) kg"

$speed = Invoke-RestMethod -Uri "$baseUrl/telemetry/orbitalSpeed" -Method Get
Write-Host "Orbital Speed: $($speed.orbitalSpeed) m/s"

Write-Host ""
Write-Host "=== Orbital Parameters ==="
$apo = Invoke-RestMethod -Uri "$baseUrl/telemetry/apoapsis" -Method Get
Write-Host "Apoapsis: $($apo.apoapsis) m"

$peri = Invoke-RestMethod -Uri "$baseUrl/telemetry/periapsis" -Method Get
Write-Host "Periapsis: $($peri.periapsis) m"

$apoElev = Invoke-RestMethod -Uri "$baseUrl/telemetry/apoapsis_elevation" -Method Get
Write-Host "Apoapsis Elevation: $($apoElev.apoapsisElevation) m"

$periElev = Invoke-RestMethod -Uri "$baseUrl/telemetry/periapsis_elevation" -Method Get
Write-Host "Periapsis Elevation: $($periElev.periapsisElevation) m"

$radius = Invoke-RestMethod -Uri "$baseUrl/telemetry/orbitingBody/meanRadius" -Method Get
Write-Host "Body Mean Radius: $($radius.meanRadius) m"
```

### Flight Computer Endpoints

#### Get Attitude Mode
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/attitudeMode" -Method Get
Write-Host "Attitude Mode: $($response.attitudeMode) (ID: $($response.modeId))"
```

#### Set Attitude Mode
```powershell
# By name
Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/attitudeMode" `
    -Method Put `
    -Body '{"mode":"Auto"}' `
    -ContentType "application/json"

# By ID
Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/attitudeMode" `
    -Method Put `
    -Body '{"mode":0}' `
    -ContentType "application/json"
```

#### List Attitude Modes
```powershell
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/attitudeModes" -Method Get
$response.modes | ForEach-Object {
    Write-Host "$($_.name) = $($_.value)"
}
```

#### Set Stabilization
```powershell
# Enable
Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/stabilization" `
    -Method Put `
    -Body '{"stabilization":true}' `
    -ContentType "application/json"

# Disable
Invoke-RestMethod -Uri "http://localhost:8080/control/flightComputer/stabilization" `
    -Method Put `
    -Body '{"stabilization":false}' `
    -ContentType "application/json"
```

## Advanced Examples

### Monitoring Script
```powershell
# Monitor telemetry in real-time
$baseUrl = "http://localhost:8080"

while ($true) {
    Clear-Host
    Write-Host "=== Kitten Remote Control - Live Telemetry ===" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        $mass = Invoke-RestMethod -Uri "$baseUrl/telemetry/totalMass" -Method Get
        $speed = Invoke-RestMethod -Uri "$baseUrl/telemetry/orbitalSpeed" -Method Get
        $apo = Invoke-RestMethod -Uri "$baseUrl/telemetry/apoapsis_elevation" -Method Get
        $peri = Invoke-RestMethod -Uri "$baseUrl/telemetry/periapsis_elevation" -Method Get
        
        Write-Host "Mass: $([math]::Round($mass.totalMass, 2)) kg"
        Write-Host "Speed: $([math]::Round($speed.orbitalSpeed, 2)) m/s"
        Write-Host "Apoapsis: $([math]::Round($apo.apoapsisElevation / 1000, 2)) km"
        Write-Host "Periapsis: $([math]::Round($peri.periapsisElevation / 1000, 2)) km"
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 1
}
```

### Automatic Launch Sequence
```powershell
$baseUrl = "http://localhost:8080"

Write-Host "=== Automated Launch Sequence ===" -ForegroundColor Cyan

# 1. Turn on engine
Write-Host "1. Turning on engine..."
Invoke-RestMethod -Uri "$baseUrl/control/engineOn" -Method Put -Body '{"engineOn":true}' -ContentType "application/json"
Start-Sleep -Seconds 1

# 2. Set throttle to 50%
Write-Host "2. Setting throttle to 50%..."
Invoke-RestMethod -Uri "$baseUrl/control/throttle" -Method Put -Body '{"throttle":0.5}' -ContentType "application/json"
Start-Sleep -Seconds 5

# 3. Increase to 100%
Write-Host "3. Increasing throttle to 100%..."
Invoke-RestMethod -Uri "$baseUrl/control/throttle" -Method Put -Body '{"throttle":1.0}' -ContentType "application/json"
Start-Sleep -Seconds 10

# 4. Cut engines
Write-Host "4. Cutting engines..."
Invoke-RestMethod -Uri "$baseUrl/control/throttle" -Method Put -Body '{"throttle":0.0}' -ContentType "application/json"

Write-Host "Launch sequence complete!" -ForegroundColor Green
```

## Error Handling

```powershell
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" `
        -Method Put `
        -Body '{"throttle":1.5}' `
        -ContentType "application/json" `
        -ErrorAction Stop
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json
    
    Write-Host "Error $statusCode : $($errorBody.error)" -ForegroundColor Red
}
```

## Using Real curl (if installed)

If you have the real `curl.exe` installed (not the PowerShell alias), you can use it like this:

```powershell
# Use full path or remove alias first
Remove-Item Alias:curl

# Now you can use real curl
curl.exe -X PUT -H "Content-Type: application/json" -d '{"throttle":0.5}' http://localhost:8080/control/throttle

# Or use full path
C:\Windows\System32\curl.exe -X PUT -H "Content-Type: application/json" -d '{"throttle":0.5}' http://localhost:8080/control/throttle
```

## Troubleshooting

### Server not responding
```powershell
# Check if server is running
try {
    $response = Invoke-RestMethod -Uri "http://localhost:8080/telemetry/totalMass" -Method Get -TimeoutSec 2
    Write-Host "✓ Server is running" -ForegroundColor Green
}
catch {
    Write-Host "✗ Server is not responding. Make sure the game is running with the mod loaded." -ForegroundColor Red
}
```

### View raw response
```powershell
$response = Invoke-WebRequest -Uri "http://localhost:8080/control/throttle" -Method Get
Write-Host "Status: $($response.StatusCode)"
Write-Host "Content-Type: $($response.Headers.'Content-Type')"
Write-Host "Body: $($response.Content)"
```

---

For more examples and documentation, see [README.md](README.md) and [openapi.yaml](openapi.yaml).

