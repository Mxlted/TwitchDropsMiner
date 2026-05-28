# Twitch Drops Miner for Windows

This is a Windows-first standalone layout for a fork of
[rangermix/TwitchDropsMiner](https://github.com/rangermix/TwitchDropsMiner).
The WPF launcher is the primary app surface. The Python FastAPI miner, web UI,
translations, Docker files, and tests are included as the backend/runtime layer
that the launcher starts and supervises.

## What Is Primary Here

- `Start-Windows.ps1` runs the Windows launcher from source.
- `Build-Windows.ps1` creates a one-folder Windows build.
- `Test-Windows.ps1` runs the Windows launcher test harness.
- `windows/TwitchDropsMiner.Windows/` contains the WPF desktop shell.
- `windows/TwitchDropsMiner.Windows.Core/` contains launcher logic that is easy
  to test without WPF.
- `src/`, `web/`, `lang/`, and `icons/` are the backend miner, local web UI,
  translations, and assets used by the launcher.

## Requirements

- Windows 10 or newer
- .NET 8 SDK for building/running from source
- Python 3.12+
- `uv` recommended for Python dependency management

## Run The Windows App

```powershell
.\Start-Windows.ps1
```

The launcher starts the local backend and opens the web UI at
`http://127.0.0.1:8080`.

The launcher resolves Python in this order:

1. `TDM_PYTHON`
2. `env\Scripts\python.exe`
3. `.venv\Scripts\python.exe`
4. `uv`
5. `py -3.12`
6. `python`

## Build The Windows Package

```powershell
.\Build-Windows.ps1
```

The packaged app is written to `artifacts\windows\TwitchDropsMiner`.

To include the .NET runtime in the package:

```powershell
.\Build-Windows.ps1 -SelfContained
```

## Test

```powershell
.\Test-Windows.ps1
```

Backend tests can still be run directly:

```powershell
uv sync
uv run python -m pytest tests/
```

## Backend-Only Use

The backend can run without the Windows shell:

```powershell
uv sync
uv run python main.py
```

Then open `http://localhost:8080`.

Docker remains available for headless use:

```powershell
docker compose up -d
```

## Forking And Pushing

See [FORK_SETUP.md](FORK_SETUP.md) for the exact commands to turn this folder
into a Git working tree for your fork while preserving the upstream fork
history.
