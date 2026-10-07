import pymupdf,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=pymupdf.open(root/'work/architecture/nature-trails.pdf')[0]
colors={'Marsh':(.451,.298,0),'Rail':(.518,0,.659),'Prairie':(1,.490,.094),'River’s Edge':(0,.302,.659),'Beese Ecology':(.149,.451,0),'Wren':(.902,0,0)}
scale=321.8688/(734-568);origin=(380,297)
paths=[]
for d in p.get_drawings():
 c=d['color'];name=next((n for n,v in colors.items() if c and sum((a-b)**2 for a,b in zip(c,v))<.002),None)
 if not name or (d['width'] or 0)<1 or d['rect'].x1<300:continue
 segments=[];chain=[]
 for item in d['items']:
  if item[0]!='l':continue
  a,b=item[1:]
  if chain and math.dist(chain[-1],(a.x,a.y))>.05:segments.append(chain);chain=[]
  if not chain:chain.append((a.x,a.y))
  chain.append((b.x,b.y))
 if chain:segments.append(chain)
 for points in segments:
  paths.append({'name':name,'closedForRepairs':name=='Marsh','points':[{'x':round((x-origin[0])*scale,3),'z':round((origin[1]-y)*scale,3)} for x,y in points]})
assert set(x['name'] for x in paths)==set(colors)
r={'source':'https://www.albion.edu/wp-content/uploads/2021/09/whitehouse-nature-center-trail-map-1.pdf','mapDate':'2018-10-02','metresPerPdfPoint':scale,'status':'Vector map geometry; estimated scale-bar endpoints and visitor origin; no surveyed terrain or vegetation','paths':paths}
(root/'Unity/Assets/Resources/Nature/trails.json').write_text(json.dumps(r,separators=(',',':')))
print(len(paths),'paths;',sum(len(x['points']) for x in paths),'vertices; all six trail families')
