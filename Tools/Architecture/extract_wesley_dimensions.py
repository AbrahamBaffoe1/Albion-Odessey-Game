"""Decode the PDF's non-Unicode numeric font; preserve published measurements.
Needs pdftotext. The room-label B glyph has no text mapping; infer B only in
an explicit common/A/B suite sequence, checked against rendered page five.
"""
import csv,json,re,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
source=ROOT/'work/realism/wesley-plans.pdf'
raw=subprocess.check_output(['pdftotext','-layout','-f','5','-l','5',str(source),'-']).decode()
mapping={ord(c):str(i) for i,c in enumerate('ÁÂÃÄÅÆÇÈÉÊ')}
mapping.update({ord('¾'):'#',ord('°'):"'",ord('±'):'"',ord('\x9f'):'-',ord('\x03'):' ',ord('\x04'):'A',ord('r'):'x'})
text=raw.translate(mapping).replace(']ZZ][','Common').replace(' /',' S')
rows=[];seen=set()
pattern=r'#(\d+)(.*?)\s+(\d+)\x27-?(\d+)"\s*x\s*(\d+)\x27-?(\d+)"'
for m in re.finditer(pattern,text):
    number,suffix,af,ai,bf,bi=m.groups();suffix=suffix.strip()
    if 'Common' in suffix:suffix=' Common'
    elif suffix=='A':suffix='A'
    elif 'S' in suffix:suffix='-S'
    elif number+' Common' in seen and number+'A' in seen:suffix='B'
    else:suffix=''
    label=number+suffix;seen.add(label)
    vals=list(map(int,[af,ai,bf,bi]));a=(vals[0]*12+vals[1])*.0254;b=(vals[2]*12+vals[3])*.0254
    rows.append(dict(room=label,dimension1=f'{af}\'-{ai}"',dimension2=f'{bf}\'-{bi}"',metres1=round(a,4),metres2=round(b,4),sourcePage=5))
assert len(rows)==len({r['room'] for r in rows}), 'Duplicate labels must be resolved against the page image'
assert next(r for r in rows if r['room']=='102')['metres1']==4.3688
assert next(r for r in rows if r['room']=='365-S')['dimension2']=='9\'-0"'
target=ROOT/'Art/Architecture/Wesley-room-dimensions.json'
target.write_text(json.dumps(dict(source='https://www.albion.edu/wp-content/uploads/2022/02/Wesley-Floor-Plan-and-Room-Dimensions.pdf',page=5,status='Published room dimensions, not a measured whole-building survey. Axis orientation and assignment to exterior walls need verification.',rooms=rows),indent=2)+'\n')
print(f'{len(rows)} published room measurements decoded, {len([r for r in rows if "Common" in r["room"]])} suite common rooms')
