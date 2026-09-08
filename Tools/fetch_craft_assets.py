from pathlib import Path
import urllib.request,json,hashlib,concurrent.futures
root=Path(__file__).resolve().parents[1];res=root/'Unity/Assets/Resources/CampusCraft';art=root/'Art';records=[]
def get(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'AlbionOdysseyAssetSetup/1.0'}),timeout=50).read()
def grab_texture(asset):
 meta=json.loads(get('https://api.polyhaven.com/files/'+asset));out=[]
 for key,suffix in [('Diffuse','Color'),('nor_gl','Normal'),('Rough','Rough')]:
  item=meta[key]['1k']['jpg'];data=get(item['url']);(res/(asset+'_'+suffix+'.jpg')).write_bytes(data);out.append({'file':asset+'_'+suffix+'.jpg','source':item['url'],'sha256':hashlib.sha256(data).hexdigest(),'license':'CC0','author':'Poly Haven'})
 return out
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
 for result in pool.map(grab_texture,['red_brick_03','concrete_pavement','aerial_grass_rock']): records.extend(result)
# Download only public Standard editions; validate archive signatures before saving.
import io,zipfile
for slug,upload,name,folder in [('universal-base-characters','15861669','UniversalBaseCharacters.zip','CharacterSource'),('universal-animation-library','17958403','UniversalAnimationLibrary.zip','AnimationSource')]:
 request=urllib.request.Request('https://quaternius.itch.io/'+slug+'/file/'+upload,data=b'',headers={'User-Agent':'AlbionOdysseyAssetSetup/1.0'})
 info=json.load(urllib.request.urlopen(request,timeout=30));data=get(info['url'])
 if not data.startswith(b'PK'):raise ValueError('Publisher did not return an asset archive')
 destination=art/'References';destination.mkdir(exist_ok=True);(destination/name).write_bytes(data)
 with zipfile.ZipFile(io.BytesIO(data)) as archive:
  if archive.testzip() is not None:raise ValueError('Asset archive integrity failure')
  archive.extractall(destination/folder)
 records.append({'file':name,'source':'https://quaternius.itch.io/'+slug,'sha256':hashlib.sha256(data).hexdigest(),'license':'CC0','author':'Quaternius'})
(art/'AssetProvenance.json').write_text(json.dumps(records,indent=2))
print('CRAFT_ASSETS_OK',len(records))
