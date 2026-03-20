# Jellyfin Web Terminal

A Jellyfin plugin that provides a web-based terminal for executing commands on your server directly from the admin panel.

> **SECURITY WARNING:** This plugin allows **arbitrary command execution** on the host machine with the privileges of the Jellyfin service account (typically Local System on Windows or the jellyfin user on Linux). **Only install this plugin if you fully understand the security implications.** A compromised Jellyfin admin account with access to this plugin means full control of the server.

## Features

- Real-time streaming output (Server-Sent Events)
- Command history (up/down arrow keys)
- Ctrl+C to cancel running commands
- Configurable working directory
- Configurable shell (cmd.exe, powershell.exe, /bin/bash, etc.)
- Configurable command timeout (default: 5 minutes)
- **API key authentication** on top of Jellyfin admin auth (two-layer security)
- Plugin settings page for managing the API key and shell configuration

## Installation

1. In Jellyfin, go to **Dashboard > Plugins > Repositories**
2. Add a new repository:
   - **Name:** Web Terminal
   - **URL:** `https://raw.githubusercontent.com/camdenbalberg/jellyfin-plugin-web-terminal/main/manifest.json`
3. Go to **Catalog** and install **Web Terminal**
4. Restart Jellyfin

## Setup

After installation:

1. Go to **Dashboard > Plugins > Web Terminal > Settings**
2. An API key is **auto-generated** on first install — copy it
3. Open the **Web Terminal** from the sidebar menu
4. Enter the API key when prompted
5. Start running commands

## Security Model

This plugin uses **two layers of authentication**:

1. **Jellyfin Admin Auth** — The API endpoints require `RequiresElevation` policy (administrator privileges). Non-admin users cannot access the terminal API at all.

2. **API Key** — Every request to the terminal API must include a valid `X-Terminal-Key` header. The key is configured in the plugin settings and auto-generated on first install. This prevents a compromised admin session from automatically having terminal access.

### Recommendations

- Regenerate the API key periodically
- Use HTTPS for your Jellyfin server (the API key is sent in headers)
- Monitor the Jellyfin logs for terminal usage
- Only grant Jellyfin admin access to trusted users

## Building from Source

Requires .NET 9 SDK.

```bash
dotnet publish Jellyfin.Plugin.HelloWorld --configuration Release --output bin/publish
```

The output DLL can be placed in your Jellyfin plugins directory, or packaged as a `.zip` for repository distribution.

## License

MIT
