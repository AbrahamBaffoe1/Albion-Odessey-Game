using System;
namespace AlbionOdyssey
{
    // Local story progress, never a source of online currency or server rewards.
    [Serializable] public sealed class AdventureProgress
    {
        public int chapter, observed;
        public bool arrived;
        public static readonly string[] Titles = {"An invitation from Pip", "The stories we keep", "Under one sky", "The living classroom", "Bring the story home", "Every path becomes a story"};
        public static readonly string[] Objectives = {
            "Pip is collecting a story of Albion: its memories, its sky and its living landscape. Accept the invitation to begin.",
            "Find three golden memories in Legacy Hall. Aim at each and use your interact key. The journal preserves what you discover.",
            "Visit the observatory upstairs. Use the telescope and record the Moon, Saturn and Orion. This is an educational sky, not a live astronomical chart.",
            "Enter Whitehouse Nature Center and record all three habitats in the field journal. You may arrive after a forest run or visit directly.",
            "Return to Legacy Hall to share your discoveries with Pip and complete your field expedition.",
            "You brought together memories, the night sky and the living landscape. Pip places your expedition in the campus archive. Your story is complete; Albion remains open to explore."
        };
        public bool Valid()=>chapter>=0&&chapter<=5&&observed>=0&&observed<=7&&(chapter<3||observed==7)&&(chapter<4||arrived);
        public bool Observe(int target){if(chapter!=2||target<0||target>2)return false;int before=observed;observed|=1<<target;return before!=observed;}
        public bool CanAdvance(Keeper keeper,bool home)=>chapter==0||chapter==1&&OdysseyState.Count(keeper.memories)>=3||chapter==2&&observed==7||chapter==3&&arrived&&keeper.fieldJournal==7||chapter==4&&home;
        public bool Advance(Keeper keeper,bool home,Func<bool> save)
        {
            if(!CanAdvance(keeper,home))return false;
            int before=chapter;chapter++;
            try { if(save())return true; } catch { chapter=before;throw; }
            chapter=before;return false;
        }
        public string Reward=>chapter>=5?"Expedition complete · Albion Storykeeper title":chapter>=4?"Field naturalist journal":chapter>=3?"Three-object observing record":chapter>=2?"Memory collector chapter":"Pip's expedition invitation";
    }
}
