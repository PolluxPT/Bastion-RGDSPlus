# Third-party notices

| Component | Version / revision | Licence / location |
|---|---|---|
| .NET runtime, Linux ARM64 | 8.0.15, frozen tested runtime | MIT plus ThirdPartyNotices; release runtime/dotnet/LICENSE.txt and ThirdPartyNotices.txt |
| FNA | 6f9735ad5b495b36581b1387824fa57f371937a2, modified for this port | Microsoft Public License; FNA_LICENSE, FNA_lzxdecoder.LICENSE, FNA_monoxna.LICENSE |
| FNA3D | 2806acf739f9250cc67be6e3078cb5b01767844e, private integration modifications | zlib; FNA3D_LICENSE |
| MojoShader | snapshot included in FNA3D source | zlib; MojoShader_LICENSE.txt |
| FAudio | 0c423758079863fc3044ba26dd00fc89c41f469e | zlib; FAudio_LICENSE |
| Theorafile | 3497e1a3cda1fa1ce79256f20e210fd08aec546f | zlib; Theorafile_LICENSE |
| libogg / libtheora / libvorbis source used by Theorafile | frozen source snapshot | BSD-style; corresponding Theorafile_*.LICENSE files |
| NVorbis | 0.10.5 | MIT; NVorbis_LICENSE |
| Mono.Cecil | 0.11.6 | MIT; Mono.Cecil_LICENSE.txt |
| SDL2 headers / bindings | pinned snapshot; firmware library not bundled | zlib; SDL2_headers_LICENSE.txt and source notices |
| Original integration code | Beta 1 | MIT; root LICENSE and RGDS-integration-MIT.txt |

Full notices are in APPS/Bastion_RGDSPlus/licenses; the .NET release carries its own notices unchanged. Source/upstream-fna.tar.gz includes the corresponding frozen framework/native source. The native/audio render components are unchanged from the Alpha1.5 build tested on the device.

The file called game/FMOD.dll is this project's managed compatibility adapter, built from Source/src/AudioCompat. It is not an FMOD vendor binary. The required proprietary Bastion.exe and Content must be supplied locally by each owner.

Source links: https://github.com/FNA-XNA/FNA ; https://github.com/FNA-XNA/FNA3D ; https://github.com/FNA-XNA/FAudio ; https://github.com/FNA-XNA/Theorafile ; https://github.com/NVorbis/NVorbis ; https://github.com/jbevain/cecil ; https://github.com/dotnet/runtime
