using System.Collections.Generic;
using System.Linq;
namespace RPG.Core
{
    // Authored spatial test only: same 11x9 fortress, moat and crossings. No siege mechanics.
    public static class DirectionalSiegeFixture
    {
        public static RetreatEdge[] Approaches(SizeExperimentMap map)
        {
            switch(map) {
                case SizeExperimentMap.Siege_41x39_East_9v9:return new[]{RetreatEdge.East};
                case SizeExperimentMap.Siege_41x39_North_9v9:return new[]{RetreatEdge.North};
                case SizeExperimentMap.Siege_41x39_South_9v9:return new[]{RetreatEdge.South};
                case SizeExperimentMap.Siege_41x39_WestEast_18v9:return new[]{RetreatEdge.West,RetreatEdge.East};
                case SizeExperimentMap.Siege_41x39_NorthSouth_18v9:return new[]{RetreatEdge.North,RetreatEdge.South};
                default:return new[]{RetreatEdge.West};
            }
        }
        private static GridPosition Place(RetreatEdge edge,int depth,int lateral)
        {
            switch(edge) {
                case RetreatEdge.West:return new GridPosition(depth,19+lateral);
                case RetreatEdge.East:return new GridPosition(40-depth,19+lateral);
                case RetreatEdge.North:return new GridPosition(20+lateral,38-depth);
                default:return new GridPosition(20+lateral,depth);
            }
        }
        public static IEnumerable<GridPosition> DeploymentCells(RetreatEdge edge)
        { for(int depth=1;depth<=3;depth++)for(int lateral=-4;lateral<=4;lateral++)yield return Place(edge,depth,lateral); }
        // Conservative measurement includes every perimeter center, including openings.
        // These are reference points, NOT standable platforms introduced into combat.
        public static IEnumerable<GridPosition> WallReferenceCells()
        { for(int x=15;x<=25;x++)for(int y=15;y<=23;y++)if(x==15||x==25||y==15||y==23)yield return new GridPosition(x,y); }
        public static long MinimumWallSeparation(RetreatEdge edge)=>DeploymentCells(edge).Min(p=>WallReferenceCells().Min(w=>p.DistanceTo(w)));
        public static UnitState[] Units(SizeExperimentMap map)
        {
            var baseline=SizeExperimentFixture.Units(SizeExperimentMap.Siege_35x27_Full_9v9);
            var units=new List<UnitState>();
            foreach(var u in baseline.Where(u=>u.Side==Side.East))units.Add(new UnitState(u.Id,u.Side,u.Profile,new GridPosition(u.Position.X+3,u.Position.Y+6),u.Facing));
            var army=baseline.Where(u=>u.Side==Side.West).ToArray();int armyIndex=0;
            foreach(var edge in Approaches(map))
            {
                foreach(var u in army)
                {
                    int index=System.Array.IndexOf(army,u);
                    var facing=edge==RetreatEdge.West?Facing.East:edge==RetreatEdge.East?Facing.West:edge==RetreatEdge.North?Facing.South:Facing.North;
                    units.Add(new UnitState(armyIndex==0?u.Id:new UnitId(19+index),Side.West,u.Profile,Place(edge,u.Position.X,u.Position.Y-13),facing,ownRetreatEdge:edge));
                }
                armyIndex++;
            }
            return units.OrderBy(u=>u.Id).ToArray();
        }
    }
}
