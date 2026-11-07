#!/bin/sh
BASE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd) || exit 1
ROOT="$BASE/Bastion_RGDSPlus"
RT="$ROOT/runtime"
LOGS="$ROOT/logs"
mkdir -p "$LOGS" "$ROOT/savedata/data" "$ROOT/savedata/config" "$ROOT/savedata/cache" || exit 1
LOCK="$ROOT/savedata/.running"
if ! mkdir "$LOCK" 2>/dev/null; then
    old=$(cat "$LOCK/pid" 2>/dev/null)
    case "$old" in ''|*[!0-9]*) old='';; esac
    if [ -n "$old" ] && kill -0 "$old" 2>/dev/null; then exit 1; fi
    rm -f "$LOCK/pid"
    rmdir "$LOCK" 2>/dev/null && mkdir "$LOCK" || exit 1
fi
echo "$$" > "$LOCK/pid"
[ ! -f "$LOGS/Launcher.txt" ] || cp "$LOGS/Launcher.txt" "$LOGS/Previous_Launcher.txt"
[ ! -f "$LOGS/Game.txt" ] || cp "$LOGS/Game.txt" "$LOGS/Previous_Game.txt"
exec > "$LOGS/Launcher.txt" 2>&1
export LANG=C LC_ALL=C
game_pid='' guard_pid='' memory_pid='' perf_pid='' session_tmp='' lower_saved='' volume_saved=''
LOWER=/sys/class/backlight/backlight/brightness
VOLUME=/sys/class/anbernic_misc/openbor_volume
kernel_report() {
    dmesg > "$LOGS/kernel-snapshot.tmp" 2>&1
    if [ "$?" -eq 0 ]; then
        grep -Ei 'out of memory|oom|killed process|mali|gpu|drm|panfrost' "$LOGS/kernel-snapshot.tmp" | tail -n 160
    else cat "$LOGS/kernel-snapshot.tmp"; fi
    rm -f "$LOGS/kernel-snapshot.tmp"
}
cleanup() {
    trap - EXIT INT TERM HUP
    if [ -n "$game_pid" ]; then
        kill -TERM "$game_pid" 2>/dev/null || :
        n=0
        while kill -0 "$game_pid" 2>/dev/null && [ "$n" -lt 3 ]; do sleep 1; n=$((n+1)); done
        kill -KILL "$game_pid" 2>/dev/null || :
        wait "$game_pid" 2>/dev/null || :
    fi
    [ -z "$perf_pid" ] || { kill "$perf_pid" 2>/dev/null || :; wait "$perf_pid" 2>/dev/null || :; }
    if [ -n "$session_tmp" ]; then python3 "$RT/performance-mode.py" restore "$session_tmp/performance-state.json" >> "$LOGS/Performance_Mode.txt" 2>&1 || :; fi
    [ -z "$memory_pid" ] || { kill "$memory_pid" 2>/dev/null || :; wait "$memory_pid" 2>/dev/null || :; }
    [ -z "$guard_pid" ] || { kill "$guard_pid" 2>/dev/null || :; wait "$guard_pid" 2>/dev/null || :; }
    [ -z "$lower_saved" ] || printf '%s\n' "$lower_saved" > "$LOWER" 2>/dev/null || :
    [ -z "$volume_saved" ] || printf '%s\n' "$volume_saved" > "$VOLUME" 2>/dev/null || :
    case "$session_tmp" in /tmp/bastion-rgdsplus.*) rm -rf -- "$session_tmp";; esac
    kernel_report > "$LOGS/Kernel_End.txt" 2>&1
    cat /proc/meminfo > "$LOGS/Memory_End.txt" 2>/dev/null || :
    rm -f "$LOCK/pid"; rmdir "$LOCK" 2>/dev/null || :
    command -v python3 >/dev/null 2>&1 && python3 "$RT/collect-logs.py" "$LOGS" || :
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM HUP
printf 'Bastion RGDS+ Beta 1 (1.0.0-beta.1)\n'
date; uname -srvm
case "$(uname -m)" in aarch64|arm64) :;; *) echo 'ERROR: ARM64 Linux is required.'; exit 2;; esac
for f in runtime/dotnet/dotnet runtime/native/libFNA3D.so.0 runtime/native/libFAudio.so.0 game/BastionRGDS.Host.dll game/FMOD.dll game/NVorbis.dll game/FNA.dll; do
    [ -s "$ROOT/$f" ] || { echo "ERROR: missing $f. Extract the complete package again."; exit 3; }
