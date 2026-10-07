import json,pathlib,re,urllib.request,urllib.parse
root=pathlib.Path(__file__).resolve().parents[1]
s=urllib.request.urlopen('https://albion.college-tour.com/').read().decode()
data=json.loads(next(x.strip().rstrip(',') for x in s.splitlines() if x.strip().startswith('[{"id": 36,')))
# Explicit correspondences: tour IDs and August 2025 campus-map IDs differ.
mapping={877:['1'],921:['2'],906:['6'],887:['7a'],886:['7'],901:['9'],888:['26'],889:['10'],922:['11'],890:['12'],892:['14'],893:['18k'],895:['18n'],896:['17'],897:['18p'],885:['5'],898:['18u'],899:['16'],900:['20','21'],902:['22'],903:['23'],904:['24'],905:['25'],928:['60'],929:['61'],930:['62'],931:['63'],932:['64'],933:['65'],934:['66'],935:['67'],937:['69'],936:['68'],938:['70'],918:['52'],919:['54'],920:['55'],907:['40'],908:['41'],909:['42'],910:['43'],911:['44'],912:['45'],914:['46'],1142:['48'],913:['47'],915:['49'],917:['50'],916:['51'],880:['76a','76b','76c'],881:['77'],883:['79'],884:['80'],927:['53']}
notes={
877:'Admissions welcomes campus visitors at 100 N. Hannah Street. Visitor parking is beside the center. Consult the official page for current visiting arrangements.',
921:'The tour identifies this location with financial aid and campus safety services. The game map calls it Cass Center; service locations should be reconfirmed before release.',
906:'Baldwin is a campus dining and gathering destination. Lower Baldwin was renovated in 2013; other dining spaces serve events and smaller groups.',
887:'The annex supports ceramics, including wheel work, hand-built pieces and kiln firing.',
886:'Art teaching, studios, galleries and an auditorium share this building. Its facilities cover painting, drawing, printmaking, sculpture, photography and digital work.',
901:'This campus service maintains and develops college facilities.',
888:'Ferguson was completed in 2002 as an administrative and student-services hub. The tour lists technology, academic and student-support offices here.',
889:'Goodrich hosted required chapel gatherings from 1958 into the late 1960s. Today the tour describes a large auditorium for college ceremonies and music, with teaching and practice spaces below.',
922:'The grounds team cares for campus landscaping and outdoor spaces throughout the year.',
890:'Herrick includes a mainstage theatre and a flexible black-box space, with nearby teaching and office rooms. Students from different majors can participate in productions.',
891:'Journey House is identified in the tour as a base for Intercultural Affairs, cultural programming and student leadership. Its game-map position has not yet been established.',
892:'Dickie Hall construction began in 1857 and finished fourteen years later. Former chapel and office space became part of the Kellogg Center, a campus gathering place with student services and entertainment.',
893:'Kresge supports life-science teaching and research. The tour describes biology facilities, specimen collections, chemistry labs and an adjoining greenhouse.',
895:'Norris contains lecture and teaching spaces along with scientific research equipment in the Dow Analytical Science Laboratory.',
896:'Olin brings together communication, psychology and education activities. Its teaching and research spaces include specialist psychology laboratories.',
897:'Palenske supports geology, physics, mathematics and computing. The tour identifies rooftop astronomy equipment, research labs and geological collections.',
885:'The historic observatory now also serves the Brown Honors Program. The tour dates it to 1883; the Physics Department history distinguishes construction beginning in 1883 from completion in 1884. Its historic telescope remains an important feature.',
898:'Putnam provides faculty offices and teaching and laboratory space for biology and chemistry, alongside computing facilities.',
899:'Robinson occupies the site of the original college building erected in 1843. It underwent a full renovation in 1992. The tour associates it with humanities and social-science classrooms and offices.',
900:'Stockwell opened in 1938 and Mudd in 1980. Together they support library study, research and archives. Stockwell’s ground-level learning commons was redesigned in 2011.',
902:'Umbrella House provides a meeting and support setting for multicultural student organizations.',
903:'Vulgamore, also known as North Hall, dates to 1854 and was renovated in 1993. The tour places humanities teaching and international education offices here.',
904:'The house honors James Welton, a 1904 graduate identified as Albion’s first African American alumnus. The Black Student Alliance house was rededicated to him in 2002.',
905:'Whitehouse provides trails and outdoor learning beside the Kalamazoo River. The tour describes an interpretive center and opportunities for fieldwork, art and environmental education.',
918:'The Octagon House contains two furnished apartments. The tour describes shared living, dining, kitchen and laundry facilities.',
919:'This Erie Street apartment offers furnished bedrooms and shared living, dining, cooking and laundry spaces.',
920:'The Michigan Avenue annex is described as student housing with shared living, dining, kitchen and laundry facilities.',
907:'Briton House provides furnished student apartments with kitchens and shared laundry facilities.',
908:'Burns Street offers furnished apartments and efficiencies, with kitchens and on-site laundry.',
909:'Dean is described as cooperative student housing. Residents share domestic responsibilities, including preparing meals and caring for common areas.',
910:'Fiske is associated with language living and learning. Its residential program encourages language practice and international awareness.',
911:'Ingham offers student bedrooms, furnished common areas, a kitchenette and laundry.',
912:'Karro Village, nicknamed The Mae, groups furnished apartments in four adjoining buildings. Shared living, kitchen and laundry facilities support residential life.',
914:'The tour lists student rooms and common areas at Mitchell Towers, nicknamed Twin. It does not establish a construction history or complete floor plan.',
1142:'The virtual campus tour identifies Munger Annex, also called E-House, as student housing near Munger Place. This entry describes its residential role without asserting an unverified construction date.',
913:'Munger Place provides furnished student apartments with living, dining and kitchen space and access to laundry.',
915:'Seaton is described as a four-story residence with double rooms, study lounges, a kitchenette and laundry. A dorm panorama is available.',
917:'Susanna Wesley Hall, nicknamed Susie, opened in 1926 as the college’s first dedicated residence building, initially for women. East and West additions and Kresge Dining Room followed in the 1950s. The tour describes four stories with double rooms and study lounges.',
916:'Whitehouse Hall has four stories. Its suites pair two double bedrooms with a shared bathroom; the tour also describes study and domestic facilities.',
880:'Davis combines soccer and lacrosse, baseball, softball and throwing facilities, connected by an entrance and walkway. The game represents its fields as three separate destinations.',
881:'Dow combines exercise, indoor sports and wellness spaces with aquatic and tennis facilities. Its tour panorama depicts the weight room.',
883:'Kresge Gymnasium hosts basketball and volleyball. Its brick facade also forms a backdrop for outdoor commencement on the Quad.',
884:'The stadium opened in 1976. The tour records renovation in 1999, field and track work in 2011, and lighting in 2012. It sits near the Kalamazoo River.',
927:'This Colonial Revival home was built in 1902 and acquired by the college in 1940. It later accommodated advancement offices and student housing before renovation in 2014 for use as the president’s residence.'}
chapters={928:('Beta',1887),929:('Beta Omicron',1889),930:('Phi',1915),931:('Zeta',1883),932:('Alpha Tau',1917),933:('Epsilon',1876),934:('Pi',1887),935:('Sigma Pi',1923),937:('Alpha Pi',1886),936:('Gamma Gamma',1895),938:('Omega',1926)}
for i,(ch,y) in chapters.items(): notes[i]=f'The tour dates Albion’s {ch} chapter to {y}. This is chapter history; the page does not establish the construction date or interior layout of its building.'
rows=[]
for m in data:
 for c in m['categories']:
  for p in c['pins']:
   media=[]
   for key,kind in [('images','photo'),('panoramas','panorama'),('videos','video')]:
    for a in p[key]:
     caption=('Photograph reference for '+p['name']+'.') if kind=='photo' else ('360-degree reference view of '+p['name']+'. Drag to look around the photographed viewpoint.') if kind=='panorama' else ('Video reference for '+p['name']+'. Use the official source page for the complete spoken transcript.')
     media.append({'label':p['name']+' · '+kind,'kind':kind,'url':a.get('path',a.get('url')),'caption':caption})
   rows.append({'id':str(p['id']),'name':p['name'],'category':c['name'],'campusIds':mapping.get(p['id'],[]),'summary':notes[p['id']],'source':'https://albion.college-tour.com/#'+urllib.parse.quote(p['name']),'media':media})
assert len(rows)==55 and len({i for r in rows for i in r['campusIds']})==sum(len(r['campusIds']) for r in rows)
(root/'Unity/Assets/Resources/CampusTour/catalog.json').write_text(json.dumps({'places':rows},indent=2))
print('CATALOG_OK',len(rows),'locations;',sum(len(r['campusIds']) for r in rows),'mapped destinations')
