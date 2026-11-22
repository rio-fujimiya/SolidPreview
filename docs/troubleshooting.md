# Troubleshooting

- **No preview:** enable Explorer's preview handlers and preview pane (`Alt+P`); check `PASS` output. Competing registered handlers or COM policies may take precedence.
- **No thumbnails:** disable “Always show icons, never thumbnails” in Explorer options. Explorer may retain its own cached icon; try a new copy of a small local model.
- **No orbit / live updates:** check `PASS orbit` / `PASS live` per format. Fallback behavior is expected when the corresponding test fails. A different upstream CLI may require changes.
- **Explorer hangs:** save any ongoing file operations if possible. Stop Explorer and preview host in the current session, then restart Explorer; uninstall the extension if hangs persist. Do not kill all `dllhost.exe` processes indiscriminately.

```powershell
$s = (Get-Process -Id $PID).SessionId
Get-Process explorer,prevhost -ErrorAction SilentlyContinue | Where-Object { $_.SessionId -eq $s } | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Process explorer.exe
```

After uninstalling, test `Install.ps1 -PreviewOnly`, `-ThumbnailOnly`, or `-StaticPreview` separately. Use a small local model first. A sign-out may be needed between modes to unload old COM DLLs.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Collect-Diagnostics.ps1
```

Diagnostic output includes local paths, registry identifiers and recent host logs. Redact usernames, filenames, private paths and any confidential details before sharing. Never upload confidential CAD files or cache images to public issues. Logs can also be read with `Get-Content -Encoding UTF8`.
