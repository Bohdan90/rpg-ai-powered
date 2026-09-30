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
                yield return new UnitState(new UnitId(i+1),Side.West,west[i],new GridPosition(nearContact?9:3,6+i*2),Facing.East,hp:match==CombatLabMatch.SupportVsFire&&i==1?26:(int?)null);
                yield return new UnitState(new UnitId(i+4),Side.East,east[i],new GridPosition(nearContact?12:19,6+i*2),Facing.West);
            }
        }
        public static Battlefield Board(CombatLabMatch match=CombatLabMatch.FireVsIce)=>new Battlefield(23,17,
            match==CombatLabMatch.FireVsIce?System.Array.Empty<GridPosition>():match==CombatLabMatch.SupportVsFire?
            new[]{new GridPosition(10,7),new GridPosition(11,7),new GridPosition(10,11)}:
            new[]{new GridPosition(11,7),new GridPosition(11,8),new GridPosition(11,9)});
    }
}
