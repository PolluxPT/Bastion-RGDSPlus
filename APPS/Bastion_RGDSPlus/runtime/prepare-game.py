#!/usr/bin/env python3
"""Prepare the owner's supplied Linux assembly locally. No game is downloaded."""
import argparse,hashlib,json,os,pathlib,struct,subprocess,sys,zlib

def digest(path):
    h=hashlib.sha256()
    with pathlib.Path(path).open('rb')as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def icon_from_bmp(source,destination):
    if destination.exists()or not source.is_file():return
    data=source.read_bytes();off=struct.unpack_from('<I',data,10)[0];size,w,h,planes,bits,compression=struct.unpack_from('<IiiHHI',data,14)
    if data[:2]!=b'BM'or not(0<w<=2048 and 0<abs(h)<=2048 and planes==1 and bits in(24,32)and compression in(0,3)):return
    stride=((w*bits+31)//32)*4;height=abs(h)
    if len(data)<off+stride*height:return
    masks=(0xff0000,0xff00,0xff,0xff000000 if compression==3 else 0)
    if compression==3:masks=struct.unpack_from('<IIII',data,54)if size>=56 else (*struct.unpack_from('<III',data,54),0)
    shifts=[(m&-m).bit_length()-1 if m else 0 for m in masks]
    raw=bytearray()
    for y in range(height):
        raw.append(0);row=off+(height-1-y if h>0 else y)*stride
        for x in range(w):
            pixel=int.from_bytes(data[row+x*(bits//8):row+(x+1)*(bits//8)],'little')
            for channel,(mask,shift)in enumerate(zip(masks,shifts)):
                raw.append(((pixel&mask)>>shift)*255//(mask>>shift)if mask else (255 if channel==3 else 0))
    def chunk(kind,payload):return struct.pack('>I',len(payload))+kind+payload+struct.pack('>I',zlib.crc32(kind+payload)&0xffffffff)
    png=b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,height,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(bytes(raw),9))+chunk(b'IEND',b'')
    destination.parent.mkdir(parents=True,exist_ok=True);destination.write_bytes(png)
    print('Created local menu icon from your supplied Bastion.bmp. Do not redistribute it without rights.')

def check_content(root):
    manifest=json.loads((root/'runtime/game-content-manifest.json').read_text());missing=[]
    for item in manifest['files']:
        p=root/'game'/item['path']
        if not p.is_file()or p.stat().st_size!=item['size']:missing.append(item['path'])
    if missing:raise ValueError('Linux Content folder is missing/incomplete or a different build (%d files). First entries: %s. Copy the COMPLETE purchased Linux Content folder to %s.'%(len(missing),', '.join(missing[:8]),root/'game/Content'))

def prepare(root,dotnet=None):
    root=pathlib.Path(root).resolve();game=root/'game';exe=game/'Bastion.exe'
    if not exe.is_file():raise ValueError('Copy your legally obtained Linux Bastion.exe and Content folder into '+str(game)+' and launch Bastion.sh again. The Windows version is not supported.')
    check_content(root);sha=digest(exe);receipt=game/'prepared-local.json'
    try:previous=json.loads(receipt.read_text())
    except (OSError,ValueError):previous={}
    if previous.get('patched_sha256')==sha:
        print('Local game preparation already verified.')
    else:
        temp=game/'Bastion.prepare.exe';backup=root/'original'/('Bastion-'+sha+'.exe')
        command=[str(dotnet or root/'runtime/dotnet/dotnet'),str(root/'runtime/patcher/BastionRGDS.Patcher.dll'),str(exe),str(temp),str(game/'BastionRGDS.Host.dll')]
        try:
            result=subprocess.run(command,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
            print(result.stdout,end='')
            if result.returncode!=0 or not temp.is_file():raise ValueError('Local game preparation failed. Check the supported Linux build and Preparation.txt; your input was left unchanged.')
            backup.parent.mkdir(parents=True,exist_ok=True)
            if not backup.exists():
                import shutil
                shutil.copy2(exe,backup)
            patched=digest(temp);os.replace(temp,exe)
            state={'release':'1.0.0-beta.1','input_sha256':sha,'patched_sha256':patched,'backup':str(backup.relative_to(root))}
            tmp=receipt.with_suffix('.new');tmp.write_text(json.dumps(state,indent=2)+'\n');os.replace(tmp,receipt)
        finally:
            if temp.exists():temp.unlink()
    try:icon_from_bmp(game/'Bastion.bmp',root.parent/'Imgs/Bastion.png')
    except (OSError,ValueError,struct.error)as ex:print('Optional local icon not generated:',ex)
    print('Game content and local ARM64 integration are ready.')

def main():
    p=argparse.ArgumentParser();p.add_argument('root');p.add_argument('--dotnet',help='Developer test override; normal installs use the bundled ARM64 runtime');args=p.parse_args()
    try:prepare(args.root,args.dotnet);return 0
    except (OSError,ValueError)as ex:print('PREPARATION:',ex,file=sys.stderr);return 3
if __name__=='__main__':sys.exit(main())
