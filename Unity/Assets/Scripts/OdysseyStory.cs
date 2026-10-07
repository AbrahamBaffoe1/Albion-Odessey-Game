using System;

namespace AlbionOdyssey
{
    // Original game fiction. These entries make no claims about Albion College history.
    public static class OdysseyStory
    {
        public static readonly string[] Floors = {
            "Welcome Atrium", "Founders Archive", "Maker Studio", "Botanical Commons",
            "Study Library", "Sky Gallery", "Star Observatory", "Legacy Council"
        };
        public static readonly string[] Titles = {
            "The Empty Blueprint", "A Place for Every Story", "Make, Break, Remake", "Roots and Routes",
            "The Unfinished Shelf", "A Window for Everyone", "The Same Night Sky", "A Seat at the Table",
            "The First Footpath", "A Garden Beyond the Gate", "The Lantern Promise", "A Campus of Your Own"
        };
        public static readonly string[] Entries = {
            "You find a blueprint with no walls drawn on it. The Keeper who left it behind wrote: begin with the people who will use this place. Your first campus begins with one small decision.",
            "The archive has an empty drawer for stories that have not happened yet. A campus remembers through the people who gather there. Leave room for someone else's story beside your own.",
            "A chipped model sits beside a perfect one. The makers kept both: one shows what worked, the other shows what they learned. Reclaiming your buildings returns every acorn so you can try again.",
            "The garden's roots cross the borders drawn on its plan. Build somewhere to pause, breathe and meet. A garden can be the first room of a campus even without walls.",
            "One shelf is deliberately unfinished. The library grows when people bring something new to it. Collect memories throughout Legacy Hall and turn your discoveries into places of your own.",
            "An empty frame faces a window. The gallery's note asks visitors to notice what changes when they move. Explore the same place from another floor, another doorway or another Keeper's perspective.",
            "The astronomy table holds worlds of very different sizes. From far away they share one sky. Your personal campus can look different while contributing to the same community Beacon.",
            "There is no special chair at the council table. The charter asks every Keeper to build something and offer something. Your contribution to the Beacon is remembered alongside your own campus.",
            "A path is drawn by walking it, the old Keeper wrote. You can explore every memory from your room. Being far from a place does not stop you imagining what it could become.",
            "Seeds wait by the gate. A small garden is enough to begin. Choose a plot, plant an idea, and give it room to grow.",
            "The lantern carries a promise: two acorns given today can become light for everyone tomorrow. The shared Beacon grows from the contributions of all four local Keepers.",
            "The last drawing has your name left blank. This campus is yours to arrange. Keep the places you love, reclaim the ones you want to change, and begin another version whenever you are ready."
        };
        public static readonly string[] Chapters = {
            "Find your first memory", "Build three places on your campus", "Explore all eight floors",
            "Build one of every structure", "Contribute six acorns", "Complete the shared Beacon"
        };
        public static readonly string[] Hints = {
            "Enter Legacy Hall. Aim at a golden memory and press your interact key.",
            "Press F2. Choose structures with 1–4, then click empty plots.",
            "Follow the STAIRS signs through the right-hand doorway in the middle of each floor.",
            "Your campus needs a Garden, Library, Observatory and Hall at the same time.",
            "Press C three times when you have acorns to share. Each contribution costs two.",
            "Bring the Beacon to 24 acorns. Tab switches local Keepers, each with their own discoveries."
        };
        public static void Refresh(OdysseyState state)
        {
            foreach(var keeper in state.keepers)
            {
                int occupied=0,types=0;
                foreach(int kind in keeper.plots)if(kind!=0){occupied++;types|=1<<kind;}
                if(keeper.memories!=0)keeper.milestones|=1;
                if(occupied>=3)keeper.milestones|=2;
                if((keeper.memories&255)==255)keeper.milestones|=4;
                if((types&30)==30)keeper.milestones|=8;
                if(keeper.contribution>=6)keeper.milestones|=16;
                if(state.beacon==24)keeper.milestones|=32;
            }
        }
        public static int Next(Keeper keeper)
        {
            for(int i=0;i<Chapters.Length;i++)if((keeper.milestones&(1<<i))==0)return i;
            return Chapters.Length;
        }
        public static string Objective(Keeper keeper)
        {
            int next=Next(keeper);
            return next<Chapters.Length?Chapters[next]:"Keeper's Charter complete — keep creating";
        }
    }
}
