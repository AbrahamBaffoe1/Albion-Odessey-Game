using System;
using UnityEngine;

namespace AlbionOdyssey
{
    /// <summary>
    /// Fictional student identities used by the offline campus population.
    /// They are authored as ordinary people with different names, pronouns,
    /// gender identities, skin tones, hair and clothing; no identity is a
    /// gameplay stat or a prediction about a real student.
    /// </summary>
    public sealed class CampusStudentProfile
    {
        public readonly string Name, GenderIdentity, Pronouns;
        public readonly int Skin, Coat, Hair;
        public readonly bool Backpack;

        public CampusStudentProfile(string name,string genderIdentity,string pronouns,int skin,int coat,int hair,bool backpack)
        {Name=name;GenderIdentity=genderIdentity;Pronouns=pronouns;Skin=skin;Coat=coat;Hair=hair;Backpack=backpack;}

        public bool IsTrans=>GenderIdentity.IndexOf("trans",StringComparison.OrdinalIgnoreCase)>=0;
    }

    public static class CampusStudentProfiles
    {
        static readonly CampusStudentProfile[] profiles={
            new CampusStudentProfile("Amina", "woman", "she/her", 1, 0, 0, true),
            new CampusStudentProfile("Kwesi", "man", "he/him", 0, 1, 1, true),
            new CampusStudentProfile("Jordan", "non-binary", "they/them", 3, 3, 2, false),
            new CampusStudentProfile("Nia", "trans woman", "she/her", 2, 4, 0, true),
            new CampusStudentProfile("Eli", "trans man", "he/him", 1, 2, 1, true),
            new CampusStudentProfile("Samira", "woman", "she/they", 4, 3, 0, false),
            new CampusStudentProfile("Mateo", "man", "he/they", 2, 0, 1, true),
            new CampusStudentProfile("Alex", "genderfluid", "they/she", 3, 2, 2, false),
            new CampusStudentProfile("Priya", "woman", "she/her", 4, 1, 1, true),
            new CampusStudentProfile("Darius", "man", "he/him", 0, 4, 0, false),
            new CampusStudentProfile("Taylor", "agender", "they/them", 2, 3, 2, true),
            new CampusStudentProfile("Ren", "trans non-binary", "they/them", 3, 0, 1, false),
            new CampusStudentProfile("Ama", "woman", "she/her", 1, 2, 0, true),
            new CampusStudentProfile("Noah", "man", "he/him", 4, 1, 2, true),
            new CampusStudentProfile("Morgan", "trans woman", "she/they", 2, 4, 1, false),
            new CampusStudentProfile("Chris", "trans man", "he/they", 0, 2, 0, true)
        };

        public static int Count=>profiles.Length;
        public static CampusStudentProfile Get(int index)=>profiles[Mathf.Abs(index)%profiles.Length];
    }

    /// <summary>
    /// Metadata attached to every visible simulated student. The identity is
    /// available to future dialogue, accessibility and multiplayer systems,
    /// while the offline chapter never exposes private data automatically.
    /// </summary>
    public sealed class CampusStudentIdentity : MonoBehaviour
    {
        public string StudentName{get;private set;}
        public string GenderIdentity{get;private set;}
        public string Pronouns{get;private set;}
        public bool IsTrans{get;private set;}

        public void Apply(CampusStudentProfile profile,string displayName=null)
        {
            StudentName=displayName??profile.Name;GenderIdentity=profile.GenderIdentity;Pronouns=profile.Pronouns;IsTrans=profile.IsTrans;
        }
    }
}
