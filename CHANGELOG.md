# Changelog

Historical source-package notes, in reverse version order. These describe changes, not proof of Windows integration testing.

## v1.0.18

- Mouse-wheel zoom (1.2x per notch, bounded to 0.2–12x) and middle-button drag to pan.
- Orbit retains zoom and the world-space target; all ViewCube navigation buttons reset framing. Middle-click alone no longer resets the view.
- Interactive frames use the existing 960px limit; release or wheel idle triggers a final frame up to 2048px. Camera and resolution remain part of the cache key.
- Added camera math regression checks to Windows CI. Windows runtime/build verification is still required.

## v1.0.17

at cardinal face views, four inward-pointing arrow
 buttons appear around the ViewCube. Each selects the adjacent face at 90
 degrees based on screen direction. Pole transitions retain the departing
 heading. Arrows have independent hover and hit testing; diagonal views
 hide them. Existing Home, face/edge/corner navigation and rendering unchanged.
 Windows runtime/build verification is still required.

## v1.0.16

Home is overlaid inside the ViewCube square at its
 upper-right corner, with priority hit testing. Cube projection is adjusted
 on small panes so Home does not cover orientation regions. Drag render
 long-edge limit increased from 640 to 960 pixels; final limit remains 2048.
 Existing aspect-ratio sizing, thumbnail provider and update BAT unchanged.
 Windows build/runtime verification is still required.

## v1.0.15

corrected LEFT/RIGHT ViewCube face labels; camera
 direction mapping and drag behavior unchanged. Includes v1.0.14 adaptive
 preview sizing, Home button, and one-click update script.

## v1.0.14

- Preview renders to pane aspect ratio, maximum long edge 2048 pixels (640 during drag).
 - Resizing reuses the last image immediately and requests a new render after 200 ms of stable size.
 - Orthographic orbit camera distance accounts for narrow aspect ratios; size is part of cache key.
 - ViewCube Home icon at upper right resets to the initial ISO direction.
 - Existing thumbnail renderer and cache are unchanged.
 - The installer verifies a 320x480 snapshot. Update-SolidPreview.bat runs the update and restarts Explorer/prevhost.
 - The first image can come from the legacy ISO cache before the adaptive frame arrives.

## v1.0.13

reversed horizontal mouse-drag yaw direction only. Vertical drag and ViewCube navigation unchanged.

## v1.0.12

replaced the entire bunny/button panel with a single
 orientation ViewCube, overlaid in the upper right of the preview. The displayed
 cube follows the latest successfully displayed model camera, not a pending
 request. Visible faces have clickable center, edge strips and corner regions
 (26 distinct directions across the cube); a shared edge/corner highlights on
 all its visible faces. Hidden directions become reachable by selecting adjacent
 edges/corners; no separate buttons, pages or tabs. Existing mouse drag outside
 the cube and middle-click/double-click ISO reset are retained. All geometry
 and hit testing are local to the dedicated preview UI thread. No icon resource
 or additional dependencies. Existing renderer, cache and thumbnail unchanged.
 Windows runtime/build verification is still required.

## v1.0.11

all 26 views plus ISO are directly clickable at all times
 when orbit is enabled and the pane can physically fit the controls. Three
 3x3 compass groups are laid out responsively, in 1, 2 or 3 columns, docked
 below or to the right of the model. Upper/middle/lower visual markers are
 noninteractive, not tabs. Buttons grow to 46 bitmap pixels and shrink for
 small panes. The embedded BunnyViews.png atlas is generated from an original
 stylized 3D bunny and the same camera angles as ViewAngles; no external assets
 or rendering calls are needed for the controls. Selection is highlighted.
 Thumbnail provider, render scheduling, cache, and COM lifetime are unchanged.
 Windows build and runtime verification are still required.

## v1.0.10

compact graphical navigation instead of text buttons.
 The left column selects upper, horizontal or lower elevation (three stacked
 planes, selected plane highlighted). The right 3x3 pad shows projected cube
 icons for each direction; the middle cell is Top / ISO home / Bottom.
 The selected view is highlighted blue. Cube icons use the same angles as
 the actual render requests. Opposite faces have different shading.
 Panel size is at most 121 x 90 bitmap pixels, with no text labels.
 All 26 directions, ISO home, mouse dragging, cache and thumbnail behavior
 are retained. No new native windows or dependencies.
 Windows compile/runtime verification is still required.

## v1.0.9

