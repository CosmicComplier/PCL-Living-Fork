# MMC component definition revisions

`MmcInstanceDefinitionChangeBackend<TNetFile>` provides a dedicated managed
preview/commit path. It does not call the ordinary Minecraft/Forge installer.
Existing managed launch and repair reconstruct the newly published source
bundle through `MmcSavedSourceStore`.

`PreviewAsync` fixes the current root, instance ID, generation and primary SHA,
then applies explicit enable/disable, exact-version, user-patch or reset edits
to existing author component UIDs. Required dependencies are resolved through
the existing immutable metadata store. Exact-version replacement explicitly
adopts that exact metadata as a user-selected patch, retaining its remote
snapshot and the original package patch independently. User patches cannot
pretend to change the author manifest or measured author-file baseline.
Unknown UIDs, unsupported active semantics, inconsistent enable/version
requests, incompatible Java and no-op edits return a clear refusal.

The preview exposes effective component/source changes, main class/JAR,
JVM/game arguments, tweakers, classpath, native inputs, Java policy and runtime
environment, plus explicit user-override differences. Its immutable candidate
digest covers the selection, normalized overrides, complete metadata bytes,
new source records, object digests, original-copy references and measured local
input paths/hashes. Local replacements are explicit file sources with a fixed
SHA256; commit never discovers replacement bytes by scanning unknown paths.

`CommitAsync` accepts the selected preview and its fixed digest. It recomputes
that digest, reloads the current saved package, resolves/recompiles the captured
inputs, verifies local source bytes and reuses the existing batched prefetch and
materializer. Only an exact declared digest can reuse an original across a new
metadata identity. Network work occurs before structural admission.

After configuration quiescence and admission, commit revalidates the current
publication, pending/session barriers and private destinations, then actually
captures a complete coverage-3 undo point under that same owner lease. It does
not recursively call a public history task while holding the gate. The fresh
configuration scope retains current preference overlays and unknown keys.

`MmcPlanBinder.BindDefinitionRevision` is a separate explicit Core operation.
It validates the old publication, exact retained-history fact, unchanged author
metadata/baseline and fresh candidate source. Unchanged artifact/asset keys
cannot change adopted bytes. The new current adopted set is freshly measured
and published at N+1. Replaced authority is retained in the complete undo point,
so two revisions of one flat private library can have different bytes without
claiming that both byte sequences currently occupy the same filename. Repair
continues to require the current adopted digests and cannot invoke this policy.

One file transaction publishes the new source/patch objects, compatibility
projection, definition, owned private/derived inputs, fresh manifest and a
`PCL/MmcDefinitionChanges/<operation>.json` provenance receipt. The receipt binds
old/new primary hashes, candidate digest and the actual undo point reference;
it is not an alternate source of execution authority. The host management probe
must retain this directory as evidence if the primary is lost. Old source bundles and CAS objects
are not deleted. New targets cannot overwrite unknown same-name files; removed
known targets must still match their old adopted bytes. Final validation also
covers reused owned inputs, not only files that required mutation.

The minimal regression group exercises custom main/JVM/library component
disable/enable/reset, fresh empty/unknown preference overlays, ordinary managed
repair after modification, exact remote-version/dependency/native resolution,
same-name A-to-B local replacement and complete history undo/redo, offline
preparation after ZIP/global-cache deletion, stale or altered candidates,
unknown target/input rejection, pending/session barriers, cancellation and
controlled transaction interruption before/after the decision. These are
backend/real-file tests; they do not claim GUI, Minecraft or process-kill
acceptance. Root-component creation/removal outside the immutable author
manifest is not an edit operation; new/removable dependency references are
resolved from the edited component declarations.
