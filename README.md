# Kitten Remote Control - API Documentation

A simple socket-based server mod for Kitten Space Agency that enables remote control via simple text commands.

## Installation

Install according this explanation: https://forums.ahwoo.com/threads/how-to-use-starmap-mod-loader.398/#post-2113

## Protocol

The server uses a simple text-based protocol over TCP sockets.

### Command Format

- **GET /path** - Retrieve a value
  - Response: `OK value` or `ERROR message`
  
- **SET /path value** - Set a value
  - Response: `OK` or `ERROR message`

## Available Endpoints

### Control Endpoints

#### GET /control/throttle
Get the current engine throttle value (0.0 to 1.0).

**Example:**
```
GET /control/throttle
OK 0.75
```

#### SET /control/throttle
Set the engine throttle value (0.0 to 1.0).

**Example:**
```
SET /control/throttle 0.5
OK
```

**Error:**
```
SET /control/throttle 1.5
ERROR: Throttle must be between 0.0 and 1.0, got 1.5
```

#### GET /control/engineOn
Get the engine on/off status.

**Returns:** `1` = engine on, `0` = engine off

**Example:**
```
GET /control/engineOn
OK 1
```

#### SET /control/engineOn
Turn the engine on or off.

**Values:** `0` = off, `1` = on

**Example:**
```
SET /control/engineOn 1
OK
```

**Error:**
```
SET /control/engineOn 2
ERROR: EngineOn must be 0 or 1, got 2
```

#### GET /control/referenceFrame
Get the current navball/reference frame for the controlled vehicle.

**Example:**
```
GET /control/referenceFrame
OK LVLH
```

#### SET /control/referenceFrame VALUE
Set the navball/reference frame. VALUE can be the enum name (case-insensitive) or its numeric value.

**Examples:**
```
SET /control/referenceFrame LVLH
OK

SET /control/referenceFrame 2
OK
```

**Error:**
```
SET /control/referenceFrame unknown
ERROR: Invalid reference frame: 'unknown'
```

When setting the reference frame the server will also attempt to update the flight computer (RateHold) if the flight computer is in auto attitude mode.

#### GET /control/referenceFrames
List all available reference frame names (comma separated).

**Example:**
```
GET /control/referenceFrames
OK LVLH,Inertial,Surface
```

#### GET /control/FlightComputer/AttitudeMode
Get the current attitude mode of the vehicle's flight computer.

**Example:**
```
GET /control/FlightComputer/AttitudeMode
OK Auto
```

#### GET /control/FlightComputer/AttitudeModes
List all available flight computer attitude modes.

**Example:**
```
GET /control/FlightComputer/AttitudeModes
OK Auto,Manual,Hold
```

#### SET /control/FlightComputer/AttitudeMode VALUE
Set the flight computer attitude mode by name. Value is case-insensitive.

**Example:**
```
SET /control/FlightComputer/AttitudeMode Auto
OK
```

**Error:**
```
SET /control/FlightComputer/AttitudeMode Foo
ERROR: Invalid FlightComputer AttitudeMode: 'Foo'
```

#### SET /control/FlightComputer/stabilization VALUE
Enable or disable stabilization. VALUE `1` = enable, `0` = disable.

**Example:**
```
SET /control/FlightComputer/stabilization 1
OK
```


### Telemetry Endpoints

All telemetry endpoints are read-only (GET only) and return numeric values as strings in invariant culture format.

#### GET /telemetry/apoapsis
Get the apoapsis (highest point) of the current orbit.

**Example:**
```
GET /telemetry/apoapsis
OK 750000.0
```

#### GET /telemetry/apopasis
Alias for `/telemetry/apoapsis` (common misspelling).

#### GET /telemetry/periapsis
Get the periapsis (lowest point) of the current orbit.

**Example:**
```
GET /telemetry/periapsis
OK 250000.0
```

#### GET /telemetry/orbitingBody/meanRadius
Get the mean radius of the body being orbited.

**Example:**
```
GET /telemetry/orbitingBody/meanRadius
OK 600000.0
```

#### GET /telemetry/apoapsis_elevation
Get the apoapsis elevation above the surface (apoapsis - body mean radius).

**Example:**
```
GET /telemetry/apoapsis_elevation
OK 150000.0
```

#### GET /telemetry/periapsis_elevation
Get the periapsis elevation above the surface (periapsis - body mean radius).

**Example:**
```
GET /telemetry/periapsis_elevation
OK 80000.0
```

#### GET /telemetry/orbitalSpeed
Get the current orbital speed of the vessel.

**Example:**
```
GET /telemetry/orbitalSpeed
OK 2250.5
```

#### GET /telemetry/propellantMass
Get the current propellant mass of the vessel.

