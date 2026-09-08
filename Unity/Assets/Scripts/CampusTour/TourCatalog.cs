using System;
using System.Linq;
namespace AlbionOdyssey
{
    [Serializable] public sealed class TourMedia { public string label,kind,url; }
    [Serializable] public sealed class TourPlace
    {
        public string id,name,category,summary,source;
        public string[] campusIds;
        public TourMedia[] media;
    }
    [Serializable] public sealed class TourCatalog
    {
        public TourPlace[] places;
        public TourPlace ForCampus(string id)=>places.FirstOrDefault(p=>Array.IndexOf(p.campusIds,id)>=0);
        public TourPlace[] Search(string term)=>places.Where(p=>string.IsNullOrWhiteSpace(term)||(p.name+" "+p.category+" "+p.summary).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        public bool Valid()=>places!=null&&places.Length>0&&places.All(p=>!string.IsNullOrWhiteSpace(p.id)&&!string.IsNullOrWhiteSpace(p.summary)&&p.campusIds!=null&&p.media!=null&&SafeSource(p.source)&&p.media.All(m=>SafeMedia(m.url)&&(m.kind=="photo"||m.kind=="panorama"||m.kind=="video")))&&places.Select(p=>p.id).Distinct().Count()==places.Length&&places.SelectMany(p=>p.campusIds).Distinct().Count()==places.Sum(p=>p.campusIds.Length);
        public static bool SafeMedia(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https"&&(u.Host=="content.studentbridge.com"||u.Host=="cdn.media.studentbridge.com");
        public static bool SafeSource(string url)=>Uri.TryCreate(url,UriKind.Absolute,out var u)&&u.Scheme=="https"&&(u.Host=="albion.college-tour.com"||u.Host=="www.albion.edu");
    }
}
