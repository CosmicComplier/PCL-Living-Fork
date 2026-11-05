# Native resource contract

The C# product embeds five runtime data/native payloads: `Custom`, `ForgeInstaller`, `Help`, `libwebp64`, and `Transformer`. Managed assemblies are supplied by the .NET dependency graph or the single-file bundle, with no second copy in `PCL.Resources`.

The version 2 schema pins the five byte-array keys/getters and records excluded managed keys. `ResourceManager` and `Culture` remain compatible. Native WPF pack assets are read from the real project and retain their separate inventory.

On Windows with SDK 10, run:

```text
python modern/BuildResources/tests/Test-NativeResources.py --main modern/Main/bin/Release/net10.0-windows/win-x64/LivingPCL.dll --dotnet <dotnet>
```

The test checks five generated/Main payloads, 15 real getter calls under three cultures, pack assets, vendor-only inputs and seven actual CLI rejection cases. `--defer-windows-getters` explicitly reports the Windows getter checks pending on Linux. Managed dependency identity and bundled bytes are checked separately by the native smoke/credential gates.
