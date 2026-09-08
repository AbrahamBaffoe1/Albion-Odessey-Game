"""Recreate the building studio's ten original 48 kHz cues."""
import math,wave,struct,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'Unity/Assets/Resources/BuildingDesigner'
out.mkdir(parents=True,exist_ok=True)
catalog=[]
for kind in range(10):
    notes=[48+kind*3,55+kind*3] if kind<8 else [72,64] if kind==8 else [60,64,72]
    samples=[0.0]*24000
    for i,note in enumerate(notes):
        frequency=440*2**((note-69)/12)
        for j in range(14000):
            t=j/48000
            envelope=min(t/.008,1)*min((14000-j)/2400,1)*math.exp(-t*14)
            samples[i*4000+j]+=envelope*(math.sin(2*math.pi*frequency*t)+.15*math.sin(2*math.pi*frequency*3*t))
    peak=max(map(abs,samples))
    data=struct.pack('<24000h',*[round(v/peak*.52*32767) for v in samples])
    with wave.open(str(out/f'Studio{kind}.wav'),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(48000);f.writeframes(data)
    catalog.append({'kind':kind,'notes':notes,'sha256':hashlib.sha256(data).hexdigest()})
assert len({x['sha256'] for x in catalog})==10
(out/'catalog.json').write_text(json.dumps(catalog,indent=2))
print('STUDIO_AUDIO_OK: ten distinct, reproducible original cues')
