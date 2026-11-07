#!/usr/bin/env python3
"""Developer test: real owned assembly + sparse file-layout fixture, never a public game payload."""
import argparse,hashlib,json,os,pathlib,shutil,subprocess,tempfile
p=argparse.ArgumentParser();p.add_argument('--original',required=True);p.add_argument('--alpha',required=True);p.add_argument('--dotnet',required=True);args=p.parse_args();repo=pathlib.Path(__file__).resolve().parents[1]
sha=lambda p:hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
with tempfile.TemporaryDirectory(prefix='bastion-beta-test-')as d:
 root=pathlib.Path(d)/'APPS/Bastion_RGDSPlus';shutil.copytree(repo/'APPS/Bastion_RGDSPlus',root)
 manifest=json.loads((root/'runtime/game-content-manifest.json').read_text())
 for item in manifest['files']:
  f=root/'game'/item['path'];f.parent.mkdir(parents=True,exist_ok=True)
  with f.open('wb')as out:out.truncate(item['size'])
 exe=root/'game/Bastion.exe';shutil.copy2(args.original,exe)
 cmd=['python3',str(root/'runtime/prepare-game.py'),str(root),'--dotnet',str(pathlib.Path(args.dotnet).resolve())];env=os.environ.copy();env['DOTNET_SYSTEM_GLOBALIZATION_INVARIANT']='1'
 def run(want=0):
  r=subprocess.run(cmd,env=env,capture_output=True,text=True,timeout=30);print(r.stdout+r.stderr);assert r.returncode==want
 run();assert sha(exe)==sha(args.alpha),'Clean patch differs from working Alpha1.5';assert any(sha(f)==sha(args.original)for f in(root/'original').glob('*.exe'))
 receipt=(root/'game/prepared-local.json').read_bytes();run();assert(root/'game/prepared-local.json').read_bytes()==receipt
 (root/'game/prepared-local.json').unlink();shutil.copy2(args.alpha,exe);run();assert sha(exe)==sha(args.alpha),'Migration duplicated hooks'
 before=exe.read_bytes();missing=root/'game'/manifest['files'][0]['path'];missing.unlink();run(3);assert exe.read_bytes()==before
 with missing.open('wb')as out:out.truncate(manifest['files'][0]['size'])
 corrupt=bytearray(pathlib.Path(args.original).read_bytes());corrupt[-1]^=1;exe.write_bytes(corrupt);(root/'game/prepared-local.json').unlink();run(3);assert exe.read_bytes()==corrupt
print('PASS: clean original patch equals Alpha1.5 byte-for-byte; backup, idempotence, existing Alpha migration, missing content and unsupported-build input preservation.')
