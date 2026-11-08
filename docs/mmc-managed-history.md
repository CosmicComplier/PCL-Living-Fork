# MMC managed environment history

`MmcInstanceHistoryBackend` is the dedicated managed backend. The ordinary
`EnvironmentHistoryEngine` and its coverage versions 1/2 remain unchanged.
This backend accepts coverage **3** only and does not partially restore old
version JSON, Setup.ini or config.json into a managed instance.

## Public contract

- `CaptureAsync` fixes the current instance ID, generation and primary SHA256.
  It returns a point ID and its manifest SHA256.
- `ListAsync` reads the append-only point index and bounded hash-bound manifests.
  Listing is not a declaration that every payload is currently healthy.
- `RestoreAsync` fixes both the current publication and the selected point
  reference. It fully verifies all point payloads before changing live files.

Points live under `PCL/MmcHistory`. Each exclusive GUID directory is private
until a flushed index record is atomically appended. A point manifest cannot
authorize itself: restore also checks the separately published index and the
caller's fixed manifest digest. An interrupted unindexed directory is retained
and cannot be listed or restored. No external point path is accepted.

## Coverage and publication

A complete point contains the published definition, exact metadata and user
patch objects, measured author baseline, package opaque originals, and all
adopted artifact/asset CAS originals, including historical lock references.
Missing or damaged adopted originals refuse capture. Derived runtime views
are rebuilt, so their absence does not require incomplete snapshots.

The active game tree uses the existing coverage-2 environment whitelist for
mods, coremods, configuration, scripts and resources. Worlds, screenshots,
map markers, known launcher account files, global accounts and live launcher
config/Setup preferences are excluded. Game configuration remains game
configuration; this is a local private history store, not an export of a
sanitized public pack. The alternate game tree is neither merged nor restored.
Both trees' current player data remain at their current paths.

Restore first publishes a complete undo point for the current environment.
Under configuration quiescence and the shared admission lease it revalidates
the current primary, rejects active/uncertain sessions and pending operations,
and reconstructs the selected source and runtime without network access.
`MmcPlanBinder.BindRestore` verifies the selected publication as its own prior
authority, keeps the current ID and publishes `current generation + 1`.
Foreign identities, changed tree roles and future-generation points refuse.
The displaced source and lock bindings remain in the undo point; no original
objects are garbage-collected by restoration.

One file transaction covers the new definition, compatibility projection,
manifest, restored original/private inputs, environment file differences and
restoration provenance receipt. The configuration write scope starts from
fresh current disk and accepted preference overlays. Current Java/JVM/memory/
window values, explicit empty overrides and unknown preference keys survive.
Unknown modified private libraries refuse restoration rather than becoming
new adopted bytes. A selected point also cannot overwrite an unowned
same-name library with different bytes, including when an earlier restore
removed that path's current authority. Identical selected bytes can be reused
and missing targets rebuilt. Unknown unowned library neighbors remain untouched.

Cancellation before the commit decision rolls back; cancellation afterward
finishes the decided publication. The normal file-transaction recovery API
handles controlled interruption before/after the decision. No separate
history-specific commit decision is introduced.

## Validation and limits

`MmcInstanceHistoryTests.Cases` exercises Java8 G1 capture, Java21 G2, restore
to Java8 G3 and undo to Java21 G4, offline launch-input preparation, both game
trees' worlds, current map markers and fresh empty/unknown preference overlays.
It also checks damaged point/CAS input refusal, fixed index references, session
and pending barriers, unsupported coverage, foreign/future definitions,
predecision cancellation, postdecision cancellation and controlled interrupted
transaction recovery on both sides of the decision. These are backend tests,
not Minecraft/GUI or actual process-kill acceptance.
An additional real Core definition-update/transaction case creates two same-ID
points with different private-library inventories. Restoring the earlier point
and placing an unknown library at the removed path makes the later restore
refuse with the current bytes and generation unchanged; matching-byte reuse
and missing-target rebuild still succeed.

The current copy store is bounded to 100000 files and 64 GiB per point and
keeps every published point immutable through this API. It streams files and
does not hardlink player files. Cross-point content deduplication, retention,
orphan cleanup, UI routing and actual host-process-kill acceptance are separate
integration work; no completion of those capabilities is implied here.
