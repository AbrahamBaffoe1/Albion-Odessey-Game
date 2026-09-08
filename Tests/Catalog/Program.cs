using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AlbionOdyssey;
class Program
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){var c=JsonSerializer.Deserialize<TourCatalog>(File.ReadAllText("Unity/Assets/Resources/CampusTour/catalog.json"),new JsonSerializerOptions{IncludeFields=true});Check(c.Valid(),"Catalog validation");Check(c.places.Length==55,"55 official entries");Check(c.places.Sum(p=>p.media.Count(m=>m.kind=="photo"))==46,"46 photos");Check(c.places.Sum(p=>p.media.Count(m=>m.kind=="panorama"))==10,"10 panoramas");Check(c.places.Sum(p=>p.media.Count(m=>m.kind=="video"))==9,"9 videos");Check(c.ForCampus("68").name=="Sigma Nu Fraternity","Sigma Nu not Sigma Chi");Check(c.ForCampus("69").name=="Sigma Chi Fraternity","Sigma Chi mapping");Check(c.ForCampus("20")==c.ForCampus("21"),"Library group");Check(c.ForCampus("76a")==c.ForCampus("76c"),"Davis group");Check(c.ForCampus("50").id=="917","Wesley ID");Check(c.Search("WESLEY").Length>=2,"Case insensitive search");Check(c.Search("zzznomatch").Length==0,"No result search");Check(!TourCatalog.SafeMedia("file:///etc/passwd"),"Reject local links");Check(!TourCatalog.SafeMedia("https://content.studentbridge.com.evil.test/x"),"Exact host match");Check(!TourCatalog.SafeMedia("http://content.studentbridge.com/x"),"HTTPS required");c.places[1].campusIds=c.places[0].campusIds;Check(!c.Valid(),"Duplicate mapping rejected");Console.WriteLine("CATALOG_TESTS_OK: media coverage, stable mappings, search, URL validation and duplicate rejection");}
}
