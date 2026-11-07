# Beta 1 validation

Checked 2026-11-02. Packaging checks passed; a fresh Beta-labelled install has not yet been played on the physical handheld.

- Public release ZIP: CRC check, required paths, launcher/runtime executable modes and redistributable-file SHA256 checks passed. A deliberately damaged FNA file was detected; editable configuration remained editable.
- Clean original Linux assembly: local preparation produced SHA256 `202a7e4ffbd1f0f97c124262bc38f758d96bc0e92e6a4bb91ba713f0261a5886`, byte-for-byte identical to the working Alpha1.5 assembly. The original was backed up.
- Repeated preparation and existing Alpha migration passed without duplicate hooks. Missing Content and unsupported input failed without modifying the user's input.
- Assembly smoke test using the build-machine .NET runtime loaded all 775 game types. The FNA/FMOD member-reference audit reported no missing members. This is not a full gameplay test or ARM64 hardware execution test.
- Optional local BMP-to-PNG conversion matched all source pixels. The personal supplied PNG is copied byte-for-byte; it is absent from the public release.
- FNA, the managed audio adapter, NVorbis and FNA3D binaries remain unchanged from the tested Alpha1.5 build. Host source changed only the release banner; the new separate local-preparation tool handles installation.
- Bundled .NET 8.0.15 ARM64 runtime matches the official Microsoft archive and its published SHA512; old-install temporary files were excluded. All 188 official runtime files are retained, including notices.
- Public payload/source audits found no proprietary game executable/content, installed saves/logs or supplied artwork. Distributed ELF files are ARM64. Corresponding framework/native source and licences are included.

Prior device evidence: the user enjoyed Alpha1.5 and supplied an approximately 69-minute session ending normally. Heavy-battle slowdown remains. This evidence supports freezing that build for Beta; it does not prove all levels, firmware variants, cutscenes, save recovery or a complete playthrough.

Next: perform the physical clean-install/upgrade and gameplay checks in BETA_TESTING.md before declaring Final. Public audit is a payload check, not legal clearance; see RIGHTS_AND_ARTWORK.md.
