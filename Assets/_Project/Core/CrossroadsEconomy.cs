using System;

namespace RPG.Core
{
    public sealed partial class CrossroadsScenario
    {
        public const int RecruitCost=100;
        public string RecruitBlocker(Side side,UnitProfileId profile)
        {
            if(!Economy)return "Economy disabled in Part A fixture.";
            if(!CanAct(side))return "No active side authority.";
            if(profile!=UnitProfileId.HumanWarriorTI&&profile!=UnitProfileId.HumanArcherTI)return "Only L1 Human Warrior / Archer.";
            var f=Force(side);
            if(f.PendingRecruit.HasValue)return "Paid recruit pending; no second order.";
            if(f.LastRecruitRefresh==Refresh)return "Maximum one recruit per Refresh.";
            return CompletionBlocker(side)??(f.Gold<RecruitCost?"Requires 100 own Gold.":null);
        }
        private string CompletionBlocker(Side side)
        {
            var f=Force(side);
            if(f.Formation.Commanderless||f.Formation.Commander==null)return "Commanderless: roster locked.";
            if(f.Node!=OwnKeep(side))return "Formation must be at own Keep.";
            if(!KeepAvailable(side))return "Own Keep occupied by enemy.";
            if(f.FreeCapacity<6)return "Requires 6 free Native Command Capacity.";
            return null;
        }
        public bool Recruit(Side side,UnitProfileId profile)
        {
            if(RecruitBlocker(side,profile)!=null)return false;
            var f=Force(side);f.Gold-=RecruitCost;f.PendingRecruit=profile;
            f.PendingRecruitId="duel-"+side+"-recruit-"+f.NextRecruit++;f.LastRecruitRefresh=Refresh;
            Log(side+" pays100 Gold; queued "+f.PendingRecruitId+" / "+profile+"; ends activation");
            return EndActivation(side);
        }
        private void CompleteRecruit(Side side)
        {
            var f=Force(side);if(!f.PendingRecruit.HasValue)return;
            var blocker=CompletionBlocker(side);
            if(blocker!=null){Log(side+" paid recruit remains pending: "+blocker);return;}
            var profile=f.PendingRecruit==UnitProfileId.HumanWarriorTI?UnitProfile.HumanWarriorTI:UnitProfile.HumanArcherTI;
            f.Formation.AddRecruit(new PersistentCharacter(f.PendingRecruitId,profile));
            Log(side+" recruit joined: "+f.PendingRecruitId+" L1 / XP0; full baseline HP/Armor. Consumption starts next Refresh.");
            f.PendingRecruit=null;f.PendingRecruitId="";
        }
    }
}
