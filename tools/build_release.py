#!/usr/bin/env python3
"""Build the clean BYOG Beta ZIP. Downloads only the pinned official runtime."""
import argparse,hashlib,json,os,pathlib,shutil,tarfile,tempfile,urllib.request,zipfile
from audit_public import audit

REPO=pathlib.Path(__file__).resolve().parents[1]
VERSION='1.0.0-beta.1'
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def files(root):return [p for p in sorted(root.rglob('*'))if p.is_file() and '__pycache__'not in p.parts and p.suffix!='.pyc']
def runtime_into(destination,source):
    if source:shutil.copytree(source,destination)
    else:
        pin=json.loads((REPO/'tools/runtime-download.json').read_text())
        with tempfile.TemporaryDirectory()as td:
            archive=pathlib.Path(td)/'runtime.tar.gz';urllib.request.urlretrieve(pin['url'],archive)
            if hashlib.sha512(archive.read_bytes()).hexdigest()!=pin['hash']:raise ValueError('Official runtime SHA512 mismatch')
            destination.mkdir(parents=True)
            with tarfile.open(archive,'r:gz')as t:
                # Pinned official runtime: reject links and unsafe paths before extraction.
                for m in t.getmembers():
                    if m.issym()or m.islnk()or pathlib.PurePosixPath(m.name).is_absolute()or '..'in pathlib.PurePosixPath(m.name).parts:raise ValueError('Unsafe runtime archive member')
                t.extractall(destination)
    expected=json.loads((REPO/'tools/runtime-files.json').read_text())
    actual={p.relative_to(destination).as_posix():sha(p)for p in files(destination)}
    if actual!=expected:raise ValueError('Runtime differs from frozen ARM64 runtime hashes')
    (destination/'dotnet').chmod(0o755)

def build(output,runtime=None,icon=None):
    output=pathlib.Path(output).resolve();output.mkdir(parents=True,exist_ok=True)
    name='Bastion_RGDSPlus_Beta1_PERSONAL_Install.zip'if icon else 'Bastion_RGDSPlus_v1.0.0-beta.1.zip'
    with tempfile.TemporaryDirectory(prefix='bastion-beta-release-')as td:
        stage=pathlib.Path(td);shutil.copytree(REPO/'APPS',stage/'APPS',ignore=shutil.ignore_patterns('__pycache__','*.pyc'))
        root=stage/'APPS/Bastion_RGDSPlus';runtime_into(root/'runtime/dotnet',runtime)
        for p in(stage/'APPS').glob('*.sh'):p.chmod(0o755)
        if icon:shutil.copyfile(icon,stage/'APPS/Imgs/Bastion.png')
        for n in ['README.md','LICENSE','THIRD_PARTY_NOTICES.md','RIGHTS_AND_ARTWORK.md','BETA_TESTING.md','SUPPORT.md','CHANGELOG.md','RELEASE_NOTES_BETA1.md']:
            shutil.copyfile(REPO/n,stage/n)
        shutil.copytree(REPO/'docs',stage/'docs');shutil.copytree(REPO/'Source',stage/'Source',ignore=shutil.ignore_patterns('__pycache__','bin','obj'))
        text='BASTION RGDS+ BETA 1 — CLEAN INSTALL\n\nRead README.md before installation. Copy APPS into your TF2 Roms folder.\nSupply your own Linux Bastion.exe and complete Content folder in APPS/Bastion_RGDSPlus/game/.\nNever replace bundled DLLs with PC DLLs. Existing users: read docs/UPGRADE_FROM_ALPHA.md and preserve savedata.\n'
        if icon:text+='\nPERSONAL COPY — includes your supplied artwork. DO NOT UPLOAD THIS ZIP TO GITHUB.\nUse the separately supplied public release ZIP for publication. No commercial game data is included here.\n'
        else:text+='\nPUBLIC BYOG PACKAGE — no commercial game or supplied artwork is included.\n'
        (stage/'README_FIRST.txt').write_text(text,encoding='utf-8')
        editable={'alsoft.conf','rgds-controller.txt','bloom.txt','catchup-steps.txt','disable-lower-screen.txt','performance-mode.txt','resolution.txt','texture-decode.txt','texture-limit.txt','texture-storage.txt'}
        manifest=[]
        for p in files(root):
            rel=p.relative_to(root).as_posix()
            if rel=='runtime/install-manifest.json'or(rel.startswith('runtime/')and p.name in editable):continue
            manifest.append({'path':rel,'size':p.stat().st_size,'sha256':sha(p)})
        (root/'runtime/install-manifest.json').write_text(json.dumps({'release':VERSION,'files':manifest},indent=2)+'\n')
        audit(stage,allow_icon=bool(icon))
        with zipfile.ZipFile(output/name,'w',zipfile.ZIP_DEFLATED,compresslevel=9)as z:
            for p in files(stage):z.write(p,p.relative_to(stage).as_posix())
    audit(output/name,allow_icon=bool(icon))
    checksum=output/(name[:-4]+'_SHA256.txt');checksum.write_text(sha(output/name)+'  '+name+'\n')
    print(str(output/name));print(checksum.read_text().strip());return output/name

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',default='release-output');p.add_argument('--runtime-dir',type=pathlib.Path);p.add_argument('--private-icon',type=pathlib.Path,help='Personal copy only; never upload its ZIP to GitHub');a=p.parse_args();build(a.output,a.runtime_dir,a.private_icon)
