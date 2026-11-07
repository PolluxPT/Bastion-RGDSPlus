#!/usr/bin/env python3
"""Check redistributed port files; proprietary content is verified by prepare-game.py."""
import hashlib,json,pathlib,sys
root=pathlib.Path(sys.argv[1]);manifest=json.loads((root/'runtime/install-manifest.json').read_text());bad=[]
for item in manifest['files']:
    p=root/item['path']
    if not p.is_file()or p.stat().st_size!=item['size']:bad.append(item['path'])
    elif '--hash'in sys.argv and hashlib.sha256(p.read_bytes()).hexdigest()!=item['sha256']:bad.append(item['path'])
if bad:
    print('Port installation is incomplete or damaged. Re-extract the complete Beta 1 release ZIP.')
    print('\n'.join(bad[:20]));sys.exit(3)
print('Beta 1 port installation verified: %d files. Supply the original Linux game separately.'%len(manifest['files']))
