#!/usr/bin/env python3
import pathlib,sys,zipfile
root=pathlib.Path(sys.argv[1])
with zipfile.ZipFile(root/'Bastion_TestLogs.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(root.glob('*.txt')):
        if p.is_file(): z.writestr(p.name,p.read_bytes()[-1048576:])
