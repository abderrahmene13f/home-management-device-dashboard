# Home Management Device Dashboard

A .NET 10 monitoring dashboard extension for the virtual devices in [HomeManagementDeviceApi](https://github.com/VladislavAntonyuk/HomeManagementDeviceApi).

## Features
- Polls virtual device `/info` endpoints.
- Shows total, online and offline device counts.
- Displays device name, network address, last-seen time and metrics.
- Automatically polls every 15 seconds (configurable).
- Stores monitoring snapshots in SQLite.
- Exposes JSON API endpoints and device history.
- Responsive dark dashboard UI and Docker support.

## Requirements
- .NET 10 SDK, or Docker Desktop.
- The original virtual device emulator project.

## Start the emulator
Clone the original project and follow its README:
https://github.com/VladislavAntonyuk/HomeManagementDeviceApi

Run each emulator in a separate terminal:
```bash
dotnet run --launch-profile Device1
dotnet run --launch-profile Device2
```
Verify `http://localhost:1235/info` and `http://localhost:1237/info`.
The original repository README currently documents ports 1235 and 1237. If your mentor provides a different port such as 1234, change the matching `InfoUrl` in `appsettings.json`.

## Run dashboard locally
```bash
dotnet restore
dotnet run
```
Open http://localhost:5080

## Run with Docker
Start the virtual devices on the host, then run:
```bash
docker compose up --build
```
Open http://localhost:5080. Docker Compose uses `host.docker.internal` to reach emulator instances on the host.

## API endpoints
- `GET /health` — health check
- `GET /api/dashboard` — totals and current device status
- `GET /api/devices` — latest status per device
- `POST /api/devices/{id}/refresh` — refresh one device
- `GET /api/devices/{id}/history` — latest 50 SQLite snapshots

## Configuration
Edit `Devices:Items` in `appsettings.json` to add devices. A device is online when its latest successful response is newer than `OnlineThresholdSeconds`.

## Important
This is the dashboard extension, not a replacement for the original emulator repository. Start the emulator first. The dashboard deliberately does not expose arbitrary operating-system command execution; command controls should only be implemented using a verified allow-list from the emulator's `/config` endpoint.

## Suggested public repository name
`home-management-device-dashboard`
