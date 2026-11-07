# Support and diagnostic reports

Beta 1 is for the tested RGDS+ Stock Linux environment: ARM64, Wayland/GLES and the firmware SDL2/OpenAL libraries. Other boards, Android, custom firmware, Windows game files and arbitrary Linux game builds are not validated.

- **Nothing starts / missing files:** inspect `logs/Launcher.txt`, `Installation_Check.txt` and `Preparation.txt`. Re-extract the complete release for a damaged port; supply your own complete Linux game for missing Content. Do not copy PC libraries over the port.
- **Unsupported build:** report storefront/build details and the SHA-256 printed in Preparation.txt. Do not upload Bastion.exe or an installer.
- **Wrong buttons:** run Bastion_Controls.sh. If you previously reassigned actions inside Bastion, restore its default action bindings after physical calibration if necessary. Controls.txt records setup; Game.txt records the effective SDL mapping.
- **Slow battles:** report the level, approximate battle/event and whether the result was slow motion or a long freeze. The catch-up limiter is a responsiveness compromise, not a guaranteed 60 FPS fix.
- **Audio or exit:** report physical volume behavior and exit method. Do not change OS-wide volume services or governors to troubleshoot this port.

Attach `logs/Bastion_TestLogs.zip` to a GitHub issue after quitting. Review logs before sharing; they contain device/build information and paths. Do not attach savedata, game archives, original/ backups or Content. Do not attach your entire installed folder.

A useful report includes Beta version, firmware version, game store/build, scene/level, reproduction steps, expected behavior and whether you changed a runtime setting.
