# Rebuilding the integration

End users do not need to build anything. Use the complete release ZIP and supply your own game.

Maintainers need .NET SDK 8, Python 3, CMake, a Linux ARM64 cross compiler/sysroot, and appropriate SDL2 headers/import library. Extract upstream-fna.tar.gz here to create build/FNA. It is the frozen modified framework/native snapshot used by this port; it is not represented as an upstream release or upstream contribution.

Build build/FNA/FNA.Core.csproj, src/Host/Host.csproj and src/Patcher/Patcher.csproj in Release mode. Copy the Host's managed port outputs to APPS/Bastion_RGDSPlus/game (never copy a proprietary executable there for publication), and copy Patcher DLL/JSON plus Mono.Cecil DLLs to runtime/patcher. The preserved rebuild.sh documents the native cross-build procedure; its locally patched game output is private-only and must not be packaged.

Public releases are assembled with tools/build_release.py. It inserts the pinned official ARM64 .NET runtime, generates the port manifest/checksums and runs the public audit. It never downloads game files. Runtime verification uses the frozen runtime file hashes.

The source contains developer regression tools and synthetic input/scheduler tests, not game assets. Local importer testing needs an owner's original Linux assembly and a known working private Alpha assembly. Do not commit fixtures, test outputs, bin/obj, original/ backups or installed folders.
