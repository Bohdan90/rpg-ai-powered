using System;
namespace RPG.Core
{
    // Owner 34 accounting-only rows; these do not create excluded gameplay/building menus.
    public enum DeepCapability { RecruitmentWing, Research, Training, Healing, ForgeMagic, CulturalSiege, Walls, SpecialSite }
    public static class DeepAccounting
    {
        public static int Capacity(int development)=>development==1||development==2?0:development==3?4:development==4?6:throw new ArgumentOutOfRangeException(nameof(development));
        public static int Load(DeepCapability capability,int tier)
        {
            if(tier<0||tier>4)throw new ArgumentOutOfRangeException(nameof(tier));if(tier==0)return 0;
            switch(capability){case DeepCapability.RecruitmentWing:return tier<3?0:tier-2;case DeepCapability.Research:return tier==1?0:2;
                case DeepCapability.Training:case DeepCapability.ForgeMagic:case DeepCapability.CulturalSiege:return 1;
                case DeepCapability.Healing:return tier==1?0:tier==2?1:2;case DeepCapability.Walls:case DeepCapability.SpecialSite:return 0;default:throw new ArgumentOutOfRangeException(nameof(capability));}
        }
        public static int Increment(int physical,int target)=>Math.Max(0,target-physical);
        public static bool CanReserve(int development,int physical,int reserved,int increment)=>physical>=0&&reserved>=0&&increment>=0&&physical+reserved+increment<=Capacity(development);
    }
}
