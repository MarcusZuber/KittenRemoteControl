# Local release handoff

Prepared version: `0.1.0-borea.2`; intended tag: `v0.1.0-borea.2`.

1. Run the build command in RELEASE-PACKAGE.md with `-RefreshPrebuilt`, inspect the package and perform the Borea smoke test.
2. Commit source, release docs and refreshed `dist/KittenRemoteControl` files together. Keep the version unchanged only while the release is unpublished.
3. When ready to publish, push the reviewed branch. Create the tag on the tested commit and a GitHub **prerelease** targeting that tag. Attach only the versioned mod ZIP and its SHA-256 file. Use RELEASE-NOTES.md as the description. Do not upload proprietary build inputs, scratch folders or a source-tree ZIP as the install asset.
4. Publish the Ahwoo forum post using FORUM-POST.md, then insert its real URL into the Borea listing template. No forum URL has been invented.
5. Submit the completed listing as `listings/KittenRemoteControl.toml` to https://github.com/KSAModding/content-index. Borea's List or change your mod flow can also help prepare it. The index validates ownership and the archive before listing it. Prereleases may require players to opt into prerelease content.

The template follows the current authored schema and uses the existing mod id, known game minimum and StarMap minimum. Its missing forum URL deliberately keeps it a draft. No public release or catalogue submission is performed by these scripts. The GitHub workflow validates a prebuilt runtime tied to the source, and uploads a package artifact; it does not publish automatically.

Sources checked on 2026-10-31:
- https://github.com/KSAModding/content-index/blob/main/CONTRIBUTING.md
- https://github.com/KSAModding/content-index/blob/main/schemas/authored.schema.json
- https://github.com/KSAModding/content-index/blob/main/listings/kRPC.toml
