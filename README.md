# Bastion RGDS+ — Beta 1

Unofficial standalone native ARM64 port integration for **Anbernic RGDS+ running Stock Linux**. No PortMaster or x86 emulation is required. **You must own the Linux PC version of Bastion. The game is not included.**

Version: `1.0.0-beta.1`. Beta 1 freezes the gameplay/runtime settings of the user-tested Alpha1.5 build. Heavy battles can still slow down; this is not a claim of full-game completion or locked 60 FPS.

## Download

Use the release asset **Bastion_RGDSPlus_v1.0.0-beta.1.zip** from the repository's Releases page after it is published. GitHub's green Code → Download ZIP is a source tree and does not include the bundled .NET runtime.

## Clean installation

1. Exit any existing Bastion session.
2. Extract the release ZIP on your PC. Copy its **APPS** folder into TF2's **Roms** folder, merging with your existing APPS folder. Use the same APPS location as your other Stock OS ports.
3. Find your purchased **Linux** game directory: the folder containing `Bastion.exe` and `Content`.
4. Copy **only `Bastion.exe` and the complete `Content` folder** into `Roms/APPS/Bastion_RGDSPlus/game/`. You may also copy `Bastion.bmp` for a menu icon generated locally. Do not copy the PC's FNA/FMOD DLLs or x86 libraries over the bundled port files.
5. Safely eject the card and launch **Bastion** / `Bastion.sh` from APPS.
6. First launch validates the game data, backs up your assembly under `original/`, and patches it locally. No SDK or online download is required on the handheld.
7. Complete the one-time 14-control setup if prompted, pressing the physical labels on the handheld and releasing between prompts.

| Location | Purpose |
|---|---|
| `Roms/APPS/Bastion.sh` | Main launcher |
| `Roms/APPS/Bastion_Controls.sh` | Repeat controller calibration |
| `Roms/APPS/Imgs/Bastion.png` | Optional local menu icon |
| `Roms/APPS/Bastion_RGDSPlus/game/Bastion.exe` | Your supplied Linux assembly, prepared locally |
| `Roms/APPS/Bastion_RGDSPlus/game/Content/` | Your complete Linux game content, including `720p/` |
| `Roms/APPS/Bastion_RGDSPlus/savedata/` | Progress, settings and calibration; preserve on upgrades |
| `Roms/APPS/Bastion_RGDSPlus/logs/` | Diagnostic logs |

Allow approximately **1.5 GB** for the port, Linux assets and local backup, plus free space for saves/logs. The supported asset set contains about 1.27 GB; no second full copy is created during preparation.

## Existing Alpha1.5 installation

See [UPGRADE_FROM_ALPHA.md](docs/UPGRADE_FROM_ALPHA.md). Rename the old support folder to `Bastion_RGDSPlus`, merge the Beta files and preserve `game/Content`, `Bastion.exe` and `savedata/`. The new package does not overwrite commercial game files or saves. A compatible Alpha-patched assembly is retained without duplicate hooks.

## Controls and volume

Calibrate A/B/X/Y, L1/R1, SELECT/START, L2/R2 and the four D-pad directions. The saved per-GUID mapping takes SDL environment priority at startup; LT/RT are included. The existing left-stick mapping is retained. Hold physical **START+SELECT for 1.5 seconds** to exit gameplay. During setup hold START+SELECT for two seconds to cancel.

Volume starts at the captured OS level; the physical buttons use gain `0.35 × (level/10)²`. The OS level is restored on exit. The lower screen is disabled during gameplay by default and its prior brightness is restored on exit.

## Performance and known limitations

Defaults: 512×384 backbuffer, official 720p asset packages, reduced/dithered storage for eligible large textures, bloom disabled, at most two catch-up updates per draw, and supported CPU/GPU performance governors during play. The original fixed physics step is retained. Governors are restored on exit; frequency limits, voltages and thermal protection are not changed. Session performance mode may increase battery use.

The catch-up bound prioritizes responsiveness by discarding excess time debt during overload. This means intense fights can run in slow motion. A user session of about 69 minutes completed normally; it does not substitute for a full playthrough or broad firmware testing. Cutscenes, later bosses and all game variants are not fully validated.

Editable settings are in `runtime/`: `catchup-steps.txt` (2 default; 0 original scheduling), `performance-mode.txt` (1/0), `resolution.txt`, `texture-limit.txt`, `bloom.txt` and `disable-lower-screen.txt`.

## Supported game build

Beta 1 supports the Linux assembly tested during development, SHA-256:

`e8d028fdca5069115202d45695cc44e4e9c39bd27179415c65cc59182db3d748`

Other storefront/build combinations are not guaranteed. Windows/macOS/Android versions are not supported substitutes. An unsupported original is left unchanged and the reason is written to `logs/Preparation.txt`. Use your store's supported method to obtain Linux files; this port does not download them or bypass DRM.

## Troubleshooting

Read [SUPPORT.md](SUPPORT.md). For a gameplay problem attach `Bastion_RGDSPlus/logs/Bastion_TestLogs.zip`, the level name and what happened. Never attach game assemblies, Content, savedata, account information or a copy of an installed folder to a public issue.

## Rights, licences and source

Bastion and its characters, artwork, music and other game materials belong to their respective rights holders. This project is not affiliated with or endorsed by Supergiant Games, Anbernic or the storefronts. The name identifies the required game.

The public release contains redistributable port/runtime components and licences, not `Bastion.exe`, `Bastion.bmp`, game Content, soundtrack or saved games. The supplied fan-art icon is omitted from the public package because its redistribution rights were not established. An optional icon can be generated locally from your own `Bastion.bmp` or supplied by you with appropriate rights.

See [RIGHTS_AND_ARTWORK.md](RIGHTS_AND_ARTWORK.md), [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and [Source/README.md](Source/README.md). The MIT licence at the root covers original integration code only. Third-party components retain their own licences. Local preparation does not grant permission to redistribute game files or establish that every use is lawful; review your game licence and applicable requirements before publication/use.