done
command -v python3 >/dev/null 2>&1 || { echo 'ERROR: firmware Python 3 is required.'; exit 4; }
python3 "$RT/verify-install.py" "$ROOT" > "$LOGS/Installation_Check.txt" 2>&1
check_result=$?
if [ "$check_result" -ne 0 ]; then cat "$LOGS/Installation_Check.txt";exit "$check_result"; fi
if [ -z "${WAYLAND_DISPLAY:-}" ] || [ -z "${XDG_RUNTIME_DIR:-}" ]; then
    for d in /var/run /run "/run/user/$(id -u)"; do
        for s in "$d"/wayland-*; do
            [ -S "$s" ] || continue
            export XDG_RUNTIME_DIR="$d" WAYLAND_DISPLAY="${s##*/}"; break 2
        done
    done
fi
[ -n "${WAYLAND_DISPLAY:-}" ] || { echo 'ERROR: no Wayland display found.'; exit 5; }
unset LD_PRELOAD SDL_DYNAMIC_API SDL_VIDEO_DRIVER SDL_AUDIO_DRIVER BASTION_TEST_SECONDS BASTION_TEST_CAPTURE BASTION_TEST_INPUT BASTION_TEST_CAPTURE_AFTER BASTION_CALIBRATION_TEST BASTION_CALIBRATION_SCREENSHOT
export LANG=C LC_ALL=C
export LD_LIBRARY_PATH="$RT/native:/lib:/usr/lib:/lib64:/usr/lib64"
export DOTNET_ROOT="$RT/dotnet" DOTNET_MULTILEVEL_LOOKUP=0 DOTNET_EnableDiagnostics=0 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 DOTNET_GCConserveMemory=5
chmod +x "$RT/dotnet/dotnet" 2>/dev/null || :
if ! python3 "$RT/prepare-game.py" "$ROOT" > "$LOGS/Preparation.txt" 2>&1; then
    cat "$LOGS/Preparation.txt"
    exit 3
fi
export BASTION_TEXTURE_16BIT=0
export BASTION_RGBA_UPLOADS=1 BASTION_PACKED_TEXTURES=1 BASTION_GL_DIAGNOSTICS=1
case "$(cat "$RT/texture-limit.txt" 2>/dev/null)" in 0) export BASTION_TEXTURE_MAX=0;; 512) export BASTION_TEXTURE_MAX=512;; *) export BASTION_TEXTURE_MAX=1024;; esac
case "$(cat "$RT/texture-decode.txt" 2>/dev/null)" in auto) export BASTION_FORCE_RGBA_TEXTURES=0;; *) export BASTION_FORCE_RGBA_TEXTURES=1;; esac
case "$(cat "$RT/catchup-steps.txt" 2>/dev/null)" in 0) export BASTION_CATCHUP_STEPS=0;; 3) export BASTION_CATCHUP_STEPS=3;; 4) export BASTION_CATCHUP_STEPS=4;; *) export BASTION_CATCHUP_STEPS=2;; esac
export BASTION_VOLUME_NODE="$VOLUME"
export SDL_GAMECONTROLLERCONFIG="$(cat "$RT/rgds-controller.txt")"
case "$(cat "$RT/bloom.txt" 2>/dev/null)" in 1) export BASTION_BLOOM=1;; *) export BASTION_BLOOM=0;; esac
export FNA_PLATFORM_BACKEND=SDL2 FNA3D_FORCE_DRIVER=OpenGL FNA3D_OPENGL_FORCE_ES3=1
export SDL_VIDEODRIVER=wayland SDL_AUDIODRIVER=alsa SDL_VIDEO_FULLSCREEN_DISPLAY=0 SDL_VIDEO_WINDOW_POS=0,0
export SDL_OPENGL_ES_DRIVER=1 SDL_VIDEO_GL_DRIVER=/lib/libGLESv2.so.2 SDL_VIDEO_EGL_DRIVER=/lib/libEGL.so.1
export SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS=1
export ALSOFT_DRIVERS=alsa ALSOFT_CONF="$RT/alsoft.conf" ALSOFT_LOGLEVEL=2 ALSOFT_LOGFILE="$LOGS/OpenAL.txt"
export BASTION_CONTROLLER_PROFILE="$ROOT/savedata/config/bastion-controls-v1.json"
export XDG_DATA_HOME="$ROOT/savedata/data" XDG_CONFIG_HOME="$ROOT/savedata/config" XDG_CACHE_HOME="$ROOT/savedata/cache"
session_tmp=$(mktemp -d /tmp/bastion-rgdsplus.XXXXXX) || exit 6
export TMPDIR="$session_tmp" BASTION_EXIT_REQUEST="$session_tmp/exit-request"
resolution=$(cat "$RT/resolution.txt" 2>/dev/null)
case "$resolution" in 512x384) width=512; height=384;; 768x576) width=768; height=576;; 1024x768) width=1024; height=768;; *) width=640; height=480;; esac
printf 'Backbuffer: %sx%s; display: %s; sampled texture limit: %s\n' "$width" "$height" "$WAYLAND_DISPLAY" "$BASTION_TEXTURE_MAX"
ulimit -c 0
chmod +x "$RT/dotnet/dotnet" 2>/dev/null || :
kernel_report > "$LOGS/Kernel_Start.txt" 2>&1
cat /proc/meminfo > "$LOGS/Memory_Start.txt" 2>/dev/null || :
if command -v ldd >/dev/null 2>&1; then
    ldd "$RT/dotnet/dotnet"
    ldd "$RT/native/libFNA3D.so.0"