click navigation in the preview pane, with three elevation
 layers (Above / Level / Below), each a 3x3 compass pad. BL/BR = back-left/right;
 FL/FR = front-left/right. Level contains 8 horizontal views and ISO (home);
 Above/Below each contain 8 elevated/lowered views and Top/Bottom respectively.
 In total: 6 faces, 12 edge directions, 8 corner directions, and ISO home.
 Elevations are 0, +/-45 (edge), +/-atan(1/sqrt(2)) (corner) and +/-90 (poles).
 Yaw convention: front 0, left +90, right -90, back 180; +Z up.
 Clicks request a full-resolution frame; most recent button selection takes
 precedence over previous requests. Existing final-frame cache is reused.
 Drag remains available outside the panel. The panel is hidden below 90x100 px
 or when orbit support is disabled. The controls are drawn inside the existing
 bitmap, using the existing dedicated UI thread and hit-testing (no new COM or
 cross-thread child-window dependencies). Thumbnail provider is unchanged.
 Windows compile/runtime verification is still required.

## v1.0.8

continuous latest-angle rendering while dragging (no idle
 debounce), with a private persistent solidcraft-cli mcp --headless session
 per preview. The model is preloaded after the first cached image is shown.
 Screenshots use 512x320 during drag and 1024x640 at release; final frames
 use a separate bounded disk cache. Drag frames are temporary, and only one
 outstanding request per preview is allowed. The installer tests two distinct
 visible camera images for each format and reports PASS live; formats that
 fail use the old one-shot snapshot renderer but continue updating during drag.
 The existing static/thumbnail cache and COM/UI architecture are unchanged.
 Log: Live MCP ready, Live orbit preloaded, Live frame ... ms, or Live orbit
 fallback. Actual FPS depends on model and GPU; no fixed 60 fps guarantee.
 Windows compile and runtime tests are not available in this environment.

## v1.0.7

the user's log returned Cache hit without entering the image
publication callback. The preview session now runs a dedicated STA/UI thread
with its own Win32 message pump, polling worker completion without WM_TIMER.
COM calls never wait for this UI thread. SetWindow/SetRect enqueue geometry;
Unload signals the session to exit and release its HWND and bitmap. Thumbnail
provider, cache keys and camera contract are unchanged. Expected log sequence:
Preview UI thread start v1.0.7 -> Preview UI child created -> Preview UI pump
started -> Preview render worker leave -> Preview image loading -> Preview
image displayed -> Orbit enabled=True. If no frame appears, run
Collect-Diagnostics.ps1. Windows runtime and compilation are not verified here.

## v1.0.6

restore the plain STATIC child and first-frame timer path from
 v1.0.3. Orbit input is installed only after the initial image is displayed.
 STATIC hit testing is handled by the orbit subclass, without SS_NOTIFY.
 Idle polling stops; mouse motion re-arms the timer. Initial rendering errors
 produce a visible error card where possible. New trace landmarks: Preview
 enter v1.0.6, Preview render worker enter/leave, Preview timer armed/first
 tick, Preview STM_SETIMAGE begin/end, Orbit subclass begin/end.
 No changes to thumbnail rendering or PNG cache keys. Windows integration
 has not been tested. Collect-Diagnostics.ps1 reports registered CodeBase and
 recent host logs without changing any settings.

## v1.0.5

complete camera JSON (target, distance, yaw, pitch, fov).
 The installer and preview share the same eval JSON bounding-box parser. Each
 format is separately tested for orbit; static preview and thumbnails remain
 available if its orbit test fails. First orbit for a file evaluates the model
 once in the background; subsequent rotations reuse those bounds while selected.
 Bounds are combined from the bbox entries in eval's JSON; hidden geometry may
 affect framing. A model with no usable bbox cannot orbit and the last image
 stays visible. Orbit cache namespace v2; static thumbnail cache unchanged.
 If needed, inspect logs for Orbit eval started, Orbit bounds ready and Orbit CLI.
 Windows/CLI 0.45.0 runtime has not been tested in this environment.

## v1.0.4

left/right drag in preview changes yaw/pitch; middle click resets
 to the original cached iso view. Actual 3D camera snapshots, not rotating a PNG.
 A 150 ms pause or release submits the latest camera; one outstanding render per
 preview, no UI thread wait, and 1-degree cache keys. The next frame is displayed
 when the CLI finishes (not continuous 60 fps). The input copy is retained only
 while the preview is open; unload cancels and defers deletion until the task
 completes. Thumbnail camera/cache keys are unchanged. If the installed CLI
 fails the two-angle smoke test, orbit is disabled and static preview remains.
 Install.ps1 -StaticPreview can also disable orbit deliberately. Log entries:
 Orbit request, CLI exit, Preview image displayed, or Preview display error.
 Windows compile and integration still need verification.

## v1.0.3

cache publication tolerates an existing valid PNG; invalid
existing cache files can be atomically replaced. Thumbnails return explicit 32-bit
DIB sections with WTSAT_RGB. Additional COM entry and DIB tracing is included.
Read logs with Get-Content -Encoding UTF8. No confirmed hang fix yet.
