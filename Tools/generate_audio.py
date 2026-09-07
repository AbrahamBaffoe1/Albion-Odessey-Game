"""Create original deterministic game cues: 48 kHz, 16-bit mono WAV.
No samples, recordings, network services or third-party music are used.
"""
import math, wave, struct, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Unity/Assets/Resources/Audio'
OUT.mkdir(parents=True,exist_ok=True)
RATE=48000
# Distinct motifs/timbres per kind; no random pitch or sample variants.
CUES=[
 ('Memory',[79,86],.105,'bell'),
 ('FirstDiscovery',[72,76,79],.13,'bell'),
 ('ThreePlaces',[60,67,72,76],.12,'marimba'),
 ('EightFloors',[67,71,74,79],.14,'glass'),
 ('AllBuildingKinds',[60,64,67,72,79],.12,'marimba'),
 ('GenerousKeeper',[65,69,72,77],.14,'soft'),
 ('BeaconComplete',[60,67,72,76,79,84],.16,'glass'),
 ('HistoryLesson',[62,66,69,74],.12,'bell'),
 ('AstronomyLesson',[69,76,81,88],.14,'glass'),
 ('DesignLesson',[64,68,71,76],.11,'marimba'),
 ('GardenBuilt',[72,79,76],.075,'soft'),
 ('LibraryBuilt',[55,62,67],.08,'marimba'),
 ('ObservatoryBuilt',[81,88,93],.085,'glass'),
 ('HallBuilt',[48,55,60],.085,'marimba'),
 ('Reclaim',[74,67],.085,'soft'),
 ('Contribution',[67,74,79],.075,'bell'),
 ('CourseCreated',[60,64,71],.09,'soft'),
 ('StudentEnrolled',[76,79],.075,'marimba'),
 ('ClassStarted',[67,72,76,79],.09,'soft'),
 ('PaperThrow',[93,69],.035,'paper'),
]
PARTIALS={'bell':[(1,1),(2.76,.15),(5.4,.035)],'glass':[(1,1),(2,.25),(4,.07)],'marimba':[(1,1),(3,.16),(5,.035)],'soft':[(1,1),(2,.16)],'paper':[(1,1),(1.41,.5),(2.31,.25)]}
manifest=[]
for name,notes,step,kind in CUES:
    tail=.34 if kind not in ('glass','paper') else .56 if kind=='glass' else .11
    duration=(len(notes)-1)*step+tail+.025
    samples=[0.0]*int(RATE*duration)
    for n,note in enumerate(notes):
        frequency=440*2**((note-69)/12);start=int((.008+n*step)*RATE)
        for j in range(int(tail*RATE)):
            t=j/RATE
            attack=min(t/.008,1)
            release=min((tail-t)/.05,1)
            env=attack*release*math.exp(-t*(8 if kind!='glass' else 5))
            tone=sum(gain*math.sin(2*math.pi*frequency*ratio*t) for ratio,gain in PARTIALS[kind])
            samples[start+j]+=tone*env
    peak=max(abs(v) for v in samples)
    samples=[v/peak*.66 for v in samples]
    data=struct.pack('<'+'h'*len(samples),*(round(v*32767) for v in samples))
    with wave.open(str(OUT/(name+'.wav')),'wb') as f:
        f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(data)
    manifest.append({'cue':name,'notes':notes,'timbre':kind,'duration':len(samples)/RATE,'sha256':hashlib.sha256(data).hexdigest()})
(OUT/'catalog.json').write_text(json.dumps({'sample_rate':RATE,'original_audio':True,'cues':manifest},indent=2)+'\n')
print('AUDIO_GENERATED',len(manifest),'original cues')
