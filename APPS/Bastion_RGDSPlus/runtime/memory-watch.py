#!/usr/bin/env python3
"""Bounded memory trace; includes native memory missed by managed GC statistics."""
import pathlib, signal, sys, time
pid = int(sys.argv[1])
running = True
def stop(*args):
    global running
    running = False
signal.signal(signal.SIGTERM, stop)
start = time.monotonic()
peak = 0
minimum = None
count = 0
print('seconds RSS_kB VmHWM_kB MemAvailable_kB', flush=True)
while running:
    try:
        status = dict(line.split(':', 1) for line in pathlib.Path('/proc/%d/status' % pid).read_text().splitlines() if ':' in line)
        if status.get('State', '').strip().startswith('Z'):
            break
        memory = dict(line.split(':', 1) for line in pathlib.Path('/proc/meminfo').read_text().splitlines())
        rss = int(status['VmRSS'].split()[0])
        hwm = int(status['VmHWM'].split()[0])
        available = int(memory['MemAvailable'].split()[0])
    except (OSError, KeyError, ValueError):
        break
    new_peak = hwm > peak
    peak = max(peak, rss, hwm)
    minimum = available if minimum is None else min(minimum, available)
    if new_peak or count % 20 == 0:
        print('%.1f %d %d %d' % (time.monotonic() - start, rss, hwm, available), flush=True)
    count += 1
    time.sleep(0.5)
print('PeakRSS_kB=%d MinimumMemAvailable_kB=%s' % (peak, minimum), flush=True)
