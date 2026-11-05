Run `python modern/BuildEvents/tests/Test-BuildEvents.py --dotnet <SDK10 dotnet>`.

The runner copies the actual task and fixtures into a temporary directory, then checks structured source directives through the production task and builds a nested local-type WPF page with `win-x64`, self-contained runtime and embedded sources. It verifies that the original SDK generated file's SHA stays unchanged and exercises both compiler input channels, Windows filename syntax, repeat projection, handwritten conflicts and empty maps.

The small WPF program is compiled only. This regression makes no claim about Windows UI behavior and writes no product build outputs.

The same runner also builds the actual helper and WPF pass2 repeatedly with strict CI, publish-like `DebugType=none`/`DebugSymbols=false`, optional-source and direct Debug callers. Its SHA **and last-write time** must stay unchanged: the host tool always uses Release, portable symbols and `ContinuousIntegrationBuild=true`. Every build uses `-nr:false` to release that process's worker nodes. Run this sequence on Windows to verify that previously loaded task DLLs do not cause file-lock/copy failures; Linux checks the property and incremental-build behavior only.
