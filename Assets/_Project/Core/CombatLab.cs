using System.Collections.Generic;
namespace RPG.Core
{
    public enum CombatLabMatch { FireVsIce, SupportVsFire, MobileBlades }
    // Authored veteran fixtures, not campaign recruitment or prebattle respec.
    public static class CombatLab
    {
        public static IEnumerable<UnitState> Units(CombatLabMatch match,bool nearContact=true)
        {
            var west=match==CombatLabMatch.FireVsIce?new[]{UnitProfile.FireMageTII,UnitProfile.FireMageTI,UnitProfile.HumanWarriorTI}:
                match==CombatLabMatch.SupportVsFire?new[]{UnitProfile.HumanHealerTI,UnitProfile.HumanWarriorTI,UnitProfile.IceMageTI}:
                new[]{UnitProfile.ElfWarriorTII,UnitProfile.ElfWarriorTII,UnitProfile.HumanHealerTI};
            var east=match==CombatLabMatch.FireVsIce?new[]{UnitProfile.IceMageTII,UnitProfile.IceMageTI,UnitProfile.HumanWarriorTI}:
                match==CombatLabMatch.SupportVsFire?new[]{UnitProfile.FireMageTII,UnitProfile.FireMageTI,UnitProfile.HumanWarriorTI}:
                new[]{UnitProfile.HumanWarriorTI,UnitProfile.HumanWarriorTI,UnitProfile.IceMageTII};
            for(int i=0;i<3;i++) {
                yield return new UnitState(new UnitId(i+1),Side.West,west[i],new GridPosition(nearContact?9:3,6+i*2),Facing.East);
                yield return new UnitState(new UnitId(i+4),Side.East,east[i],new GridPosition(nearContact?12:19,6+i*2),Facing.West);
            }
        }
        public static Battlefield Board()=>new Battlefield(23,17);
    }
}
