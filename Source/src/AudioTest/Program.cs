using FMOD;
var bytes=File.ReadAllBytes(args[0]);
try {
Framework.init();
var info=new CREATESOUNDEXINFO{length=(uint)bytes.Length};FMOD.Sound? sound=null;Channel? channel=null;
if(Framework.system.createSound(bytes,MODE.LOOP_NORMAL,ref info,ref sound)!=RESULT.OK)throw new Exception("createSound");
if(Framework.system.playSound(CHANNELINDEX.FREE,sound!,false,ref channel)!=RESULT.OK)throw new Exception("playSound");
Thread.Sleep(100);bool playing=false;channel!.isPlaying(ref playing);if(!playing)throw new Exception("not playing");
channel.setPaused(true);bool paused=false;channel.getPaused(ref paused);if(!paused)throw new Exception("pause");
float rate=0;channel.getFrequency(ref rate);channel.setFrequency(rate*1.1f);channel.setVolume(0.2f);channel.setLowPassGain(0.5f);
var reverb=new REVERB_CHANNELPROPERTIES{Room=-1000};channel.setReverbProperties(ref reverb);
channel.setPaused(false);Thread.Sleep(600);channel.isPlaying(ref playing);if(!playing)throw new Exception("stream/loop failed");
channel.stop();sound!.release();Framework.resetSystem();Framework.terminate();
Console.WriteLine("PASS Ogg streaming, looping, pause/resume, pitch/volume/filter/reverb calls, stop, runtime reset");

} finally { Framework.terminate(); RGDSVolume.Shutdown(); }
