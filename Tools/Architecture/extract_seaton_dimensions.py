"""Retain table cells verbatim, flag ambiguous unit marks instead of repairing them."""
import re,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
src=ROOT/'work/architecture/seaton-plans.pdf'
raw=subprocess.check_output(['pdftotext','-f','5','-l','5','-layout',str(src),'-']).decode()
rows=[]
def metric(value):
    m=re.fullmatch(r"(\d+)'(?:(\d+(?:\.\d+)?|\.\d+)\")?",value)
    if not m:return None
    return round((float(m[1])*12+float(m[2] or 0))*.0254,5)
for line in raw.splitlines():
    for match in re.finditer(r'(?:^|\s{2,})(\d+)\s+(\S+)\s+(\S+)',line):
        room,length,width=match.groups()
        if not any(c in length for c in ["'",'"']) and length!='Storage':continue
        a,b=metric(length),metric(width)
        rows.append(dict(room=room,lengthText=length,widthText=width,lengthMetres=a,widthMetres=b,sourcePage=5,status='storage, no dimensions published' if length=='Storage' else 'ambiguous source unit marks; needs confirmation' if a is None or b is None else 'published dimensions'))
# Page 5's visible glyphs disagree with its embedded text for room 232.
# Preserve the extracted value as evidence, but do not choose either length for
# construction until the college confirms it. Visual review: seaton-5.png.
conflict=next(r for r in rows if r['room']=='232')
assert conflict['lengthText']=="13'9\"", 'Re-review source: room 232 text layer changed'
conflict.update(extractedLengthText=conflict['lengthText'],visualLengthText="23'9\"",
                lengthMetres=None,status='source conflict: visible length 23 feet 9 inches; embedded text 13 feet 9 inches; confirmation required')
assert len(rows)==len({r['room'] for r in rows})
assert next(r for r in rows if r['room']=='105')['widthMetres'] is None
assert next(r for r in rows if r['room']=='2')['lengthMetres']==4.1529
out=dict(source='https://www.albion.edu/wp-content/uploads/2022/02/Seaton-Floor-Plan-and-Room-Dimensions.pdf',page=5,status='Source table transcription. Not integrated into runtime walls. Ambiguous source unit marks are not guessed.',rooms=rows)
(ROOT/'Art/Architecture/Seaton-room-dimensions.json').write_text(json.dumps(out,indent=2)+'\n')
print(len(rows),'entries;',sum('ambiguous' in r['status'] for r in rows),'ambiguous unit marks; one visual/text conflict; one storage entry')
