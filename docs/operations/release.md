# Release Operations

## Scope

Pull Request and Main workflows build and validate the source. GitHub
prerelease publication is enabled for SemVer prerelease versions. The first
Thunderstore publication is planned as a clearly labeled public beta. Stable
release approval remains disabled until the complete runtime matrix passes.

The project version is currently `0.1.0-alpha.4`. The Main workflow creates a
validated package artifact and immutable GitHub prerelease. BepInEx metadata
and the Thunderstore manifest use `0.0.0` placeholders, and no Thunderstore
upload occurs.

The maintainer has confirmed that the `THUNDERSTORE_TOKEN` repository secret is
configured. Its value must never be copied into documentation, logs, workflow
inputs, or release evidence. The current workflow still lacks an authorized
numeric-beta publication path, so this documentation change cannot upload a
package.

Version classification, staging, ZIP creation, and checksums belong to
`.github/workflows/main.yml`. Assembly and archive validation belongs to the
test project. APM manages repository-local Agent Skills and is not a mod
package host.

## Archive contract

The ZIP root contains exactly:

```text
RemoteVoiceSplit.dll
RemoteVoiceSplit.AudioHost.exe
RemoteVoiceSplit.AudioHost.exe.config
manifest.json
icon.png
README.md
CHANGELOG.md
LICENSE
```

Both binaries must be valid managed assemblies with the expected identity and
numeric-core version. The plugin must also contain exactly one matching
`BepInPlugin` and `BepInProcess` attribute. For a prerelease, the plugin
attribute version is `0.0.0`.

All entries must be regular root files with safe names. The validator rejects
absolute paths, traversal, backslashes, links, duplicates, unexpected files,
additional binaries, excessive entry or expanded size, extreme compression
ratios, invalid metadata, incomplete package prose, and an empty license.
Mutation fixtures exercise these rejection branches.

## Edge artifacts

A push to `main`:

1. runs the shared lint and deterministic test gates;
2. classifies `0.0.0` as
   `0.0.0-edge.<UTC timestamp>.<commit>`;
3. builds both managed binaries from the locked graph;
4. stages only the eight allowed files and creates the ZIP;
5. validates the completed ZIP with the same test project;
6. writes and verifies `SHA256SUMS`; and
7. uploads both files as a short-lived workflow artifact.

Packaging is CI-owned. Download an edge artifact from the successful Main
workflow and verify `SHA256SUMS` before extraction.

## Public beta publication gates

Before enabling the first Thunderstore beta:

1. Install the exact validated ZIP through a clean Lethal Company v81 BepInEx
   profile.
2. Run a two-player smoke test that confirms the plugin loads, the helper
   starts outside the game process tree, remote voice reaches the separate
   process, game audio remains in `Lethal Company.exe`, and shutdown leaves no
   helper process behind.
3. Use a nonzero three-part numeric package version. Thunderstore does not
   accept prerelease suffixes, so the numeric version is an artifact identity,
   not a stable-quality claim.
4. Put the beta notice at the start of the packaged `README.md`, in the
   manifest description, and in both versioned changelog entries.
5. Confirm the Thunderstore namespace, Lethal Company community, and selected
   categories. Confirm the configured token can publish for that namespace
   without exposing its value.
6. Review and enable a workflow path that marks the GitHub release as a
   prerelease, submits the same verified ZIP to Thunderstore, and remains
   limited to the explicitly selected numeric beta version.
7. Run all development, workflow, package, dependency, and documentation
   checks on the exact commit.

Do not upload the current `0.1.0-alpha.4` artifact or rewrite it as a numeric
version after the build. The project versions, generated manifest, changelog
entries, binary metadata, archive name, and release tag must agree before the
ZIP is built.

## Stable release runtime checks

Before describing a release as stable, use the exact Lethal Company v81 build
and a clean BepInEx 5 profile:

- confirm the plugin loads without reflection or patch errors;
- confirm the helper starts outside the Lethal Company process tree;
- confirm its window appears as `Lethal Company Remote Voice Split` in OBS;
- with two players, confirm only the remote player's processed voice moves to
  the helper process while game audio remains in `Lethal Company.exe`;
- record the two sources to separate tracks and inspect the resulting file for
  missing, duplicated, or cross-contaminated audio;
- repeat as lobby host and non-host, and cover alive, dead, spectating, and
  walkie-talkie voice paths;
- close and restart the helper, change the Windows default endpoint, and
  disconnect an endpoint; remote voice must become silent by default and
  recover without stale replay or a game crash;
- repeat the failure checks with
  `General.FallbackToGameOutput` enabled; remote voice must
  remain on Unity output and recover;
- toggle `General.FallbackToGameOutput` through a BepInEx configuration UI while
  routing is unavailable; the next remote-voice block must change paths
  without a restart;
- toggle `General.Enabled` through a BepInEx configuration UI while routing is
  ready; disabled voice must remain on Unity output and re-enabled voice must
  resume separate routing without a restart; and
- exit the game and confirm the helper and its audio session stop.

Also verify BepInExPack installation, Thunderstore packaging, OBS monitoring,
and coexistence with the intended release mod set. Record failures with enough
context to reproduce them. After the public beta gates pass, unresolved items
in this broader matrix may remain documented beta limitations. They must all
be closed before stable-release approval.

## Prepare a GitHub prerelease

1. Add a complete SemVer prerelease section to `CHANGELOG.md`.
2. Add the same version to `assets/CHANGELOG.md`.
3. Set `Version` in both project files to that prerelease version.
4. Record pending runtime validation in both changelogs.
5. Run all development, workflow, package, dependency, and documentation
   checks.
6. Push the reviewed commit to `main`.

CI keeps the prerelease identity in assembly metadata, the ZIP name, tag, and
GitHub Release. It supplies `0.0.0` only to the BepInEx plugin attribute and
Thunderstore manifest, then publishes the validated ZIP to GitHub as a
prerelease. Thunderstore is not contacted.

## Prepare a stable release

1. Complete the stable release runtime checks for the exact commit.
2. Add matching versioned entries to both changelogs.
3. Set `Version` in `RemoteVoiceSplit/RemoteVoiceSplit.csproj` and
   `RemoteVoiceSplit.AudioHost/RemoteVoiceSplit.AudioHost.csproj` to the same
   nonzero three-part version.
4. Confirm repository settings and immutable Releases.
5. Confirm the namespace, community, categories, and continued availability of
   the `THUNDERSTORE_TOKEN` secret without exposing its value.
6. Replace the beta notices with stable-release wording.
7. Explicitly enable stable GitHub and Thunderstore publication.
8. Run all development, workflow, package, dependency, and documentation
   checks.
9. Push the reviewed commit to `main`.

The release job never rebuilds the ZIP. It publishes the exact verified
artifact to GitHub and then Thunderstore.

## Recovery

Validation failures create no release. Never replace a published tag, release,
or ZIP.

If GitHub fails while creating an unpublished draft, inspect it before
deleting any workflow-created draft or tag. If GitHub publication succeeds but
Thunderstore fails, first check whether the version already exists. Preserve
the published ZIP and use a separately reviewed Thunderstore-only recovery
path instead of rerunning immutable GitHub publication.
