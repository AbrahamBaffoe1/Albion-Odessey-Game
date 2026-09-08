using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class CampusPlace
    {
        public string id,name,category,shape; public Vector3 position; public float width,depth,height;
        public CampusPlace(string id,string name,string category,string shape,float x,float y,float w,float d,float h)
        {this.id=id;this.name=name;this.category=category;this.shape=shape;position=CampusCatalog.Point(x,y);width=w*1.5f;depth=d*1.5f;height=h;}
        public Vector3 Arrival=>position+new Vector3(0,.08f,-depth/2-5);
    }
    public static class CampusCatalog
    {
        public const string MapSource="https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf";
        // Approximate centers read from the official August 2025 campus map. Game meters, not surveyed coordinates.
        public static Vector3 Point(float x,float y)=>new Vector3((x-400)*1.5f,0,400+(250-y)*1.5f);
        public static readonly CampusPlace[] Places={
            new CampusPlace("1","Bonta Admission Center","Resources","house",504f,151f,10f,9f,7f),
            new CampusPlace("2","Cass Center","Resources","hall",486f,151f,12f,8f,6f),
            new CampusPlace("5","Astronomical Observatory","Academic","observatory",398f,174f,6f,7f,8f),
            new CampusPlace("6","Baldwin Hall","Campus life","hall",497f,204f,8f,17f,12f),
            new CampusPlace("7","Bobbitt Visual Arts Center","Academic","arts",455f,148f,13f,11f,7f),
            new CampusPlace("7a","Bobbitt Annex","Academic","hall",443f,142f,7f,5f,5f),
            new CampusPlace("9","Facilities Operations","Resources","hall",324f,227f,12f,7f,5f),
            new CampusPlace("10","Goodrich Chapel","Academic","chapel",393f,130f,17f,10f,13f),
            new CampusPlace("11","Grounds","Resources","hall",428f,274f,14f,5f,5f),
            new CampusPlace("12","Herrick Theatre","Academic","theatre",518f,358f,11f,17f,10f),
            new CampusPlace("14","Kellogg Center","Campus life","hall",445f,225f,10f,10f,12f),
            new CampusPlace("15","Ludington Center","Academic","hall",57f,150f,8f,12f,8f),
            new CampusPlace("16","Robinson Hall","Academic","hall",445f,203f,8f,10f,13f),
            new CampusPlace("17","Olin Hall","Academic","hall",375f,229f,20f,8f,10f),
            new CampusPlace("18n","Norris Center · Science Complex","Academic","science",503f,125f,29f,10f,10f),
            new CampusPlace("18k","Kresge Hall · Science Complex","Academic","science",492f,105f,10f,22f,12f),
            new CampusPlace("18p","Palenske Hall · Science Complex","Academic","science",512f,105f,10f,22f,12f),
            new CampusPlace("18u","Putnam Hall · Science Complex","Academic","science",503f,138f,29f,8f,9f),
            new CampusPlace("19","Seely-Berkey House","Campus life","house",323f,69f,8f,7f,7f),
            new CampusPlace("20","Stockwell Memorial Library","Academic","library",377f,180f,15f,9f,12f),
            new CampusPlace("21","Mudd Learning Center","Academic","library",346f,180f,22f,9f,11f),
            new CampusPlace("22","Umbrella House","Campus life","house",562f,151f,7f,6f,7f),
            new CampusPlace("23","Vulgamore Hall (North Hall)","Academic","hall",445f,180f,13f,8f,12f),
            new CampusPlace("24","Welton House","Campus life","house",544f,151f,7f,6f,7f),
            new CampusPlace("25","Whitehouse Nature Center","Nature","nature",739f,294f,11f,11f,6f),
            new CampusPlace("26","Ferguson Hall","Resources","hall",412f,231f,21f,8f,9f),
            new CampusPlace("40","Briton House Apartments","Residential","house",310f,275f,7f,6f,7f),
            new CampusPlace("41","Burns Street Apartments","Residential","hall",693f,206f,24f,6f,7f),
            new CampusPlace("42","Dean Hall","Residential","hall",235f,296f,10f,6f,10f),
            new CampusPlace("43","Fiske House","Residential","house",429f,150f,7f,6f,7f),
            new CampusPlace("44","Ingham Hall","Residential","house",427f,134f,7f,6f,9f),
            new CampusPlace("45","Karro Apartments","Residential","hall",572f,268f,12f,19f,8f),
            new CampusPlace("46","Mitchell Towers","Residential","tower",575f,203f,10f,19f,24f),
            new CampusPlace("47","Munger Hall / Apartments","Residential","hall",194f,65f,16f,10f,11f),
            new CampusPlace("48","Munger Annex","Residential","house",199f,50f,6f,5f,6f),
            new CampusPlace("49","Seaton Hall","Residential","hall",504f,178f,23f,7f,12f),
            new CampusPlace("50","Wesley Hall","Residential","hall",413f,47f,16f,20f,14f),
            new CampusPlace("51","Whitehouse Hall","Residential","hall",505f,229f,19f,7f,12f),
            new CampusPlace("52","416 Erie St. Apartments","Residential","house",288f,296f,8f,6f,7f),
            new CampusPlace("53","President’s House","Campus life","house",305f,69f,8f,7f,7f),
            new CampusPlace("54","507 Erie St. Apartments","Residential","house",325f,275f,7f,6f,7f),
            new CampusPlace("55","711 E. Michigan Ave. Annex","Residential","house",443f,69f,6f,5f,6f),
            new CampusPlace("56","Guest House · 810 E. Michigan Ave.","Residential","house",465f,86f,8f,6f,7f),
            new CampusPlace("60","Alpha Chi Omega Sorority","Greek life","house",579f,152f,7f,6f,7f),
            new CampusPlace("61","Alpha Tau Omega Fraternity","Greek life","house",497f,274f,7f,6f,7f),
            new CampusPlace("62","Alpha Xi Delta Sorority","Greek life","house",528f,175f,6f,5f,7f),
            new CampusPlace("63","Delta Gamma Sorority","Greek life","house",540f,175f,6f,5f,7f),
            new CampusPlace("64","Delta Sigma Phi Fraternity","Greek life","house",489f,258f,6f,6f,7f),
            new CampusPlace("65","Delta Tau Delta Fraternity","Greek life","house",532f,258f,6f,6f,7f),
            new CampusPlace("66","Kappa Alpha Theta Sorority","Greek life","house",540f,188f,6f,5f,7f),
            new CampusPlace("67","Kappa Delta Sorority","Greek life","house",528f,196f,6f,5f,7f),
            new CampusPlace("68","Sigma Nu Fraternity","Greek life","house",529f,274f,7f,6f,7f),
            new CampusPlace("69","Sigma Chi Fraternity","Greek life","house",483f,274f,6f,6f,7f),
            new CampusPlace("70","Tau Kappa Epsilon Fraternity","Greek life","house",509f,258f,7f,6f,7f),
            new CampusPlace("76a","Alumni Stadium · Davis Athletic Complex","Athletics","field",630f,349f,43f,39f,1f),
            new CampusPlace("76b","Joranko Field · Davis Athletic Complex","Athletics","baseball",684f,349f,34f,36f,1f),
            new CampusPlace("76c","Dempsey Field · Davis Athletic Complex","Athletics","baseball",682f,401f,30f,27f,1f),
            new CampusPlace("77","Dow Recreation and Wellness Center","Athletics","gym",550f,344f,45f,36f,13f),
            new CampusPlace("78","Held Equestrian Center","Athletics","stable",394f,383f,31f,21f,9f),
            new CampusPlace("79","Kresge Gymnasium","Athletics","gym",333f,203f,14f,14f,12f),
            new CampusPlace("80","Sprankle-Sprandel Stadium","Athletics","stadium",544f,400f,49f,28f,2f),
        };
    }
}
