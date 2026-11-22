# SolidPreview

**CAD previews and thumbnails in Windows File Explorer, powered by SolidCraft.**
![demo_animation](animation_f30.0.gif)

[日本語](README.ja.md) · [Architecture](docs/architecture.md) · [Troubleshooting](docs/troubleshooting.md) · [Changelog](CHANGELOG.md)

> Experimental, unofficial Windows shell extension. Not affiliated with or endorsed by SolidCraft or any CAD vendor.

## Features

- Explorer preview pane (`Alt+P`) and thumbnail icons for CAD and mesh files.
- Interactive orientation cube: select faces, edges and corners for 26 standard directions; at face views, arrows navigate to neighboring faces in 90° steps.
- Mouse-drag orbit, wheel zoom, middle-button pan, Home button and adaptive preview resolution (up to 2048 px on the long edge; 960 px while dragging).
- Shared PNG disk cache; a private persistent headless SolidCraft MCP session when supported, with CLI snapshot fallback.
- Existing 3MF package thumbnails are displayed without rendering a replacement. For 3DM, the installed Rhino thumbnail handler is used when available; otherwise SolidCraft renders the thumbnail.
- Per-user setup and registry rollback; no change to default applications.

## Requirements

- Windows 11 x64 (ARM64 and other versions are not validated).
- 64-bit Windows PowerShell 5.1 and .NET Framework 4.x with WPF and the built-in C# compiler.
- A working `solidcraft-cli.exe`, installed separately from [SolidCraft](https://docs.solidcraft.io/). CLI 0.45.0 was used during early user testing.

The package contains source and scripts, not prebuilt binaries. Review them before running. No administrator rights should be needed under ordinary per-user COM policy. Installing changes which preview/thumbnail handlers Explorer uses; it may affect Explorer stability.

## Install

Download the source ZIP from the repository (Code → Download ZIP), extract it and start SolidCraft so the installer can locate its CLI. In a **non-elevated 64-bit Windows PowerShell** window opened in the extracted directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1
```

If CLI discovery fails, enter its full path when prompted, or specify it directly:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Install.ps1 -Cli "C:\Program Files\SolidCraft\solidcraft-cli.exe"
```

The path above is an example. The installer compiles the DLL locally, runs format/render smoke tests, and registers only passing formats. `PASS orbit` and `PASS live` indicate optional capabilities, not complete Explorer integration tests. `-ExecutionPolicy Bypass` applies only to the launched PowerShell process; it does not change the system-wide policy.

## Usage

Select a model in Explorer and press `Alt+P` to show the preview pane. Use Large or Extra large icons to see thumbnails. Drag with the left or right mouse button to orbit; click the orientation cube or its arrows to navigate. Scroll the wheel to zoom, and hold the middle mouse button while dragging to pan. Orbit preserves the current zoom and world-space pan target. Every ViewCube face, edge, corner, arrow or Home click resets zoom and pan; Home returns to ISO. Double-click the model to reset to ISO. Drag updates are best-effort, not a guaranteed real-time frame rate.

| Format | Extensions | Setup |
| --- | --- | --- |
| STEP | `.stp`, `.step` | Default |
| Mesh | `.stl`, `.obj`, `.3mf` | Default |
| SolidCraft | `.sc`, `.solidcraft` | Default |
| Rhino | `.3dm` | Opt-in; requires a CLI with 3DM import support |
| IGES | `.igs`, `.iges` | Opt-in; supply a known-good sample |

For 3DM the installer checks rendering with the included synthetic box; `-SampleFiles` can override it with your own `.3dm` file. Import coverage depends on the SolidCraft version and model contents; files without supported geometry may not render.

For 3MF files without a package thumbnail, and 3DM files when Rhino's thumbnail handler returns no image or is unavailable, SolidPreview renders a thumbnail as before. Invalid or oversized 3MF thumbnails are reported in the host log instead of being replaced. The preview pane remains independent of embedded thumbnails. Explorer may retain previously cached thumbnails.

For the opt-in formats, pass arrays through PowerShell’s `-Command` option:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '.\Install.ps1' -Extensions @('.stp','.3dm','.igs') -SampleFiles @('C:\Models\sample.igs')"
```

## Update and uninstall

Extract the new source package and run `Update-SolidPreview.bat`. This stops Explorer and the preview host in the current session, rolls back prior registrations, installs the new version and restarts Explorer even on reported failure. Close file transfers first; Explorer windows may close. A sign-out may still be needed if a COM surrogate holds an older DLL. For non-default extensions/options, reapply your preferred install flags using PowerShell.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Uninstall.ps1
```

Add `-ClearCache` to delete the extension's PNG cache. After unloading the hosts, you may remove `%LOCALAPPDATA%\SolidPreview` if no longer needed. The uninstaller restores recorded registry values only if they still match the values installed by this extension.

## Development and status

`Handler.cs` implements the COM handlers and preview UI. `formats.json` maps extensions to COM classes. `Install.ps1`, `Uninstall.ps1` and the update BAT are kept at repository root for direct use. `tests/Check-Build.ps1` compiles and checks registration metadata without installing the extension. The Windows CI workflow is configured but has not yet been run on GitHub. It does not exercise Explorer or SolidCraft rendering.

Early versions were exercised on Windows 11 with CLI 0.45.0 during development. The final 1.0.18 source package and subsequent repository packaging have not been independently Windows integration-tested. See [manual checklist](docs/testing.md) before publishing a release.

## Privacy and security

Rendering uses a locally installed external CLI and a local, per-user cache. The source contains no telemetry or network upload feature. The CLI is a separate dependency and is responsible for its own behavior. Cached images may reveal sensitive CAD designs; avoid sharing them. Do not disable Windows preview isolation to work around failures. See [security policy](SECURITY.md).

## License and attribution

SolidPreview is licensed under the [MIT License](LICENSE). SolidCraft is a separately installed dependency with its own licensing terms; see [licensing](docs/licensing.md) and [third-party notices](THIRD_PARTY_NOTICES.md).
