using System;
using System.IO;
using System.Collections;
using UnityEngine;
namespace AlbionOdyssey
{
    // Isolated architectural study, never installed as the current library.
    // Overall width follows the 1939 plan; other dimensions are provisional.
    public sealed class StockwellFacadeStudy:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-stockwellFacadeStudy")>=0)new GameObject("Stockwell facade study").AddComponent<StockwellFacadeStudy>();}
        Transform root;Material brick,stone,glass,metal;
        GameObject Box(string name,Vector3 at,Vector3 size,Material material,bool solid=false)
        {
            var obj=KeeperAvatar.Part(root,name,PrimitiveType.Cube,at,size,material,solid);
            if(material.mainTexture!=null)
            {
                var mesh=obj.GetComponent<MeshFilter>().mesh;
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    var point=Vector3.Scale(vertices[i],size)+at;
                    uv[i]=Mathf.Abs(normals[i].y)>.7f?new Vector2(point.x,point.z)/2:
                        Mathf.Abs(normals[i].x)>.7f?new Vector2(point.z,point.y)/2:new Vector2(point.x,point.y)/2;
                }
                mesh.uv=uv;mesh.RecalculateTangents();
            }
            return obj;
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;OdysseyGame game=null;float end=Time.realtimeSinceStartup+90;
            while(game==null||!game.Ready){if(Time.realtimeSinceStartup>end){Application.Quit(1);yield break;}game=FindAnyObjectByType<OdysseyGame>();yield return null;}
            game.shell.Play();game.player.controls=false;
            root=new GameObject("Stockwell source-based facade study").transform;root.position=new Vector3(4000,100,4000);
            brick=CraftModel.Surface("Stockwell study brick",new Color(.68f,.39f,.26f),"red_brick_03");
            stone=CraftModel.Surface("Stockwell study pale sandstone",new Color(.83f,.78f,.65f));
            glass=CraftModel.Surface("Stockwell study glazing",new Color(.15f,.23f,.28f));
            metal=CraftModel.Surface("Stockwell dark ironwork",new Color(.07f,.08f,.08f));
            const float width=36.576f,entry=2.1f,top=11.8f;
            Box("Study ground",new Vector3(0,-.15f,6),new Vector3(48,.3f,44),CraftModel.Surface("Study lawn",new Color(.22f,.30f,.16f)),true);
            Box("Library facade mass",new Vector3(0,top/2,.3f),new Vector3(width,top,.6f),brick);
            BuildHistoricalShell(width,top);
            Box("Sandstone foundation band",new Vector3(0,.5f,-.05f),new Vector3(width,.95f,.2f),stone);
            // Scaled historical plan trace; not a surveyed current dimension.
            float bay=3.7368f,centreWidth=bay*5;
            Box("Pale five-bay facade",new Vector3(0,(top+entry)/2,-.1f),new Vector3(centreWidth,top-entry,.2f),stone);
            for(int i=0;i<6;i++)
            {
                float x=(i-2.5f)*bay;
                Box("Two-story pilaster",new Vector3(x,(top+entry)/2,-.32f),new Vector3(.7f,top-entry,.5f),stone);
                Box("Pilaster capital",new Vector3(x,top-.18f,-.37f),new Vector3(.98f,.25f,.6f),stone);
            }
            for(int i=-2;i<=2;i++)
            {
                Window(i*bay,8.6f,1.7f,2.6f);
                if(i!=0)Window(i*bay,4.25f,1.8f,3.1f);
            }
            foreach(int side in new[]{-1,1}){Window(side*15f,8.6f,2.0f,2.6f);Window(side*15f,4.25f,2f,3.1f);}
            Box("Facade entablature",new Vector3(0,top+.18f,-.16f),new Vector3(width+.5f,.55f,.7f),stone);
            Box("Cornice projecting lip",new Vector3(0,top+.53f,-.18f),new Vector3(width+.8f,.16f,.95f),stone);
            Box("Terrace deck",new Vector3(0,entry-.12f,-1.6f),new Vector3(centreWidth+.6f,.24f,3.2f),stone,true);
            for(int i=0;i<14;i++)
            {
                float h=(i+1)*entry/14;
                Box("Provisional entrance tread",new Vector3(0,h/2,-3.2f-(13-i+.5f)*.32f),new Vector3(5.3f,h,.33f),stone,true);
            }
            foreach(int side in new[]{-1,1})
            {
                // Stair sidewalls follow the step profile; coping slopes above them.
                for(int i=0;i<14;i++)
                {
                    float h=(i+1)*entry/14+.6f;
                    Box("Brick stair cheek",new Vector3(side*2.92f,h/2,-3.2f-(13-i+.5f)*.32f),new Vector3(.5f,h,.33f),brick,true);
                }
                var coping=Box("Pale stair coping",new Vector3(side*2.92f,entry/2+.65f,-3.2f-7*.32f),new Vector3(.65f,.16f,Mathf.Sqrt(entry*entry+4.48f*4.48f)),stone);
                coping.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(entry,4.48f)*Mathf.Rad2Deg,0,0);
                Box("Terrace brick parapet",new Vector3(side*6.12f,entry+.38f,-3f),new Vector3(5.94f,.76f,.45f),brick);
                Box("Terrace pale coping",new Vector3(side*6.12f,entry+.79f,-3f),new Vector3(6.04f,.12f,.58f),stone);
                Box("Portico square post",new Vector3(side*2.2f,entry+1.9f,-1.8f),new Vector3(.42f,3.8f,.42f),stone,true);
                Box("Portico post plinth",new Vector3(side*2.2f,entry+.14f,-1.8f),new Vector3(.6f,.28f,.6f),stone,true);
                Box("Portico post capital",new Vector3(side*2.2f,entry+3.73f,-1.8f),new Vector3(.7f,.22f,.7f),stone);
                TerraceRailing(side,entry);
                EntranceLantern(side*3.1f,entry+.82f,-3f);
            }
            Box("Portico entablature",new Vector3(0,entry+3.95f,-1.05f),new Vector3(5.3f,.4f,2.15f),stone);
            var pediment=new GameObject("Triangular portico pediment");pediment.transform.SetParent(root,false);
            float y=entry+4.15f,z=-2.18f,half=2.7f,rise=1.3f;
            var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-half,y,z),new Vector3(0,y+rise,z),new Vector3(half,y,z)};mesh.triangles=new[]{0,1,2};mesh.RecalculateNormals();pediment.AddComponent<MeshFilter>().sharedMesh=mesh;pediment.AddComponent<MeshRenderer>().sharedMaterial=stone;
            foreach(int side in new[]{-1,1})
            {
                var rake=Box("Portico pediment rake",new Vector3(side*half/2,y+rise/2,z-.05f),new Vector3(Mathf.Sqrt(half*half+rise*rise),.17f,.3f),stone);
                rake.transform.localRotation=Quaternion.Euler(0,0,-side*Mathf.Atan2(rise,half)*Mathf.Rad2Deg);
            }
            // Photograph shows a glazed door assembly and transom, not a sash window.
            EntranceDoor(entry);
            for(int i=-4;i<=4;i++)
            {
                float targetX=i*.47f;
                float targetY=y+rise*(1-Mathf.Abs(targetX)/half)-.17f;
                if(targetY>y+.12f)Rail("Pediment radial relief",new Vector3(0,y+.1f,z-.17f),new Vector3(targetX,targetY,z-.17f),.018f,stone);
            }
            var label=new GameObject("Library inscription");label.transform.SetParent(root,false);label.transform.localPosition=new Vector3(0,top+.15f,-.54f);var text=label.AddComponent<TextMesh>();text.text="STOCKWELL MEMORIAL LIBRARY";text.fontSize=64;text.characterSize=.14f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.42f,.39f,.31f);
            Physics.SyncTransforms();
            game.player.Teleport(root.TransformPoint(new Vector3(0,.05f,-8.2f)));
            for(int i=0;i<190;i++){game.player.body.Move(new Vector3(0,-.025f,.035f));if(i%8==0)yield return null;}
            if(Mathf.Abs(game.player.transform.position.y-(100+entry))>.2f){Debug.LogError("STOCKWELL_STUDY_FAILED entrance stair");Application.Quit(1);yield break;}
            var camera=new GameObject("Stockwell study camera").AddComponent<Camera>();camera.CopyFrom(game.player.eyes);camera.enabled=false;camera.fieldOfView=52;camera.aspect=1.6f;camera.transform.position=root.TransformPoint(new Vector3(24,9,-36));camera.transform.LookAt(root.TransformPoint(new Vector3(0,6,0)));
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional){light.transform.rotation=Quaternion.Euler(45,-25,0);light.intensity=1.3f;}
            var rt=new RenderTexture(1600,1000,24){antiAliasing=4};camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
            string output=Environment.GetEnvironmentVariable("STOCKWELL_OUTPUT");Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,"Stockwell-facade-study.png"),pixels.EncodeToPNG());File.WriteAllText(Path.Combine(output,"result.json"),"{\"entranceStairTraversed\":true,\"liveInstalled\":false,\"historicalWidthMetres\":36.576,\"otherDimensionsProvisional\":true,\"exactReplica\":false}");Debug.Log("STOCKWELL_FACADE_STUDY_OK");Application.Quit(0);
        }
        void BuildHistoricalShell(float width,float top)
        {
            // Plan-scaled depth; hipped roof form visible on Hanley printed p84.
            // Section p85 gives an approximately 0.25 rise/span ratio, not a surveyed pitch.
            const float depth=19.5754f;
            float eave=top+.58f,rise=depth*.25f,overhang=.4f;
            foreach(int side in new[]{-1,1})
            {
                Box("Historical side wall mass",new Vector3(side*(width/2-.3f),top/2,depth/2),new Vector3(.6f,top,depth),brick,true);
                Box("Side foundation band",new Vector3(side*(width/2+.04f),.5f,depth/2),new Vector3(.15f,.95f,depth),stone);
                Box("Side entablature",new Vector3(side*width/2,top+.18f,depth/2),new Vector3(.7f,.55f,depth+.4f),stone);
                Box("Side cornice",new Vector3(side*width/2,top+.53f,depth/2),new Vector3(.95f,.16f,depth+.8f),stone);
            }
            Box("Historical rear wall mass",new Vector3(0,top/2,depth-.3f),new Vector3(width,top,.6f),brick,true);
            Box("Rear entablature",new Vector3(0,top+.18f,depth),new Vector3(width+.5f,.55f,.7f),stone);
            Box("Rear cornice",new Vector3(0,top+.53f,depth),new Vector3(width+.8f,.16f,.95f),stone);
            var a=new Vector3(-width/2-overhang,eave,-overhang);
            var b=new Vector3(width/2+overhang,eave,-overhang);
            var c=new Vector3(width/2+overhang,eave,depth+overhang);
            var d=new Vector3(-width/2-overhang,eave,depth+overhang);
            var left=new Vector3(-width/2+depth/2,eave+rise,depth/2);
            var right=new Vector3(width/2-depth/2,eave+rise,depth/2);
            var vertices=new[]{a,left,right,a,right,b,b,right,c,c,right,left,c,left,d,d,left,a};
            var triangles=new int[vertices.Length];var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i+=3)
            {
                if(Vector3.Cross(vertices[i+1]-vertices[i],vertices[i+2]-vertices[i]).y<0)
                {var swap=vertices[i+1];vertices[i+1]=vertices[i+2];vertices[i+2]=swap;}
                for(int j=i;j<i+3;j++){triangles[j]=j;uv[j]=new Vector2(vertices[j].x,vertices[j].z)/2;}
            }
            var mesh=new Mesh{name="Stockwell provisional historical hip roof"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var roof=new GameObject("Historical hipped roof study");roof.transform.SetParent(root,false);roof.AddComponent<MeshFilter>().sharedMesh=mesh;roof.AddComponent<MeshRenderer>().sharedMaterial=CraftModel.Surface("Stockwell provisional roof finish",new Color(.27f,.28f,.27f));
            roof.AddComponent<MeshCollider>().sharedMesh=mesh;
            // Visible rear and side openings are intentionally unresolved, not asserted absent.
        }
        void Rail(string name,Vector3 start,Vector3 end,float thickness,Material material)
        {
            var beam=Box(name,(start+end)/2,new Vector3(thickness,thickness,Vector3.Distance(start,end)),material);
            beam.transform.localRotation=Quaternion.LookRotation(end-start,Vector3.up);
        }
        void TerraceRailing(int side,float entry)
        {
            // Visible side railing motif; bay count and dimensions remain provisional.
            float start=9.1f,end=17.6f,z=-3f;
            for(int i=0;i<=4;i++)
            {
                float x=side*Mathf.Lerp(start,end,i/4f);
                Box("Terrace iron railing post",new Vector3(x,entry+.49f,z),new Vector3(.065f,.98f,.065f),metal);
                if(i==4)continue;
                float next=side*Mathf.Lerp(start,end,(i+1)/4f);
                Rail("Terrace cross brace",new Vector3(x,entry+.12f,z),new Vector3(next,entry+.85f,z),.035f,metal);
                Rail("Terrace cross brace",new Vector3(x,entry+.85f,z),new Vector3(next,entry+.12f,z),.035f,metal);
            }
            foreach(float h in new[]{.12f,.9f})Rail("Terrace horizontal rail",new Vector3(side*start,entry+h,z),new Vector3(side*end,entry+h,z),.065f,metal);
            Box("Side terrace deck",new Vector3(side*13.35f,entry-.12f,-1.6f),new Vector3(8.6f,.24f,3.2f),stone,true);
        }
        void EntranceLantern(float x,float baseY,float z)
        {
            Box("Entrance lantern base",new Vector3(x,baseY+.13f,z),new Vector3(.28f,.26f,.28f),metal);
            KeeperAvatar.Part(root,"Entrance lantern stem",PrimitiveType.Cylinder,new Vector3(x,baseY+1.3f,z),new Vector3(.075f,1.2f,.075f),metal);
            float lampY=baseY+2.6f;
            Box("Entrance lantern glazing",new Vector3(x,lampY,z),new Vector3(.23f,.42f,.23f),glass);
            foreach(int a in new[]{-1,1})foreach(int b in new[]{-1,1})Box("Lantern corner frame",new Vector3(x+a*.13f,lampY,z+b*.13f),new Vector3(.03f,.48f,.03f),metal);
            foreach(float h in new[]{-.25f,.25f})Box("Lantern cap",new Vector3(x,lampY+h,z),new Vector3(.37f,.055f,.37f),metal);
            KeeperAvatar.Part(root,"Lantern finial",PrimitiveType.Sphere,new Vector3(x,lampY+.37f,z),new Vector3(.12f,.19f,.12f),metal);
        }
        void EntranceDoor(float entry)
        {
            const float w=2.6f,h=3.25f,z=-.39f;
            Box("Doorway brick reveal",new Vector3(0,entry+h/2,-.23f),new Vector3(3.5f,h+.15f,.16f),brick);
            Box("Entrance glass assembly",new Vector3(0,entry+h/2,z),new Vector3(w,h,.08f),glass);
            foreach(float x in new[]{-w/2,-.68f,.68f,w/2})Box("Entrance vertical frame",new Vector3(x,entry+h/2,z-.07f),new Vector3(.07f,h,.08f),stone);
            foreach(float y in new[]{0f,2.55f,h})Box("Entrance horizontal frame",new Vector3(0,entry+y,z-.07f),new Vector3(w,.07f,.08f),stone);
            Box("Door meeting stile",new Vector3(0,entry+1.27f,z-.07f),new Vector3(.055f,2.55f,.08f),stone);
            foreach(int side in new[]{-1,1})
            {
                Box("Entrance door pull",new Vector3(side*.12f,entry+1.2f,z-.16f),new Vector3(.025f,.4f,.035f),metal);
                Box("Entrance kick plate",new Vector3(side*.34f,entry+.12f,z-.08f),new Vector3(.61f,.18f,.025f),metal);
            }
        }
        void Window(float x,float y,float w,float h)
        {
            Box("Pale opening surround",new Vector3(x,y,-.23f),new Vector3(w+.2f,h+.2f,.12f),stone);
            Box("Study window glazing",new Vector3(x,y,-.31f),new Vector3(w,h,.07f),glass);
            Box("Window vertical mullion",new Vector3(x,y,-.36f),new Vector3(.035f,h,.03f),stone);
            foreach(float f in new[]{-.25f,0,.25f})Box("Window horizontal sash",new Vector3(x,y+h*f,-.36f),new Vector3(w,.045f,.03f),stone);
        }
    }
}
