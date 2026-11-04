# Native LF host sources

The product starts from LF `f4aa0fae62c3ee8af24c81e61844c8fdd13c8983`, PCL foundation `5ef81e490ae46b1100d61e33d0183094c7a80ca8`, and LF migration source checkpoint `a1d2ee1ed215647ab7e38f11c9e45859fbb4fcf7`. LF remains the application base. Its original `LICENCE`, branding, WPF names, resources and user-data contracts remain authoritative.

CE stable `1a7653127d578efdb2a9388999a3fdc67d9e49d5` was reviewed for C# WPF/My/project migration experience; no CE product base, profile store, new component rules or policies replace LF. CE application paths use their directory `LICENCE`; Core paths use its root Apache license. Per-module selection is recorded in `docs/migration/MODULE_MAP.md`.

`modern/Presentation/NativeWpf` selectively adapts these actual fixed LF submodule sources at `MeloongCore` commit `c774ae4c85fd94e0eaa171db21e78a65870df19b`: `Wpf/Main.cs`, `Wpf/Controls/ModernTooltipService.cs`, `Wpf/General/Dialogs.cs`, `Wpf/Extensions/WpfExtensions.cs`, and `Wpf/Utils/WindowsUtils.cs`. The four old CLR namespace converter aliases delegate to the existing Presentation converters, preserving one implementation. See `modern/ThirdParty/MeloongCore-Wpf/LICENSE.txt` (Apache-2.0); original individual attributions are retained. The retired net48 WPF implementation DLL is not a native product reference or embedded payload.

The read-only native bundle identity reader adapts the actual .NET 10.0.14 bundle format at dotnet/runtime `6aa72daa24f66c01c3d7b6a32519e9d21396deec`; exact public paths, hashes and MIT license are in `modern/ThirdParty/dotnet-runtime-bundle`. The reader does not extract or execute candidates. LF managed identity/hash/rollback and old four-argument update handoff remain the baseline.

The complete typed `NativeHandles` sources and `modern/BuildEvents` preserve original VB named-part replacement/override/event ordering. WPF's original generated files are retained; only compilation inputs receive the declared-field projection. Original implicit XAML initialization and handler ordering are part of the migration contract, not permission to remove behavior.

This source checkpoint is not complete Windows acceptance. Remaining LF main source synchronization, inactive conditional features, actual startup/close, accounts, game launch, native publication and update compatibility are tracked in `docs/MIGRATION_STATUS.md`. No new MMC/Prism rules, performance work, Rust or C++ modules are authorized by this checkpoint.
