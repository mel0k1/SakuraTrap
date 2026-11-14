<div align="center">

<img src="Images/SakuraTrap-Icon.png" width="140" alt="SakuraTrap" />

# SakuraTrap

**Custom Roblox bootstrapper with one-click performance profiles**

[![CI](https://img.shields.io/github/actions/workflow/status/mel0k1/SakuraTrap/ci.yml?branch=main&style=flat-square&label=CI)](https://github.com/mel0k1/SakuraTrap/actions/workflows/ci.yml)
[![License](https://img.shields.io/github/license/mel0k1/SakuraTrap?style=flat-square)](LICENSE)
[![Русский](https://img.shields.io/badge/README-Русский-ff69b4?style=flat-square)](README.md)

</div>

SakuraTrap is a fork of [Fishstrap](https://github.com/fishstrap/fishstrap), which is itself a fork of [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap). It is a Roblox launcher for Windows 10 and newer: it installs and updates the game, applies FastFlags, mods and shortcuts in place of the stock Roblox bootstrapper.

The core idea behind SakuraTrap is that you should not have to hand-tune hundreds of FastFlags. Instead, you pick a single performance profile and the launcher applies a consistent set of flags for you: frame rate, render quality, anti-aliasing and mesh detail are changed together, not one by one.

> [!WARNING]
> SakuraTrap is not affiliated with Roblox Corporation. Roblox restricts local FastFlags with an allowlist — flags outside the list are ignored by the client, so some settings may not apply. That is a Roblox-side limitation, not a launcher bug.

## Features

- **FastFlag profiles** — Default / Balanced / Performance / Quality, one selection in settings (see the table below)
- Everything Fishstrap offers: frame rate unlock, renderer choice (Vulkan / D3D11), MSAA, mesh detail, mods (cursors, sounds, old textures), Discord Rich Presence, Roblox Studio support, custom bootstrapper dialogs
- Full FastFlag editor and Global Basic Settings editor (frame cap, quality levels, mouse sensitivity)
- Sakura icon and a fresh rebrand

## FastFlag profiles

Located in **Settings → FastFlags → Performance profile**. A profile only touches its own flags: your manual settings and editor flags stay untouched. The selected profile is remembered across launches.

| Flag | Purpose | Balanced | Performance | Quality | Default |
|---|---|---|---|---|---|
| `DFIntTaskSchedulerTargetFps` | Target FPS | 144 | 240 | 240 | — |
| `DFIntDebugFRMQualityLevelOverride` | Render quality (1–21) | 10 | 1 | 21 | — |
| `FIntDebugForceMSAASamples` | Anti-aliasing | — | 1 | 4 | — |
| `DFFlagDebugRenderForceTechnologyVoxel` | Simplified lighting | — | True | — | — |
| `DFIntCSGLevelOfDetailSwitchingDistance*` | Mesh detail (0–3) | 1 | 0 | 3 | — |

`Default` clears only the profile flags, handing those settings back to Roblox and your manual edits.

## Installation

1. Download `SakuraTrap.exe` from the [Releases](https://github.com/mel0k1/SakuraTrap/releases) page
2. Run it — the launcher will install Roblox or pick up an existing installation
3. Open settings, go to FastFlags, choose a performance profile

## Building from source

You need Windows and the .NET SDK 6.0 (pinned in `global.json`). The repository has a `wpfui` submodule, so clone recursively.

```sh
git clone --recursive https://github.com/mel0k1/SakuraTrap.git
cd SakuraTrap
dotnet publish Bloxstrap/Bloxstrap.csproj -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

The resulting `SakuraTrap.exe` lands in `Bloxstrap/bin/Release/net6.0-windows/win-x64/publish/`. CI also builds every push — grab the artifact from the Actions page.

## Plans

The roadmap of ideas lives in [ROADMAP.md](ROADMAP.md). Bugs and suggestions go to [Issues](https://github.com/mel0k1/SakuraTrap/issues).

## Credits

- [Fishstrap](https://github.com/fishstrap/fishstrap) — the direct base of this fork (MIT)
- [pizzaboxer](https://github.com/pizzaboxer) and [Bloxstrap](https://github.com/bloxstraplabs/bloxstrap) — the original project (MIT)
- [WPF-UI](https://github.com/lepoco/wpfui) and every library author listed in the in-app Licenses page

## License

[MIT](LICENSE). This project inherits code from Fishstrap (MIT, © returnrqt) and Bloxstrap (MIT, © pizzaboxer) — their notices are preserved in [LICENSE](LICENSE) and [LICENSE.Bloxstrap](LICENSE.Bloxstrap).
