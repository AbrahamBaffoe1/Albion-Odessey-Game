"""Extract Ingham's official table, retaining source text and uncertainty.
The two-page diagram is explicitly not to scale; no pixel-to-metre scale is inferred.
"""
import hashlib,json,re
from html.parser import HTMLParser
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
SOURCE='https://www.albion.edu/offices/community-living/living-on-campus/our-communities/ingham-hall/'
PDF='https://www.albion.edu/wp-content/uploads/2022/02/Ingham-Floor-Plan.pdf'
class Tables(HTMLParser):
    def __init__(self):super().__init__();self.rows=[];self.row=None;self.cell=None
    def handle_starttag(self,tag,attrs):
        if tag=='tr':self.row=[]
        if tag in ('td','th') and self.row is not None:self.cell=[]
    def handle_data(self,data):
        if self.cell is not None:self.cell.append(data)
    def handle_endtag(self,tag):
        if tag in ('td','th') and self.cell is not None:
            self.row.append(' '.join(''.join(self.cell).split()));self.cell=None
        if tag=='tr' and self.row is not None:self.rows.append(self.row);self.row=None

def metres(text):
    m=re.fullmatch(r"(\d+)'(?:\s*(\d+)\")?",text.strip())
    if not m:raise ValueError('Unrecognized source dimension: '+text)
    return round((int(m[1])*12+int(m[2] or 0))*.0254,5)

def extract():
    parser=Tables();parser.feed((ROOT/'work/architecture/ingham-source.html').read_text())
    header=['Room '+str(n) for n in [101,102,103,104,201,202,203,204]]
    index=parser.rows.index(header);values=parser.rows[index+1]
    assert len(values)==8
    rooms=[]
    for name,value in zip(header,values):
        a,b=re.split(r'\s*x\s*',value);number=name.split()[1]
        rooms.append(dict(room=number,lengthText=a,widthText=b,lengthMetres=metres(a),widthMetres=metres(b),sourceSection='Room Dimensions',sourcePage=None,diagramPage=1 if number.startswith('1') else 2,status='Published dimensions; axis orientation and measurement conventions unconfirmed',diagramOccupancy='single' if number in ['101','103','104'] else 'double'))
    assert rooms[0]['lengthMetres']==4.2672 and rooms[1]['widthMetres']==5.6896
    result=dict(building='Ingham Hall',source=SOURCE,planSource=PDF,planSHA256=hashlib.sha256((ROOT/'work/architecture/ingham-plans.pdf').read_bytes()).hexdigest(),status='Eight published measurements transcribed; not yet implemented in runtime geometry. Not a surveyed footprint.',rooms=rooms,
        planObservations={'firstFloor':{'bedrooms':['101','102','103','104'],'bathrooms':1,'livingRoom':True,'frontPorch':True},'secondFloor':{'bedrooms':['201','202','203','204'],'bathrooms':3},'orientation':{'porch':'Ingham Street (west)','rightSideOnDrawing':'Perry Street (north)'},'stairs':'Central stair with turning/winder geometry; first-floor drawing marks UP and DOWN, second-floor drawing also marks up and down. No basement or upper-level floor plan supplied.'},
        unresolved=['Current web page lists four double rooms and three singles; linked PDF labels five doubles and three singles. Do not infer current occupancy of each room from total capacity.','Neither page is drawn to scale. Room rectangles cannot determine a unique whole-building footprint.','Basement and any level above the second floor are undocumented by this PDF.','Stair rise, run, winder dimensions, floor heights, wall thickness, exact room-axis orientation, roof and exterior measurements remain unverified.'])
    (ROOT/'Art/Architecture/Ingham-room-dimensions.json').write_text(json.dumps(result,indent=2)+'\n')
    print('Ingham: 8 source measurements; 2 diagram floors; unresolved stair continuations and occupancy conflict retained.')
if __name__=='__main__':extract()
