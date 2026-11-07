# Preserve your Alpha1.5 progress when moving to Beta 1

1. Quit Bastion and back up your current `savedata` folder to your PC.
2. Locate the support folder containing `game`, `runtime` and `savedata`. Older packages call it **Bastion_Native** or **BastionNative_PC**; your local folder may have another Alpha name.
3. On the card, rename that support folder to **Bastion_RGDSPlus**. Do not put it inside another Bastion_RGDSPlus folder.
4. Copy Beta 1's APPS contents into the same `Roms/APPS`, merging the renamed folder and overwriting port files.
5. Keep your existing `game/Bastion.exe`, `game/Content`, optional `Bastion.bmp`, and **all `savedata` contents**. The release ZIP contains none of those files.
6. Start `Bastion.sh`. The preparation tool recognizes a compatible Alpha assembly and does not patch its hooks twice. Calibration is reused when the GUID/counts still match.
7. Check Continue, volume, LT/RT and START+SELECT. Once the new launcher works, remove/archive the obsolete `Bastion_Native.sh` and old calibration launcher so the menu does not show duplicate entries. Do not delete your support folder or saves.

The original assembly backup created during a clean install stays under `original/`. Everything copied or generated from the commercial game is local-only. Never upload your upgraded installed directory.
