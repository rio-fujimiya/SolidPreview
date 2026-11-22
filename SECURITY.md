# Security

This is experimental software that registers managed COM shell handlers and invokes a separately installed native CAD parser on selected files. Treat CAD files as potentially untrusted. Use a current CLI from a trusted source; do not disable Windows process isolation or low-integrity protections. Prefer a disposable VM for testing. A malicious or malformed file may still cause crashes or resource exhaustion.

The installer writes per-user COM registrations and a rollback journal, while the renderer writes local model copies, generated command scripts, PNG cache and logs. Do not share these artifacts without reviewing them. The uninstaller can remove the extension cache with `-ClearCache`; Windows may keep its own thumbnail cache.

Report vulnerabilities privately via GitHub's **Report a vulnerability** feature if the maintainer has enabled private vulnerability reporting. Otherwise, open an issue requesting a private contact channel without disclosing exploit details or sensitive data. Include version, affected component, impact and safe reproduction steps. No guaranteed response time or supported release commitment is made yet.
