"""Review the staged geometry; not a source drawing or runtime verification."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
p=json.loads((ROOT/'Art/Architecture/Ingham-staged-plan.json').read_text())
im=Image.new('RGB',(1500,850),'#f4f0e6');d=ImageDraw.Draw(im)
f=ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',18);big=ImageFont.truetype('/System/Library/Fonts/Helvetica.ttc',28)
d.text((40,25),'INGHAM · measured-room fit under review',font=big,fill='#242c32')
d.text((40,65),'STAGING ONLY — dimensions of common spaces, envelope and stairs remain provisional',font=f,fill='#8c3c2c')
colors={'room':'#cbd9df','bath':'#d2dbc5','lounge':'#e6d4b8','unresolvedStair':'#e8bca7'}
for i,level in enumerate(p['floors']):
 x0=45+i*750;z0=170;scale=45
 def pt(x,z):return (x0+(x+p['width']/2)*scale,z0+(z+p['depth']/2)*scale)
 d.text((x0,120),level['name']+' floor · west / Ingham Street at top',font=f,fill='#242c32')
 d.rectangle((x0,z0,x0+p['width']*scale,z0+p['depth']*scale),fill='white',outline='#333333',width=3)
 for s in level['spaces']:
  x,y=pt(s['x']-s['width']/2,s['z']-s['depth']/2)
  d.rectangle((x,y,x+s['width']*scale,y+s['depth']*scale),fill=colors[s['kind']],outline='#333333',width=2)
  label=s['name'] if s['kind']=='room' else 'Living room' if s['kind']=='lounge' else 'Bath' if s['kind']=='bath' else 'STAIRS\npending'
  if s['kind']=='room':label+='\n'+str(round(s['clearWidth'],3))+' × '+str(round(s['clearDepth'],3))+' m'
  d.multiline_text((x+8,y+10),label,font=f,fill='#242c32',spacing=5)
 for door in level['doors']:
  x,z=door['x'],door['z'];half=door['width']/2
  ends=(pt(x-half,z),pt(x+half,z)) if door['side'] in ('north','south') else (pt(x,z-half),pt(x,z+half))
  d.line(ends,fill='#168674',width=7)
 d.text((x0,690),str(len(level['doors']))+' connected doorway approaches (2D check)',font=f,fill='#168674')
d.text((45,740),'Green = proposed doorway. Exact widths, offsets, wall thickness and player traversal remain unverified.',font=f,fill='#242c32')
d.text((45,775),'Room rectangles omit source alcoves/projections. No live interior has been replaced.',font=f,fill='#8c3c2c')
im.save(sys.argv[1])
