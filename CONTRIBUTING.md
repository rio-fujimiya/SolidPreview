# Contributing

Keep the per-user registration, rollback semantics and shell process isolation intact. Use the existing Windows PowerShell 5.1 / .NET Framework C# compiler syntax; do not introduce a runtime or dependency without documenting it. Preserve COM class IDs for compatible releases. Run `tests/Check-Build.ps1` and the relevant [manual checks](docs/testing.md). Never include proprietary models, personal logs or credentials in tests or issue reports. Use small synthetic fixtures. Update documentation and changelog when behavior changes.

Contributions are made under the project's [MIT License](LICENSE). Before submitting code or assets, ensure you have the right to contribute them and disclose third-party material and applicable licenses.
