#!/bin/sh
# Build-machine instructions. Not intended to run on the handheld.
set -eu
cd "$(dirname "$0")"
[ "$#" -eq 3 ] || { echo 'Usage: ./rebuild.sh ORIGINAL_Bastion.exe ARM64_SDL2_INCLUDE_DIR ARM64_libSDL2.so'; exit 2; }
original=$(realpath "$1")
sdl_include=$(realpath "$2")
sdl_library=$(realpath "$3")
tar -xzf upstream-fna.tar.gz
dotnet build build/FNA/FNA.Core.csproj -c Release
dotnet build src/Host/Host.csproj -c Release
dotnet build src/Tools/Tools.csproj -c Release
dotnet build src/Patcher/Patcher.csproj -c Release
mkdir -p out/game out/native out/patcher
cp src/Host/bin/Release/net8.0/*.dll src/Host/bin/Release/net8.0/*.json src/Host/bin/Release/net8.0/*.config out/game/
cp src/Patcher/bin/Release/net8.0/*.dll src/Patcher/bin/Release/net8.0/*.json out/patcher/
dotnet out/patcher/BastionRGDS.Patcher.dll "$original" out/game/Bastion.exe out/game/BastionRGDS.Host.dll
compiler=$(command -v aarch64-linux-gnu-gcc)
cat > arm64.cmake <<EOF
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_PROCESSOR aarch64)
set(CMAKE_C_COMPILER "$compiler")
EOF
for library in FNA3D FAudio; do
    cmake -S "build/FNA/lib/$library" -B "out/$library" -DCMAKE_TOOLCHAIN_FILE="$PWD/arm64.cmake" -DBUILD_SDL3=OFF -DSDL2_INCLUDE_DIRS="$sdl_include" -DSDL2_LIBRARIES="$sdl_library" -DCMAKE_BUILD_TYPE=Release -DCMAKE_SKIP_RPATH=ON
    cmake --build "out/$library" -j4
    cp -L "out/$library/lib$library.so.0" out/native/
done
make -C build/FNA/lib/Theorafile CC="$compiler" TFSRC_ARCH_OPT= DEFINES=
cp build/FNA/lib/Theorafile/libtheorafile.so out/native/
echo 'Rebuilt game integration and native libraries are in out/. Supply the official Linux ARM64 .NET 8 runtime separately.'
