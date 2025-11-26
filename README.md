# Kitten Remote Control - REST API Documentation

A RESTful API server mod for Kitten Space Agency that enables remote control via HTTP endpoints with JSON responses.

## Installation

### From GitHub Releases (Recommended)

1. **Download the latest release** from the [Releases page](https://github.com/[your-repo]/KittenRemoteControl/releases)
2. **Extract the ZIP file** to your game's mod directory:
   ```
   <Game Directory>/mods/KittenRemoteControl/
   ```
   Example:
   ```
   C:\Program Files\Kitten Space Agency\mods\KittenRemoteControl\
   ```
3. **Launch the game** - StarMap mod loader will automatically load the mod
4. **Check the console** for the message: "Remote Control REST Server started successfully on http://localhost:8080"

### Manual Installation

If you want to build from source:

1. Clone the repository
2. Ensure you have the KSA DLLs in the `KSA_dlls` directory (see [Development](#development))
3. Build the project:
   ```bash
   dotnet build -c Release
   ```
4. Copy the contents of `bin/Release/net9.0/` to your game's mod directory

### Verification

To verify the installation:
1. Launch Kitten Space Agency
2. Open a browser and navigate to: http://localhost:8080/telemetry/totalMass
3. You should see a JSON response with the current vessel's mass

## What's Included

The release package includes:
- ✅ Main mod DLL (KittenRemoteControl.dll)
- ✅ All required dependencies (Harmony, Grapevine, Microsoft.Extensions.*)
- ✅ Complete documentation (README, OpenAPI spec)
- ✅ All license files

**Note**: Kitten Space Agency game files are NOT included. You must own the game to use this mod.

For a complete list of included files, see [RELEASE-PACKAGE.md](RELEASE-PACKAGE.md).

## Requirements

- **Kitten Space Agency** (the game)
- **StarMap Mod Loader** v0.3.1 or higher
- **.NET 9.0 Runtime** (usually included with the game)

## Uninstallation

To remove the mod:
1. Delete the `KittenRemoteControl` folder from your game's mods directory
2. Restart the game

## Installation according to this explanation

See: https://forums.ahwoo.com/threads/how-to-use-starmap-mod-loader.398/#post-2113

## Protocol

The server uses a RESTful HTTP API with JSON request/response format powered by Grapevine.

### Base URL
```
http://localhost:8080
```

### Response Format

All endpoints return JSON objects. Successful responses include relevant data, error responses include an `error` field.

**Success Example:**
```json
{
  "throttle": 0.75
}
```

**Error Example:**
```json
{
  "error": "Throttle must be between 0.0 and 1.0, got 1.5"
}
```

## Available Endpoints

### Control Endpoints

#### GET /control/throttle
Get the current engine throttle value (0.0 to 1.0).

**Response:**
```json
{
  "throttle": 0.75
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/throttle
```

#### PUT /control/throttle
Set the engine throttle value (0.0 to 1.0).

**Request Body (JSON):**
```json
{
  "throttle": 0.5
}
```

**Or plain value:**
```
0.5
```

**Success Response:**
```json
{
  "success": true,
  "throttle": 0.5
}
```

**Error Response:**
```json
{
  "error": "Throttle must be between 0.0 and 1.0, got 1.5"
}
```

**cURL Examples:**
```bash
# JSON
curl -X PUT -H "Content-Type: application/json" -d '{"throttle":0.5}' http://localhost:8080/control/throttle

# Plain value
curl -X PUT -H "Content-Type: text/plain" -d '0.5' http://localhost:8080/control/throttle
```

#### GET /control/engineOn
Get the engine on/off status.

**Response:**
```json
{
  "engineOn": true
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/engineOn
```

#### PUT /control/engineOn
Turn the engine on or off.

**Request Body (JSON):**
```json
{
  "engineOn": true
}
```

**Or plain value:** `true`, `false`, `1`, or `0`

**Success Response:**
```json
{
  "success": true,
  "engineOn": true
}
```

**cURL Examples:**
```bash
# JSON
curl -X PUT -H "Content-Type: application/json" -d '{"engineOn":true}' http://localhost:8080/control/engineOn

# Plain value
curl -X PUT -H "Content-Type: text/plain" -d '1' http://localhost:8080/control/engineOn
```

#### GET /control/thrusters
Get the current thruster command flags as a dictionary mapping thruster names to boolean (true = command on).

**Response:**
```json
{
  "thrusters": {
    "RollRight": true,
    "RollLeft": false,
    "PitchUp": false,
    "PitchDown": false,
    "YawRight": false,
    "YawLeft": false,
    "TranslateForward": false,
    "TranslateBackward": false,
    "TranslateRight": false,
    "TranslateLeft": false,
    "TranslateDown": false,
    "TranslateUp": false
  }
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/thrusters
```

#### POST /control/thrusters
Set thruster command flags in batch by providing a JSON object mapping thruster enum names to boolean/number/string values. Numbers and the strings "1"/"true" (case-insensitive) are treated as true; 0 and "0"/"false" are false.

Behaviour: this endpoint performs a partial update — only the keys provided in the request are changed; unspecified flags remain as they were.

**Request Body (JSON with `thrusters` object):**
```json
{
  "thrusters": {
    "RollRight": true,
    "PitchUp": 1,
    "TranslateUp": "true",
    "YawLeft": 0
  }
}
```

**Or request body can be the object itself:**
```json
{
  "RollRight": true,
  "PitchUp": 1
}
```

**Success Response:**
```json
{
  "success": true,
  "thrusters": {
    "RollRight": true,
    "RollLeft": false,
    "PitchUp": true,
    ...
  }
}
```

**Error Responses:**
- Unknown thruster names → 400 with a list of unknown keys.
```json
{ "error": "Unknown thruster keys", "unknown": ["Foo"] }
```
- Invalid JSON structure → 400

**cURL Example:**
```bash
curl -X POST -H "Content-Type: application/json" -d '{"thrusters":{"RollRight":1,"PitchUp":0}}' http://localhost:8080/control/thrusters
```

#### GET /control/referenceFrame
Get the current navball/reference frame for the controlled vehicle.

**Response:**
```json
{
  "frame": "LVLH",
  "frameId": 0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/referenceFrame
```

#### PUT /control/referenceFrame
Set the navball/reference frame. Value can be the enum name (case-insensitive) or its numeric value.

**Request Body (JSON with name):**
```json
{
  "frame": "LVLH"
}
```

**Request Body (JSON with ID):**
```json
{
  "frame": 0
}
```

**Or plain value:** `LVLH` or `0`

**Success Response:**
```json
{
  "success": true,
  "frame": "LVLH",
  "frameId": 0
}
```

**Error Response:**
```json
{
  "error": "Invalid reference frame: 'unknown'"
}
```

**cURL Examples:**
```bash
# JSON with name
curl -X PUT -H "Content-Type: application/json" -d '{"frame":"LVLH"}' http://localhost:8080/control/referenceFrame

# JSON with ID
curl -X PUT -H "Content-Type: application/json" -d '{"frame":0}' http://localhost:8080/control/referenceFrame

# Plain value
curl -X PUT -H "Content-Type: text/plain" -d 'LVLH' http://localhost:8080/control/referenceFrame
```

**Note:** When setting the reference frame, the server will also attempt to update the flight computer (RateHold) if the flight computer is in auto attitude mode.

#### GET /control/referenceFrames
List all available reference frames.

**Response:**
```json
{
  "frames": [
    { "name": "LVLH", "value": 0 },
    { "name": "Inertial", "value": 1 },
    { "name": "Surface", "value": 2 }
  ]
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/referenceFrames
```

#### GET /control/flightComputer/attitudeMode
Get the current attitude mode of the vehicle's flight computer.

**Response:**
```json
{
  "attitudeMode": "Auto",
  "modeId": 0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/flightComputer/attitudeMode
```

#### GET /control/flightComputer/attitudeModes
List all available flight computer attitude modes.

**Response:**
```json
{
  "modes": [
    { "name": "Auto", "value": 0 },
    { "name": "Manual", "value": 1 },
    { "name": "Hold", "value": 2 }
  ]
}
```

**cURL Example:**
```bash
curl http://localhost:8080/control/flightComputer/attitudeModes
```

#### PUT /control/flightComputer/attitudeMode
Set the flight computer attitude mode by name or ID.

**Request Body (JSON):**
```json
{
  "mode": "Auto"
}
```

**Or:** `{"mode": 0}` or plain value `Auto` or `0`

**Success Response:**
```json
{
  "success": true,
  "attitudeMode": "Auto",
  "modeId": 0
}
```

**Error Response:**
```json
{
  "error": "Invalid FlightComputer AttitudeMode: 'Foo'"
}
```

**cURL Examples:**
```bash
# JSON
curl -X PUT -H "Content-Type: application/json" -d '{"mode":"Auto"}' http://localhost:8080/control/flightComputer/attitudeMode

# Plain value
curl -X PUT -H "Content-Type: text/plain" -d 'Auto' http://localhost:8080/control/flightComputer/attitudeMode
```

#### PUT /control/flightComputer/stabilization
Enable or disable stabilization.

**Request Body (JSON):**
```json
{
  "stabilization": true
}
```

**Or plain value:** `true`, `false`, `1`, or `0`

**Success Response:**
```json
{
  "success": true,
  "stabilization": true
}
```

**cURL Examples:**
```bash
# JSON
curl -X PUT -H "Content-Type: application/json" -d '{"stabilization":true}' http://localhost:8080/control/flightComputer/stabilization

# Plain value
curl -X PUT -H "Content-Type: text/plain" -d '1' http://localhost:8080/control/flightComputer/stabilization
```

### Telemetry Endpoints

All telemetry endpoints are read-only (GET only) and return numeric values in JSON format.

#### GET /telemetry/apoapsis
Get the apoapsis (highest point) of the current orbit in meters.

**Response:**
```json
{
  "apoapsis": 750000.0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/apoapsis
```

#### GET /telemetry/periapsis
Get the periapsis (lowest point) of the current orbit in meters.

**Response:**
```json
{
  "periapsis": 250000.0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/periapsis
```

#### GET /telemetry/orbitingBody/meanRadius
Get the mean radius of the body being orbited in meters.

**Response:**
```json
{
  "meanRadius": 600000.0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/orbitingBody/meanRadius
```

#### GET /telemetry/apoapsis_elevation
Get the apoapsis elevation above the surface (apoapsis - body mean radius) in meters.

**Response:**
```json
{
  "apoapsisElevation": 150000.0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/apoapsis_elevation
```

#### GET /telemetry/periapsis_elevation
Get the periapsis elevation above the surface (periapsis - body mean radius) in meters.

**Response:**
```json
{
  "periapsisElevation": 80000.0
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/periapsis_elevation
```

#### GET /telemetry/orbitalSpeed
Get the current orbital speed of the vessel in m/s.

**Response:**
```json
{
  "orbitalSpeed": 2250.5
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/orbitalSpeed
```

#### GET /telemetry/propellantMass
Get the current propellant mass of the vessel in kg.

**Response:**
```json
{
  "propellantMass": 1500.25
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/propellantMass
```

#### GET /telemetry/totalMass
Get the total mass of the vessel in kg.

**Response:**
```json
{
  "totalMass": 5000.75
}
```

**cURL Example:**
```bash
curl http://localhost:8080/telemetry/totalMass
```

## Examples with Various Tools

### Python
```python
import requests

# GET example
response = requests.get("http://localhost:8080/telemetry/apoapsis")
print(response.json())  # {'apoapsis': 750000.0}

# PUT example with JSON
response = requests.put(
    "http://localhost:8080/control/throttle",
    json={"throttle": 0.5}
)
print(response.json())  # {'success': True, 'throttle': 0.5}

# PUT example with plain value
response = requests.put(
    "http://localhost:8080/control/throttle",
    data="0.5",
    headers={"Content-Type": "text/plain"}
)
print(response.json())  # {'success': True, 'throttle': 0.5}
```

### JavaScript (Node.js)
```javascript
// GET example
fetch('http://localhost:8080/telemetry/orbitalSpeed')
  .then(res => res.json())
  .then(data => console.log(data));  // {orbitalSpeed: 2250.5}

// PUT example
fetch('http://localhost:8080/control/throttle', {
  method: 'PUT',
  headers: {'Content-Type': 'application/json'},
  body: JSON.stringify({throttle: 0.5})
})
  .then(res => res.json())
  .then(data => console.log(data));  // {success: true, throttle: 0.5}
```

### PowerShell
```powershell
# GET example
$response = Invoke-RestMethod -Uri "http://localhost:8080/telemetry/apoapsis"
Write-Host $response.apoapsis  # 750000.0

# PUT example with JSON
$body = @{throttle = 0.5} | ConvertTo-Json
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" -Method Put -Body $body -ContentType "application/json"
Write-Host $response.throttle  # 0.5

# PUT example with plain value
$response = Invoke-RestMethod -Uri "http://localhost:8080/control/throttle" -Method Put -Body "0.5" -ContentType "text/plain"
Write-Host $response.throttle  # 0.5
```

### C#
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

// GET example
var getResponse = await client.GetAsync("/telemetry/apoapsis");
var getJson = await getResponse.Content.ReadAsStringAsync();
var getResult = JsonSerializer.Deserialize<Dictionary<string, double>>(getJson);
Console.WriteLine(getResult["apoapsis"]);  // 750000.0

// PUT example with JSON
var putData = new { throttle = 0.5 };
var putJson = JsonSerializer.Serialize(putData);
var putContent = new StringContent(putJson, Encoding.UTF8, "application/json");
var putResponse = await client.PutAsync("/control/throttle", putContent);
var putResult = await putResponse.Content.ReadAsStringAsync();
Console.WriteLine(putResult);  // {"success":true,"throttle":0.5}
```

## Error Handling

All endpoints return error responses with HTTP status codes:
- **200 OK**: Successful operation
- **400 Bad Request**: Invalid input (e.g., value out of range, invalid enum value)
- **500 Internal Server Error**: Server-side error

**Error Response Format:**
```json
{
  "error": "Error message describing what went wrong"
}
```

Common error scenarios:
- **No vehicle controlled**: When trying to access control/telemetry without an active vessel
- **Invalid value**: When setting values outside allowed ranges
- **Invalid enum**: When providing an invalid reference frame or attitude mode name
- **Field not found**: When reflection fails to access internal game structures

## Development

### Build
```bash
dotnet build -c Release
```

### Deployment
The DLL is built to `bin/Release/net9.0/KittenRemoteControl.dll` and can be copied to the game's mod directory along with Grapevine dependencies.

## Features

- ✅ RESTful HTTP API with JSON
- ✅ Powered by Grapevine server
- ✅ Flexible input (JSON objects or plain values)
- ✅ Proper HTTP status codes
- ✅ Clean and modern API design
- ✅ Works over network (not just localhost)
- ✅ Async/await for non-blocking requests
- ✅ Thread-safe
- ✅ Clean reflection-based access to private game structures

## Technical Details

The server uses Grapevine REST server framework:
- Port: 8080 (configurable)
- Protocol: HTTP/1.1
- Format: JSON (with fallback to plain text for simple values)
- Encoding: UTF-8

Access to private game structures (`_manualControlInputs`) is achieved through reflection via `ManualControlHelper`, with proper struct write-back to ensure changes persist.

## API Reference

### Control Endpoints
| Endpoint | Method | Description | Request | Response |
|----------|--------|-------------|---------|----------|
| `/control/throttle` | GET | Get throttle | - | `{throttle: float}` |
| `/control/throttle` | PUT | Set throttle (0.0-1.0) | `{throttle: float}` or plain | `{success: bool, throttle: float}` |
| `/control/engineOn` | GET | Get engine state | - | `{engineOn: bool}` |
| `/control/engineOn` | PUT | Set engine on/off | `{engineOn: bool}` or `0`/`1` | `{success: bool, engineOn: bool}` |
| `/control/thrusters` | GET | Get current thruster commands | - | `{thrusters: {thrusterName: bool}}` |
| `/control/thrusters` | POST | Set thruster commands (batch) | `{thrusters: {thrusterName: bool}}` or plain | `{success: bool, thrusters: {thrusterName: bool}}` |
| `/control/referenceFrame` | GET | Get reference frame | - | `{frame: string, frameId: int}` |
| `/control/referenceFrame` | PUT | Set reference frame | `{frame: string/int}` or plain | `{success: bool, frame: string, frameId: int}` |
| `/control/referenceFrames` | GET | List reference frames | - | `{frames: array}` |
| `/control/flightComputer/attitudeMode` | GET | Get attitude mode | - | `{attitudeMode: string, modeId: int}` |
| `/control/flightComputer/attitudeMode` | PUT | Set attitude mode | `{mode: string/int}` or plain | `{success: bool, attitudeMode: string, modeId: int}` |
| `/control/flightComputer/attitudeModes` | GET | List attitude modes | - | `{modes: array}` |
| `/control/flightComputer/stabilization` | PUT | Set stabilization | `{stabilization: bool}` or `0`/`1` | `{success: bool, stabilization: bool}` |

### Telemetry Endpoints (All GET, Read-Only)
| Endpoint | Description | Response |
|----------|-------------|----------|
| `/telemetry/apoapsis` | Apoapsis altitude (m) | `{apoapsis: float}` |
| `/telemetry/periapsis` | Periapsis altitude (m) | `{periapsis: float}` |
| `/telemetry/orbitingBody/meanRadius` | Body mean radius (m) | `{meanRadius: float}` |
| `/telemetry/apoapsis_elevation` | Apoapsis above surface (m) | `{apoapsisElevation: float}` |
| `/telemetry/periapsis_elevation` | Periapsis above surface (m) | `{periapsisElevation: float}` |
| `/telemetry/orbitalSpeed` | Orbital velocity (m/s) | `{orbitalSpeed: float}` |
| `/telemetry/propellantMass` | Propellant mass (kg) | `{propellantMass: float}` |
| `/telemetry/totalMass` | Total vessel mass (kg) | `{totalMass: float}` |

## OpenAPI Documentation

See [openapi.yaml](openapi.yaml) for the complete OpenAPI 3.0 specification.

## License

MIT License
