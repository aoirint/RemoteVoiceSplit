# Release Pipeline

This design depends on
[GitHub automation dependencies](../domain/github-automation.md) and
[release operations](../operations/release.md).

## Version model

`RemoteVoiceSplit/RemoteVoiceSplit.csproj` is the canonical three-part version.
The helper project must match it. BepInEx metadata and both assembly versions
derive from those project values.

- `0.0.0` is always an edge build. Its artifact identity adds `edge`, UTC
  timestamp, and a short commit SHA; loader metadata remains `0.0.0`.
- A nonzero numeric version is stable only with matching developer and package
  changelog entries and completed runtime checks.
- Prerelease strings are rejected because BepInEx 5 parses plugin metadata as
  `System.Version`.

## Artifact flow

The Pull Request workflow owns `pull_request` and `merge_group`. The Main
workflow owns pushes to `main` and re-runs the same lint and deterministic test
gates on the integrated commit.

The Linux build job restores the locked graph and builds the plugin, helper,
and test harness. It stages the two binaries, generated host runtime
configuration, and five package assets, creates one ZIP, writes
`SHA256SUMS`, validates the final archive, and uploads both files. The release
job downloads that exact artifact and never rebuilds it.

Stable publication is currently disabled. When explicitly enabled for a
validated nonzero version, the pinned release action creates an immutable
GitHub release for the integrated commit and the same ZIP is submitted to
Thunderstore.

## Failure boundaries

Lint, restore, format, build, test, version agreement, classification,
packaging, assembly inspection, archive validation, or checksum failure blocks
artifact upload and publication. Edge builds skip publication.

An existing release for the same tag fails instead of replacing assets.
Recovery is documented in [release operations](../operations/release.md).
