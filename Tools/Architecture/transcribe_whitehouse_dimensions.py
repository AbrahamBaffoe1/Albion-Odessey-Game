"""Manual transcription from raster table, official PDF page 5.
Asterisks/apostrophes in this scan separate feet and inches. Bare integers are feet.
"""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
data='''21 21 10'11
23 21 10'11
25 21 11
27 22'1 10'11
31 21 11'2
32 21 11
33 21 10'10
34 21 11
35 21 11
36 21 11
37 21 10'10
38 21 11
41 21 11
42 21 11
43 21 11
44 21'1 10'11
45 21 10'11
46 21 10'11
47 21 10'10
48 21 11'2
119 20*7 12*7
121 20*6 10*11
123 20*6 10*11
125 20*6 10*11
127 21*5 10*11
129 21*8 10*11
131 21*5 10*11
132 20*6 10*11
133 20*6 10*11
134 20*6 11
135 20*6 10*11
136 20*6 10*11
137 20*6 10*10
138 20*6 10*11
141 20*6 11
142 20*6 10*10
143 20*6 10*10
144 20*6 10*10
145 20*6 10*11
146 20*6 10*10
147 20*6 11
148 20*6 11*2
200 14*6 11*3
211 20*8 10*9
212 20*8 10*9
213 20*8 10*11
214 20*8 11
215 20*8 10*10
216 20*8 10*10
217 20*9 10*11
218 20*9 10*11
219 20*9 12*9
221 20*8 11
223 20*8 10*11
225 20*8 10*11
227 21*8 11
228 20*8 10*11
229 21*8 10*11
231 21*8 11
232 20*8 10*11
233 20*9 11
234 20*8 10*11
235 20*8 10*11
236 20*8 10*11
237 20*8 10*11
238 20*6 10*11
241 20*8 10*11
242 20*8 10*11
243 20*9 10*11
244 20*8 11
245 20*7 11
246 20*8 10*11
247 20*7 10*9
248 20*8 11*1
311 20*8 10*10
312 20*8 10*10
313 20*8 11
314 20*8 10*11
315 20*8 10*11
316 20*8 10*11
317 20*8 10*11
318 20*8 11*1
319 20*8 12*9
321 20*8 10*10
323 20*9 10*11
325 20*8 10*11
326 20*7 10*11
327 20*8 11
328 20*8 11*1
329 21*8 10*11
331 21*8 11
332 20*7 11*1
333 20*8 10*11
334 20*7 10*11
335 20*8 10*11
336 20*7 10*11
337 20*8 11
338 20*7 10*9
341 20*8 11
342 20*6 10*11
343 20*6 10*11
344 20*8 10*11
345 20*8 10*11
346 20*7 11
347 20*7 10*10
348 20*8 11'''
def metres(s):
 p=re.split("['*]",s);return round((int(p[0])*12+(int(p[1]) if len(p)>1 else 0))*.0254,5)
rooms=[dict(room=n,lengthText=l,widthText=w,lengthMetres=metres(l),widthMetres=metres(w),sourcePage=5) for n,l,w in (line.split() for line in data.splitlines())]
assert len(rooms)==len({r['room'] for r in rooms})
plan=json.loads((ROOT/'Unity/Assets/Resources/CampusCraft/whitehouse-plan.json').read_text())
ids={str(int(re.match(r'\d+',s['name'])[0])) for f in plan['floors'] for s in f['spaces'] if re.match(r'\d+',s['name'])}
assert all(r['room'] in ids for r in rooms)
obj=dict(source=plan['source'],page=5,status='Manually transcribed published table, used by the runtime plan generator as clear rectangular dimensions. Measurement method and wall construction are unverified. Room 226 is present on the floor diagram but absent from this table.',rooms=rooms)
(ROOT/'Art/Architecture/Whitehouse-room-dimensions.json').write_text(json.dumps(obj,indent=2)+'\n')
print(len(rooms),'published dimension rows; diagram-only room 226 retained without invented measurement')
