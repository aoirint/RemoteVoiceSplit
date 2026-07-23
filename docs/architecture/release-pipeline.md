# Release Pipeline

This design depends on
[GitHub automation dependencies](../domain/github-automation.md) and
[release operations](../operations/release.md).

## Version model

`RemoteVoiceSplit/RemoteVoiceSplit.csproj` is the canonical SemVer release
identity. The helper project must match it. Both assembly versions, BepInEx
metadata, the artifact name, and the Git tag derive from those project values.

- `0.0.0` is always an edge build. Its artifact identity adds `edge`, UTC
  timestamp, and a short commit SHA; loader metadata remains `0.0.0`.
- A SemVer prerelease such as `0.1.0-alpha.2` creates a GitHub prerelease. Its
  assembly versions use the numeric core, while BepInEx metadata and the
  Thunderstore manifest remain `0.0.0` because neither consumer accepts the
  prerelease identity.
- A nonzero numeric version is stable only with matching developer and package
  changelog entries and completed runtime checks.

## Artifact flow

The Pull Request workflow owns `pull_request` and `merge_group`. The Main
workflow owns pushes to `main` and re-runs the same lint and deterministic test
gates on the integrated commit.

The Linux build job restores the locked graph and builds the plugin, helper,
and test harness. It stages the two binaries, generated host runtime
configuration, and five package assets, creates one ZIP, writes
`SHA256SUMS`, validates the final archive, and uploads both files. The release
job downloads that exact artifact and never rebuilds it.

For a prerelease, the pinned release action creates an immutable GitHub
prerelease for the integrated commit and attaches only the validated ZIP.
Stable GitHub and Thunderstore publication remain disabled until separately
authorized after runtime validation.

## Failure boundaries

Lint, restore, format, build, test, version agreement, classification,
packaging, assembly inspection, archive validation, or checksum failure blocks
artifact upload and publication. Edge builds skip publication.

An existing release for the same tag fails instead of replacing assets. Edge
and stable builds currently skip publication. Recovery is documented in
[release operations](../operations/release.md).
