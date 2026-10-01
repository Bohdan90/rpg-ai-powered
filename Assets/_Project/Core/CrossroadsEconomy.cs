using System;

namespace RPG.Core
{
    public sealed partial class CrossroadsScenario
    {
        public const int RecruitCost=100;
        public string RecruitBlocker(Side side,UnitProfileId profile)
        {
            if(Realm!=null)return "06 requires an explicit army or Reserve recipient.";
            if(!Economy)return "Economy disabled in Part A fixture.";
            if(!CanAct(side))return "No active side authority.";
            bool mage=profile==UnitProfileId.FireMageTI||profile==UnitProfileId.IceMageTI||profile==UnitProfileId.HumanHealerTI;
            if(profile!=UnitProfileId.HumanWarriorTI&&profile!=UnitProfileId.HumanArcherTI&&!mage)return "Only authorized L1 profiles.";
            if(mage&&(Foundations==null||!Foundations.Combined||Foundations.City(side).MageTower<1||!Foundations.City(side).Functioning
                ||(Foundations.Realm(side).Preset==CombatPreset.Support)!=(profile==UnitProfileId.HumanHealerTI)))return "Compatible 05B realm Mage direction and functioning Tower I required.";
            var f=Force(side);
            if(f.PendingRecruit.HasValue)return "Paid recruit pending; no second order.";
            if(f.LastRecruitRefresh==Refresh)return "Maximum one recruit per Refresh.";
            return CompletionBlocker(side)??(f.Gold<(mage?150:RecruitCost)?"Insufficient own Gold.":null);
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
            var f=Force(side);f.Gold-=UnitProfile.Get(profile).IsCaster?150:RecruitCost;f.PendingRecruit=profile;
            f.PendingRecruitId="duel-"+side+"-recruit-"+f.NextRecruit++;f.LastRecruitRefresh=Refresh;
            Log(side+" pays "+(UnitProfile.Get(profile).IsCaster?150:RecruitCost)+" Gold; queued "+f.PendingRecruitId+" / "+profile+"; ends activation");
            return EndActivation(side);
        }
        private void CompleteRecruit(Side side)
        {
            var f=Force(side);if(!f.PendingRecruit.HasValue)return;
            var blocker=CompletionBlocker(side);
            if(blocker!=null){Log(side+" paid recruit remains pending: "+blocker);return;}
            var profile=UnitProfile.Get(f.PendingRecruit.Value);
            if(profile.IsCaster&&(Foundations==null||Foundations.City(side).MageTower<1||!Foundations.City(side).Functioning))return;
            f.Formation.AddRecruit(new PersistentCharacter(f.PendingRecruitId,profile));
            Log(side+" recruit joined: "+f.PendingRecruitId+" L1 / XP0; full baseline HP/Armor. Consumption starts next Refresh.");
            f.PendingRecruit=null;f.PendingRecruitId="";
        }
    }
}
