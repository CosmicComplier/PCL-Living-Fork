Run on Windows with SDK 10 and existing real RID build outputs:

```text
python modern/BuildResources/tests/Test-NativeResources.py --main modern/Main/bin/Release/net10.0-windows/win-x64/LivingPCL.dll --dotnet <dotnet>
```

The generator must already be built. Defaults use its Release DLL, the native schema, real Native Main resource items and original fixed vendor assets. Five actual dependency DLLs default to the Main DLL directory; explicit artifact, schema, generator and SDK reference-assembly arguments are supported.

Only a small reader is compiled in a temporary directory. On Windows it targets `net10.0-windows` with `UseWPF`, so WindowsDesktop resolves even for framework-dependent Main outputs without neighboring framework DLLs. It loads the actual managed Main assembly, reads its resource stream and invokes its existing 18 getters under three cultures. It never invokes Main or creates replacement production types. `Program.cs.txt` deliberately avoids the parent generator's recursive C# source glob and is compiled explicitly by the reader project.

On Linux, pass `--defer-windows-getters` explicitly to check actual blob bytes and getter metadata plus all generator cases. It prints the 54 getter calls and Culture/ResourceManager runtime checks as **PENDING**: Main's real assembly attributes require WindowsDesktop, which Linux cannot load. Default mode never suppresses that failure. Run the default command on Windows to complete those checks.

The test compares actual source bytes, all 18 generated and embedded blobs, 54 getters, Culture/ResourceManager, current native pack inventory, vendor-only operation without VB schemas, and eight real CLI failure cases. It never rebuilds or copies Main/the large single-file exe, changes user data or runs UI. Windows launcher interaction remains separate validation.