**Example:**
```
GET /telemetry/propellantMass
OK 1500.25
```

#### GET /telemetry/totalMass
Get the total mass of the vessel.

**Example:**
```
GET /telemetry/totalMass
OK 5000.75
```

## Examples with Various Tools

### Python (without client)
```python
import socket

def send_command(cmd):
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(("localhost", 8080))
    sock.sendall(cmd.encode('utf-8'))
    response = sock.recv(4096).decode('utf-8')
    sock.close()
    return response

# GET example
print(send_command("GET /telemetry/apoapsis"))  # OK 750000.0

# SET example
print(send_command("SET /control/throttle 0.5"))  # OK
```

### Netcat
```bash
# GET
echo "GET /telemetry/periapsis" | nc localhost 8080

# SET
echo "SET /control/throttle 0.75" | nc localhost 8080
```

### PowerShell
```powershell
$client = [System.Net.Sockets.TcpClient]::new("localhost", 8080)
$stream = $client.GetStream()
$bytes = [System.Text.Encoding]::UTF8.GetBytes("GET /telemetry/orbitalSpeed")
$stream.Write($bytes, 0, $bytes.Length)
$buffer = New-Object byte[] 4096
$count = $stream.Read($buffer, 0, 4096)
[System.Text.Encoding]::UTF8.GetString($buffer, 0, $count)
$client.Close()
```

### C#
```csharp
using System.Net.Sockets;
using System.Text;

var client = new TcpClient("localhost", 8080);
var stream = client.GetStream();

var command = Encoding.UTF8.GetBytes("GET /control/throttle");
stream.Write(command, 0, command.Length);

var buffer = new byte[4096];
var bytesRead = stream.Read(buffer, 0, buffer.Length);
var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
Console.WriteLine(response);

client.Close();
```

## Error Handling

All endpoints return errors in the format:
```
ERROR: <error message>
```

Common error scenarios:
- **No vehicle controlled**: When trying to access telemetry without an active vessel
- **Invalid value**: When setting values outside allowed ranges
- **Field not found**: When reflection fails to access internal game structures

## Development

### Build
```bash
dotnet build -c Release
```

### Deployment
The DLL is built to `bin/Release/net9.0/KittenRemoteControl.dll` and can be copied to the game's mod directory.

## Features

- ✅ Simple socket-based server
- ✅ No external dependencies (only .NET BCL)
- ✅ No administrator access required
- ✅ Works over network (not just localhost)
- ✅ Simple text protocol
- ✅ Async/await for non-blocking requests
- ✅ Thread-safe
- ✅ Clean reflection-based access to private game structures

## Advantages over HTTP

- **Simpler**: Just text commands, no HTTP overhead
- **No Admin Rights**: Socket server doesn't need URL-ACL registration
- **Network-ready**: Works automatically on all network interfaces
- **Lightweight**: Minimal overhead, very fast

## Technical Details

The server uses `TcpListener` from the .NET Base Class Library:
- Port: 8080 (configurable)
- Protocol: Plain text over TCP
- Format: Command-based (GET/SET)
- Encoding: UTF-8

Access to private game structures (`_manualControlInputs`) is achieved through reflection, with proper struct write-back to ensure changes persist.

## Quick Reference

### Control Commands
| Endpoint | Type | Range | Description |
|----------|------|-------|-------------|
| `/control/throttle` | GET/SET | 0.0-1.0 | Engine throttle |
| `/control/engineOn` | GET/SET | 0 or 1 | Engine on/off |
| `/control/referenceFrame` | GET/SET | enum or numeric | Navball / reference frame |
| `/control/referenceFrames` | GET | - | Comma separated list of frames |
| `/control/FlightComputer/AttitudeMode` | GET/SET | enum name | FlightComputer attitude mode |
| `/control/FlightComputer/AttitudeModes` | GET | - | Comma separated list of modes |
| `/control/FlightComputer/stabilization` | SET | 0 or 1 | Enable/disable stabilization |

### Telemetry Commands (Read-Only)
| Endpoint | Description |
|----------|-------------|
| `/telemetry/apoapsis` | Apoapsis altitude |
| `/telemetry/apopasis` | Alias for apoapsis |
| `/telemetry/periapsis` | Periapsis altitude |
| `/telemetry/orbitingBody/meanRadius` | Body mean radius |
| `/telemetry/apoapsis_elevation` | Apoapsis above surface |
| `/telemetry/periapsis_elevation` | Periapsis above surface |
| `/telemetry/orbitalSpeed` | Current orbital velocity |
| `/telemetry/propellantMass` | Current propellant mass |
| `/telemetry/totalMass` | Total vessel mass |

## License

MIT License
