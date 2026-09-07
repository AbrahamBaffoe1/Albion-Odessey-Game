"""Check the actual audio files and their stable enum-to-asset mapping."""
import hashlib,json,math,re,struct,wave
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=root/'Unity/Assets/Resources/Audio'
catalog=json.loads((folder/'catalog.json').read_text())
source=(root/'Unity/Assets/Scripts/OdysseyFeedback.cs').read_text()
keys=re.search(r'public enum OdysseyCue\s*\{([^}]+)',source).group(1)
keys=[v.strip() for v in keys.split(',') if v.strip()]
assert [v['cue'] for v in catalog['cues']]==keys
fingerprints=set()
for cue in catalog['cues']:
    with wave.open(str(folder/(cue['cue']+'.wav')),'rb') as f:
        assert f.getnchannels()==1 and f.getsampwidth()==2 and f.getframerate()==48000
        assert f.getnframes()/48000==cue['duration']
        data=f.readframes(f.getnframes())
    digest=hashlib.sha256(data).hexdigest()
    assert digest==cue['sha256'] and digest not in fingerprints
    fingerprints.add(digest)
    samples=struct.unpack('<'+'h'*(len(data)//2),data)
    peak=max(abs(v) for v in samples)/32768
    rms=math.sqrt(sum((v/32768)**2 for v in samples)/len(samples))
    assert .05<peak<.8 and .01<rms<.5,(cue['cue'],peak,rms)
    assert abs(samples[0])<2 and abs(samples[-1])<2,'No discontinuous start/end'
    assert max(abs(b-a) for a,b in zip(samples,samples[1:]))<12000,'Unexpected waveform discontinuity'
print(f'AUDIO_OK: {len(keys)} unique original WAV cues, valid mapping, levels, envelopes and fingerprints')
