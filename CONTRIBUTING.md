# Contributing

Keep diagnosis, filtering, and system maintenance separate. Describe the observable problem before proposing a change.

## Report a problem

Use a [GitHub issue](https://github.com/Prathmesh333/KeyboardStabilizer/issues) for a reproducible problem. Include:

- Windows version and application build or commit.
- Keyboard model, connection type, and selected key.
- Preview or live mode, release grace, and tap interval.
- Physical and filtered counts.
- Whether the same keyboard fails in another application or on another computer.
- The smallest sequence that reproduces the problem.

Remove private information from screenshots and logs. Do not attach startup backups, credentials, or crash dumps that contain private data.

## Change the code

Preserve live output off by default, explicit target selection, released-key activation, and stop cleanup. Keep timing behavior in the pure engine where practical.

Add a meaningful regression check when a change affects state transitions or cleanup. Do not infer hardware repair from a passing unit test.

Run both suites before you submit a change:

```powershell
.\Build-Stabilizer.ps1 -Test -OutputDirectory .\artifacts\contribution
.\Build.ps1 -Test -OutputDirectory .\artifacts\contribution
```

The native smoke check is optional and requires a desktop with F8 available. Do not run startup Apply or Restore as part of an automated test.

## Change the documentation

Use the project terminology in the [documentation index](docs/README.md). Preserve interface labels, timing values, conditions, and stop criteria. Use explicit conditions and one principal action per procedural step.

Document compatibility observations with their environment and limits. Do not claim Riot approval, exact latency, or hardware repair without supporting evidence.

## License

Contributions are distributed under the repository's [MIT license](LICENSE). Do not copy third-party code or documentation without the required rights and notices.
