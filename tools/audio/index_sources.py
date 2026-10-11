"""Inventory decoded source files; measurements are not a listening review."""
import argparse, hashlib, json, math, sys
from pathlib import Path
p=argparse.ArgumentParser()
p.add_argument('root',type=Path)
p.add_argument('output',type=Path)
p.add_argument('--library-dir',type=Path)
a=p.parse_args()
if a.library_dir: sys.path.insert(0,str(a.library_dir))
import numpy as np
import soundfile as sf
items=[]
def db(value): return round(20*math.log10(max(float(value),1e-12)),3)
for path in sorted(a.root.rglob('*')):
    if path.suffix.lower() not in ('.wav','.ogg','.flac'): continue
    data,rate=sf.read(path,dtype='float32',always_2d=True)
    peak=float(np.max(np.abs(data))) if data.size else 0
    items.append(dict(file=path.relative_to(a.root).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        seconds=round(len(data)/rate,4),rate=rate,channels=data.shape[1],
        sample_peak_dbfs=db(peak),rms_dbfs=db(np.sqrt(np.mean(data.astype(np.float64)**2))) if data.size else -240,
        samples_at_or_above_full_scale=int(np.count_nonzero(np.abs(data)>=1)),
        listening_review='not_done',artistic_acceptance=False))
a.output.parent.mkdir(parents=True,exist_ok=True)
a.output.write_bytes((json.dumps(dict(note='Sample peak and RMS only; not true peak, LUFS, or artistic approval.',files=items),ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
print(f'{len(items)} decoded files -> {a.output}')