#!/usr/bin/env python3
"""Audit release/repository payload boundaries. This is not legal clearance."""
import argparse, io, pathlib, struct, tarfile, zipfile

def check(name, data, allow_icon=False):
    p=pathlib.PurePosixPath(name); parts=[s.lower() for s in p.parts]; base=p.name.lower()
    problems=[]
    if '..' in parts or p.is_absolute(): problems.append('unsafe archive path')
    if base.endswith(('.exe','.apk','.zim','.7z','.sav')): problems.append('commercial/installed/archive payload not allowed')
    if base.endswith(('.zip','.zip.001','.zip.002','.zip.003','.zip.004')): problems.append('nested game/release archive not allowed')
    if base=='bastion.bmp' or ('imgs' in parts and base=='bastion.png' and not allow_icon): problems.append('game/supplied artwork not allowed in public pack')
    if ('game' in parts and 'content' in parts) or ('original' in parts and base!='readme.txt'): problems.append('owner game files/backups not allowed')
    if any(s in parts for s in ('savedata','logs','__pycache__','.git','bin','obj')): problems.append('installed/developer state not allowed')
    if base=='prepared-local.json' or base.endswith(('.pyc','.pdb')): problems.append('local/developer output not allowed')
    if data[:4]==b'\x7fELF' and (len(data)<20 or data[4]!=2 or data[5]!=1 or struct.unpack_from('<H',data,18)[0]!=183): problems.append('non-ARM64 ELF executable')
    if base.endswith('.tar.gz'):
        with tarfile.open(fileobj=io.BytesIO(data),mode='r:gz')as t:
            for m in t.getmembers():
                if m.issym()or m.islnk():
                    # Relative source symlinks are permitted only if they stay in the source tree.
                    if pathlib.PurePosixPath(m.linkname).is_absolute()or '..' in pathlib.PurePosixPath(m.linkname).parts: problems.append('unsafe source symlink '+m.name)
                elif m.isfile(): problems.extend(m.name+': '+e for e in check(m.name,t.extractfile(m).read(),allow_icon))
    return problems

def audit(path,allow_icon=False):
    path=pathlib.Path(path);errors=[];count=0
    if path.is_dir():
        entries=((str(p.relative_to(path)).replace('\\','/'),p.read_bytes())for p in sorted(path.rglob('*'))if p.is_file() and '.git' not in p.relative_to(path).parts)
        for name,data in entries:
            count+=1;errors.extend(name+': '+e for e in check(name,data,allow_icon))
    else:
        with zipfile.ZipFile(path)as z:
            if z.testzip():errors.append('ZIP CRC failed')
            for entry in z.infolist():
                if entry.is_dir():continue
                count+=1;errors.extend(entry.filename+': '+e for e in check(entry.filename,z.read(entry),allow_icon))
    if errors:raise ValueError('\n'.join(errors))
    print('PASS: %d files; no game payload, installed state or disallowed artwork; distributed ELF files are ARM64.'%count)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('path');p.add_argument('--allow-private-icon',action='store_true');a=p.parse_args()
    try:audit(a.path,a.allow_private_icon)
    except (ValueError,tarfile.TarError,zipfile.BadZipFile)as e:p.exit(1,'FAIL: '+str(e)+'\n')
