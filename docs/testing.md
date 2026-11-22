# Testing and release checklist

## Automated, no installation

On Windows x64 run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tests\Check-Build.ps1
```

This validates PowerShell syntax, compiles using the same framework compiler as the installer, and checks the extension-to-COM metadata against the compiled types. It does not register COM classes or launch SolidCraft. The GitHub workflow runs this check on a Windows runner; no successful run is claimed before it actually executes.

## Manual integration (not covered by CI)

Use a disposable Windows 11 x64 VM or account and an independently installed CLI. Record exact OS build, CLI version, package version and results.

- Fresh install as a standard user; check all applicable PASS messages.
- Preview and thumbnails for every registered format, including small and large models.
- Check `.3dm` (when opted in) with the included synthetic box and representative Rhino models; import support depends on the CLI and the model's geometry.
- Check embedded and absent thumbnails in 3MF and 3DM, including with Rhino unavailable, and confirm the preview pane is unaffected. Explorer may serve an older cached image.
- Resize narrow/wide; orbit in both axes; ViewCube face/edge/corner selection; four 90° arrows at all six face views; Home.
- Wheel zoom in/out, including multiple rapid notches and zoom limits. Middle drag in cardinal, oblique and pole views; confirm the model follows the pointer. Orbit after zoom/pan and confirm scale and target are retained. Each cube face/edge/corner/arrow/Home must reset zoom and target. Check release/idle produces a full-resolution frame. Middle-click without moving must leave the frame unchanged.
- Rapid selection changes, repeated rotations, cache hits and concurrent thumbnail requests; verify no persistent loading indicator or Explorer hang.
- Test static and live fallback, PreviewOnly and ThumbnailOnly; test invalid and oversized models with safe fixtures.
- Update with the BAT; confirm new version loads; uninstall and verify prior handler values and default apps are preserved.
- Check cache removal and no abandoned CLI workers. Do not publish runtime claims for untested cases.

## Release packaging

Keep the source package free of personal paths, local binaries, personal model files, screenshots of confidential data, registry export journals and cached PNGs. Retain the synthetic `tests/box.3dm` fixture used by the installer's 3DM opt-in smoke test. Publish a source release after confirming licensing and the build/integration status. Keep the shell extension and upstream SolidCraft version numbers distinct.

The 3DM fixture is an original 20 × 10 × 5 mm box generated with rhino3dm; neither Rhino nor rhino3dm is required to install or run the extension. Rhino's installed thumbnail handler is required to use embedded 3DM thumbnails.

The source-build CI also verifies camera zoom bounds and pan projection across 60 orientation/aspect combinations; actual mouse input routing and rendering remain manual Windows checks.
