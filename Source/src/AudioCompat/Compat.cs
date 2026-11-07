// RGDS+ integration: MIT. Implements the subset called by the supplied PC game.
using System.Runtime.InteropServices;
using NVorbis;

namespace FMOD;
public enum RESULT { OK=0, ERR_INTERNAL=28, ERR_INVALID_HANDLE=30 }
[Flags] public enum MODE:uint { DEFAULT=0, LOOP_OFF=1, LOOP_NORMAL=2 }
public enum CHANNELINDEX { FREE=-1, REUSE=-2 }
[StructLayout(LayoutKind.Sequential)] public struct CREATESOUNDEXINFO { public int cbsize; public uint length; }
[StructLayout(LayoutKind.Sequential)] public struct REVERB_CHANNELPROPERTIES { public int Room; public uint Flags; }
public static class Framework {
    public static System system;
    public static void init() { lock(Audio.Gate) { system ??=new System(); Audio.Init(); } }
    public static void update() { }
    public static void terminate() { lock(Audio.Gate) { Audio.Close(); system=null; } }
    public static void resetSystem() { terminate(); init(); }
}
public sealed class System {
    public RESULT createSound(byte[] bytes, MODE mode, ref CREATESOUNDEXINFO info, ref Sound sound) {
        if(bytes==null || info.length==0 || info.length>bytes.Length) return RESULT.ERR_INTERNAL;
        sound=new Sound(bytes,checked((int)info.length),(mode & MODE.LOOP_NORMAL)!=0); return RESULT.OK;
    }
    public RESULT playSound(CHANNELINDEX index, Sound sound, bool paused, ref Channel channel) {
        lock(Audio.Gate) {
            try { Audio.Init(); if(sound==null || sound.Released)return RESULT.ERR_INVALID_HANDLE;
                if(Audio.Channels.Count>=64) { Audio.Channels[0].stop(); }
                channel=new Channel(sound,paused); Audio.Channels.Add(channel); return RESULT.OK;
            } catch(Exception ex) { Console.Error.WriteLine("AUDIO play: "+ex.Message); return RESULT.ERR_INTERNAL; }
        }
    }
}
public sealed class Sound {
    internal readonly byte[] Bytes; internal readonly int Length; internal readonly bool Loop;
    internal readonly long Id=Interlocked.Increment(ref nextId); static long nextId;
    internal bool Released;
    internal Sound(byte[] bytes,int length,bool loop) { Bytes=bytes; Length=length; Loop=loop; }
    public IntPtr getRaw()=>new IntPtr(Id);
    public RESULT release() { lock(Audio.Gate) { foreach(var c in Audio.Channels.Where(c=>c.Sound==this).ToArray()) c.stop(); Released=true; return RESULT.OK; } }
}
public sealed class Channel {
    internal readonly Sound Sound;
    readonly VorbisReader decoder;
    readonly MemoryStream stream;
    readonly float[] samples=new float[2048]; readonly short[] pcm=new short[2048];
    uint source; readonly uint[] buffers=new uint[4];
    bool paused,finished,disposed,eof;
    readonly int rate; readonly int format;
    uint filter; float lowPass=1; int room=-10000;
    internal Channel(Sound sound,bool pause) {
        Sound=sound; paused=pause;
        stream=new MemoryStream(sound.Bytes,0,sound.Length,false);
        decoder=new VorbisReader(stream,false);
        rate=decoder.SampleRate;
        if(decoder.Channels!=1 && decoder.Channels!=2) { decoder.Dispose(); stream.Dispose(); throw new NotSupportedException("Only mono/stereo Vorbis supported"); }
        format=decoder.Channels==1?0x1101:0x1103;
        AL.alGenSources(1,out source);
        if(source==0)throw new InvalidOperationException("OpenAL source allocation failed");
        AL.alGenBuffers(4,buffers); AL.alSourcei(source,0x202,1); AL.alSourcef(source,0x1021,0);
        foreach(var buffer in buffers) { uint b=buffer; if(Fill(b))AL.alSourceQueueBuffers(source,1,ref b); }
        if(!paused)AL.alSourcePlay(source);
    }
    bool Fill(uint b) {
        int n=0;
        while(n<samples.Length) {
            int read=decoder.ReadSamples(samples,n,samples.Length-n);
            if(read==0) { if(Sound.Loop && decoder.TotalSamples>0) { decoder.SamplePosition=0; continue; } eof=true; break; }
            n+=read;
        }
        if(n==0)return false;
        for(int i=0;i<n;i++)pcm[i]=float.IsFinite(samples[i])?(short)Math.Clamp((int)(samples[i]*32767f),-32768,32767):(short)0;
        AL.alBufferData(b,format,pcm,n*2,rate); return true;
    }
    internal void Tick() {
        if(disposed)return;
        AL.alGetSourcei(source,0x1016,out int processed);
        while(processed-->0) { AL.alSourceUnqueueBuffers(source,1,out uint b); if(!eof && Fill(b))AL.alSourceQueueBuffers(source,1,ref b); }
        AL.alGetSourcei(source,0x1015,out int queued);
        if(queued==0) { finished=true; DisposeNative(); return; }
        AL.alGetSourcei(source,0x1010,out int state);
        if(!paused && state!=0x1012) AL.alSourcePlay(source);
    }
    void DisposeNative() {
        if(disposed)return; disposed=true;
        if(source!=0) { AL.alSourceStop(source); AL.alDeleteSources(1,ref source); }
        AL.alDeleteBuffers(4,buffers);
        if(filter!=0 && Audio.DeleteFilter!=null)Audio.DeleteFilter(1,ref filter);
        decoder.Dispose(); stream.Dispose(); Audio.Channels.Remove(this);
    }
    public RESULT getCurrentSound(ref Sound sound) { lock(Audio.Gate) { sound=disposed?null:Sound; return RESULT.OK; } }
    public RESULT isPlaying(ref bool playing) { lock(Audio.Gate) { playing=!disposed&&!finished; return RESULT.OK; } }
    public RESULT getPaused(ref bool value) { lock(Audio.Gate) { value=paused; return RESULT.OK; } }
    public RESULT setPaused(bool value) { lock(Audio.Gate) { paused=value; if(!disposed) { if(value)AL.alSourcePause(source);else AL.alSourcePlay(source); } return RESULT.OK; } }
    public RESULT getFrequency(ref float value) { value=rate; return RESULT.OK; }
    public RESULT setFrequency(float value) { lock(Audio.Gate) { if(!disposed)AL.alSourcef(source,0x1003,Math.Clamp(value/rate,0.01f,4f)); return RESULT.OK; } }
    public RESULT setVolume(float value) { lock(Audio.Gate) { if(!disposed)AL.alSourcef(source,0x100A,Math.Clamp(value,0,3)); return RESULT.OK; } }
    public RESULT setLowPassGain(float value) { lock(Audio.Gate) { lowPass=Math.Clamp(value,0,1); ApplyEffects(); return RESULT.OK; } }
    public RESULT setReverbProperties(ref REVERB_CHANNELPROPERTIES prop) { lock(Audio.Gate) { room=prop.Room; ApplyEffects(); return RESULT.OK; } }
    void ApplyEffects() {
        if(disposed || Audio.GenFilter==null)return;
        if(filter==0) { Audio.GenFilter(1,out filter); Audio.Filteri(filter,0x8001,1); }
        Audio.Filterf(filter,1,1); Audio.Filterf(filter,2,lowPass); AL.alSourcei(source,0x20005,(int)filter);
        // One common room preset. FMOD room levels map to send gain in millibels.
        if(Audio.Slot!=0 && room>-10000) AL.alSource3i(source,0x20006,(int)Audio.Slot,0,0);
        else AL.alSource3i(source,0x20006,0,0,0);
    }
    public RESULT stop() { lock(Audio.Gate) { finished=true; DisposeNative(); return RESULT.OK; } }
}
internal static class Audio {
    internal static readonly object Gate=new(); internal static readonly List<Channel> Channels=new();
    static IntPtr device,context; static Thread worker; static bool running; static int generation; static float appliedGain; internal static uint Slot;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void Gen(int n,out uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void Del(int n,ref uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SetI(uint id,int key,int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void SetF(uint id,int key,float value);
    internal static Gen GenFilter; internal static Del DeleteFilter; internal static SetI Filteri; internal static SetF Filterf;
    static uint effect; static Del deleteEffect,deleteSlot;
    static T Proc<T>(string name) where T:Delegate { var p=AL.alGetProcAddress(name); return p==IntPtr.Zero?null:Marshal.GetDelegateForFunctionPointer<T>(p); }
    internal static void Init() {
        if(context!=IntPtr.Zero)return;
        RGDSVolume.Initialize();
        device=AL.alcOpenDevice(null); if(device==IntPtr.Zero)throw new InvalidOperationException("OpenAL cannot open audio device");
        context=AL.alcCreateContext(device,null);
        if(context==IntPtr.Zero || !AL.alcMakeContextCurrent(context)) { Close(); throw new InvalidOperationException("OpenAL context failed"); }
        if(AL.alcIsExtensionPresent(device,"ALC_EXT_EFX")) {
            GenFilter=Proc<Gen>("alGenFilters"); DeleteFilter=Proc<Del>("alDeleteFilters"); Filteri=Proc<SetI>("alFilteri"); Filterf=Proc<SetF>("alFilterf");
            var gen=Proc<Gen>("alGenEffects"); var set=Proc<SetI>("alEffecti"); var genSlot=Proc<Gen>("alGenAuxiliaryEffectSlots"); var setSlot=Proc<SetI>("alAuxiliaryEffectSloti");
            deleteEffect=Proc<Del>("alDeleteEffects"); deleteSlot=Proc<Del>("alDeleteAuxiliaryEffectSlots");
            if(gen!=null && set!=null && genSlot!=null && setSlot!=null) { gen(1,out effect); set(effect,0x8001,1); genSlot(1,out Slot); setSlot(Slot,1,(int)effect); }
        }
        appliedGain=RGDSVolume.Gain; AL.alListenerf(0x100A,appliedGain);
        running=true; int ticket=++generation; worker=new Thread(()=>Work(ticket)){IsBackground=true,Name="Bastion Vorbis stream"}; worker.Start();
        Console.WriteLine("AUDIO: ARM64-compatible OpenAL/Vorbis backend initialized");
    }
    static void Work(int ticket) {
        while(true) { lock(Gate) { if(!running || ticket!=generation)return; float volume=RGDSVolume.Gain; if(volume!=appliedGain) { AL.alListenerf(0x100A,volume);appliedGain=volume; } foreach(var c in Channels.ToArray()) { try { c.Tick(); }catch(Exception ex) { Console.Error.WriteLine("AUDIO stream: "+ex.Message); c.stop(); } } } Thread.Sleep(10); }
    }
    internal static void Close() {
        running=false; generation++; foreach(var c in Channels.ToArray())c.stop();
        if(context!=IntPtr.Zero) { if(Slot!=0)deleteSlot?.Invoke(1,ref Slot); if(effect!=0)deleteEffect?.Invoke(1,ref effect); AL.alcMakeContextCurrent(IntPtr.Zero); AL.alcDestroyContext(context); context=IntPtr.Zero; }
        if(device!=IntPtr.Zero) { AL.alcCloseDevice(device); device=IntPtr.Zero; }
    }
}
internal static class AL {
    const string Lib="libopenal.so.1";
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alListenerf(int key,float value);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr alcOpenDevice(string name);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr alcCreateContext(IntPtr device,int[] attrs);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool alcMakeContextCurrent(IntPtr context);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alcDestroyContext(IntPtr context);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool alcCloseDevice(IntPtr device);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] [return:MarshalAs(UnmanagedType.I1)] internal static extern bool alcIsExtensionPresent(IntPtr device,string name);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr alGetProcAddress(string name);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alGenSources(int n,out uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alDeleteSources(int n,ref uint id);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alGenBuffers(int n,[Out]uint[] ids);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alDeleteBuffers(int n,uint[] ids);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alBufferData(uint buffer,int format,short[] data,int length,int rate);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourceQueueBuffers(uint source,int n,ref uint buffer);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourceUnqueueBuffers(uint source,int n,out uint buffer);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourcePlay(uint source);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourcePause(uint source);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourceStop(uint source);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourcef(uint source,int key,float value);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSourcei(uint source,int key,int value);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alSource3i(uint source,int key,int x,int y,int z);
    [DllImport(Lib,CallingConvention=CallingConvention.Cdecl)] internal static extern void alGetSourcei(uint source,int key,out int value);
}
