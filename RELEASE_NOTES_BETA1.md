# Bastion RGDS+ — Beta 1

Unofficial standalone native ARM64 integration for RGDS+ Stock Linux.

**Bring your own legally obtained Linux PC version of Bastion. Game files are not included.** Download Bastion_RGDSPlus_v1.0.0-beta.1.zip; the GitHub auto-generated source archive is not the complete installation package.

Copy APPS into TF2/Roms, then place your Linux Bastion.exe and complete Content folder inside APPS/Bastion_RGDSPlus/game/. Bastion.bmp is optional for a locally generated icon. Launch Bastion.sh. First launch prepares the assembly locally and asks for control calibration if needed.

Existing Alpha1.5 owners: rename the support folder containing game/runtime/savedata to Bastion_RGDSPlus, merge this release, and preserve savedata and game files. Read the upgrade guide before removing old launchers.

Beta 1 keeps the tested Alpha1.5 behavior: calibrated LT/RT, OS volume capture/restore, START+SELECT exit, lower-screen handling, native GLES rendering, bounded fixed-step catch-up and session performance governors. Severe battles can still slow down. No full-game completion or stable 60 FPS guarantee is made.

This public asset excludes commercial game files and the supplied fan-art PNG. See the repository's licences/rights notes. Report issues with version, level/event and reviewed Bastion_TestLogs.zip; do not upload game files or savedata.
