# Publish Beta 1 on GitHub — step by step

Nothing has been published automatically. Suggested repository: PolluxPT/Bastion-RGDSPlus. Suggested tag: v1.0.0-beta.1.

## 1. Choose the right files

- Use the **Bastion-RGDSPlus** repository folder from the GitHub Kit.
- The public release asset is **Bastion_RGDSPlus_v1.0.0-beta.1.zip**, supplied separately, together with its SHA256 file.
- Do NOT publish the PERSONAL install ZIP, the supplied Bastion.png, an installed game folder, original/ backups, savedata, logs, game/Bastion.exe, game/Content, game/Bastion.bmp or old Alpha archives.
- The public release excludes artwork; review RIGHTS_AND_ARTWORK.md. A licence/credit notice does not by itself clear third-party art or game rights.

## 2. Create the repository with GitHub Desktop (Windows-friendly)

1. Install GitHub Desktop from https://desktop.github.com/ and sign in to your PolluxPT account.
2. Choose File → New repository. Name it **Bastion-RGDSPlus** and choose a local parent folder. Do not add another licence or generated template; the kit already provides them.
3. Open the newly created local repository folder. Copy the CONTENTS of the kit's Bastion-RGDSPlus folder into it, including .gitignore. Avoid creating a nested Bastion-RGDSPlus/Bastion-RGDSPlus folder.
4. In Desktop's Changes list, check that there is no proprietary game, icon, save, log or original/ backup. The bundled .NET runtime is intentionally absent from the Git source tree; it is present in the release ZIP.
5. For an additional automated check, install Python 3 from python.org, open a terminal in this folder and run `python tools/audit_public.py .`. It should print PASS. Do not ignore a failure.
6. Commit with summary **Prepare Beta 1 release**.
7. Click Publish repository. You may start private for review. When ready to make it public, follow GitHub's visibility controls after checking the same content boundary. Select the intended account and repository name carefully.

## 3. Create the Beta prerelease

1. On the repository website, open **Releases** → **Draft a new release**.
2. Create tag **v1.0.0-beta.1**, targeting the main branch containing this kit.
3. Title: **Bastion RGDS+ — Beta 1**.
4. Copy RELEASE_NOTES_BETA1.md into the description.
5. Attach **Bastion_RGDSPlus_v1.0.0-beta.1.zip** and **Bastion_RGDSPlus_v1.0.0-beta.1_SHA256.txt**. Do not attach the personal icon package or the outer GitHub Kit.
6. Select **This is a pre-release**. Do not describe it as Final or guaranteed 60 FPS.
7. Save a draft first. Check the asset names, README installation paths, licences and rights notes.
8. When your review is complete, click **Publish release**. The default GitHub source ZIP is not the installation asset; point users to the named release ZIP.
9. Download the asset from the release page and compare its SHA256 with the provided value. Test a clean install from that downloaded asset plus your own game files.

## 4. After publication

Keep a Beta issue list using BETA_TESTING.md. Ask reporters for reviewed diagnostic logs, not game data or saves. Do not upload a populated installed folder as a replacement release. If new game builds need support, identify them by storefront/version/hash and adjust the local preparation tool without bundling their executables.

The root MIT licence covers original integration code only. Keep all third-party licence files. If you want to add artwork later, establish permission/provenance first; the kit does not claim copyright clearance or game-licence approval.

Official GitHub release instructions checked 2026-11-02: https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository
