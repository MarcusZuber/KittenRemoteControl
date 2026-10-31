# Verified prebuilt runtime

The files in KittenRemoteControl/ are built against actual installed KSA and StarMap assemblies.
build-info.json records their hashes, source fingerprint and version. They are inputs to package-release.ps1,
not a complete player package: run that script to include Houston, docs, licenses and Borea metadata.

For player installation use the versioned ZIP from artifacts/ or GitHub Releases. See ../RELEASE-PACKAGE.md.
Rebuild with ../build-package.ps1 -RefreshPrebuilt when source changes. Do not release stub builds.
