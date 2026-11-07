using Microsoft.Xna.Framework.Content;
var data=File.ReadAllBytes(args[0]);
for(int offset=0;offset<data.Length-14;offset++) {
 if(data[offset]!='X' || data[offset+1]!='N' || data[offset+2]!='B')continue;
 int length=BitConverter.ToInt32(data,offset+6);if(length<14 || offset+length>data.Length)continue;
 using var payload=new MemoryStream();
 if((data[offset+5]&128)!=0) {
  int expected=BitConverter.ToInt32(data,offset+10);
  using var compressed=new MemoryStream(data,offset+14,length-14);
  var decoder=new LzxDecoder(16);long pos=0;
  while(pos<length-14) {
   int hi=compressed.ReadByte(),lo=compressed.ReadByte();int block=(hi<<8)|lo,frame=32768;
   if(hi==255) { frame=(lo<<8)|compressed.ReadByte();block=(compressed.ReadByte()<<8)|compressed.ReadByte();pos+=5; }else pos+=2;
   if(block==0 || frame==0)break;
   decoder.Decompress(compressed,block,payload,frame);pos+=block;compressed.Position=pos;
  }
  if(payload.Length!=expected)throw new Exception("Length mismatch");
 }else payload.Write(data,offset+10,length-10);
 payload.Position=0;using var r=new BinaryReader(payload);
 int count=r.Read7BitEncodedInt();var types=new List<string>();for(int i=0;i<count;i++){types.Add(r.ReadString());r.ReadInt32();}
 r.Read7BitEncodedInt();r.Read7BitEncodedInt();
 if(types.Any(t=>t.Contains("Texture2DReader"))) {
  int format=r.ReadInt32(),w=r.ReadInt32(),h=r.ReadInt32(),levels=r.ReadInt32();
  Console.WriteLine($"XNB offset={offset} version={data[offset+4]} format={format} {w}x{h} levels={levels}");
  if(args.Length>1 && offset==114) {int bytes=r.ReadInt32();File.WriteAllBytes(args[1],r.ReadBytes(bytes));}
 }
 offset+=length-1;
}
