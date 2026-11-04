Run `python modern/BuildEvents/tests/Test-BuildEvents.py --dotnet <SDK10 dotnet>`.

The runner copies the actual task and fixtures into a temporary directory, then checks structured source directives through the production task and builds a nested local-type WPF page with `win-x64`, self-contained runtime and embedded sources. It verifies that the original SDK generated file's SHA stays unchanged and exercises both compiler input channels, Windows filename syntax, repeat projection, handwritten conflicts and empty maps.

The small WPF program is compiled only. This regression makes no claim about Windows UI behavior and writes no product build outputs.