fi
if [ -r "$VOLUME" ] && [ -w "$VOLUME" ]; then
    volume_saved=$(cat "$VOLUME" 2>/dev/null)
    case "$volume_saved" in 0|1|2|3|4|5|6|7|8|9|10) echo "Saved OS volume level: $volume_saved/10";; *) volume_saved='';; esac
fi
if [ "$(cat "$RT/disable-lower-screen.txt" 2>/dev/null)" = 1 ] && [ -r "$LOWER" ] && [ -w "$LOWER" ]; then
    lower_saved=$(cat "$LOWER" 2>/dev/null)
    case "$lower_saved" in ''|*[!0-9]*) lower_saved='';; *) printf '0\n' > "$LOWER" 2>/dev/null || :;; esac
fi
cd "$ROOT/game" || exit 7
echo 'Checking controller calibration.'
calibration_mode=--ensure-controls
[ "${BASTION_REMAP_CONTROLS:-0}" != 1 ] || calibration_mode=--calibrate-controls
"$RT/dotnet/dotnet" "$ROOT/game/BastionRGDS.Host.dll" "$calibration_mode" > "$LOGS/Controls.txt" 2>&1 & game_pid=$!
wait "$game_pid"; calibration_result=$?; game_pid=''
if [ "$calibration_result" -ne 0 ]; then
    echo "Controller setup exited with code $calibration_result. See Controls.txt."
    exit "$calibration_result"
fi
# Export at SDL's environment priority; a later AddMapping API call cannot
# override an older environment-priority entry, even when it returns success.
if ! python3 "$RT/controller-map.py" "$BASTION_CONTROLLER_PROFILE" > "$session_tmp/controller-map.txt"; then
    echo 'ERROR: could not export calibrated controller mapping.'
    exit 8
fi
SDL_GAMECONTROLLERCONFIG=$(cat "$session_tmp/controller-map.txt")
[ -n "$SDL_GAMECONTROLLERCONFIG" ] || exit 8
export SDL_GAMECONTROLLERCONFIG
printf 'Active calibrated SDL mapping: %s\n' "$SDL_GAMECONTROLLERCONFIG"
if [ "$(cat "$RT/performance-mode.txt" 2>/dev/null)" = 1 ]; then
    python3 "$RT/performance-mode.py" apply "$session_tmp/performance-state.json" > "$LOGS/Performance_Mode.txt" 2>&1 || :
else echo 'Session performance governors disabled.' > "$LOGS/Performance_Mode.txt"; fi
printf 'Fixed-step catch-up limit: %s updates per draw\n' "$BASTION_CATCHUP_STEPS"
echo 'Starting game. Hold START+SELECT for 1.5 seconds to exit.' 
"$RT/dotnet/dotnet" "$ROOT/game/BastionRGDS.Host.dll" -x "$width" -y "$height" -lang en > "$LOGS/Game.txt" 2>&1 & game_pid=$!
python3 -u "$RT/exit-guard.py" "$$" "$BASTION_EXIT_REQUEST" > "$LOGS/Exit_Keys.txt" 2>&1 & guard_pid=$!
python3 -u "$RT/memory-watch.py" "$game_pid" > "$LOGS/Memory_Trace.txt" 2>&1 & memory_pid=$!
python3 -u "$RT/performance-mode.py" watch "$game_pid" > "$LOGS/Performance_Trace.txt" 2>&1 & perf_pid=$!
wait "$game_pid"; result=$?; game_pid=''
echo "Game exit code: $result"
exit "$result"
