using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class SiegeScaleV2Tests
    {
        private static GridPosition P(int x,int y)=>new GridPosition(x,y);
        private static GridPosition[] RelativeSolids(Battlefield b)=>b.SolidCells
            .Select(p=>P(p.X-b.Columns/2,p.Y-b.Rows/2)).ToArray();

        [Test]
        public void BothMapsHaveIdenticalFortressMoatCrossingsAndFormationTopology()
        {
            var a=SizeExperimentFixture.Board(SizeExperimentMap.Siege_31x25_Medium);
            var b=SizeExperimentFixture.Board(SizeExperimentMap.Siege_35x27_Large);
            CollectionAssert.AreEqual(RelativeSolids(a),RelativeSolids(b));
            var old=SizeExperimentFixture.Board(SizeExperimentMap.Siege_23x17_Tight);
            var fortress=RelativeSolids(a).Where(p=>Math.Abs(p.X)<=5&&Math.Abs(p.Y)<=4).ToArray();
            CollectionAssert.AreEqual(RelativeSolids(old),fortress);
            Assert.That(fortress.Length,Is.EqualTo(24));Assert.That(a.SolidCells.Count,Is.EqualTo(56));
            foreach(var map in new[]{SizeExperimentMap.Siege_31x25_Medium,SizeExperimentMap.Siege_35x27_Large})
            {
                var board=SizeExperimentFixture.Board(map);int cx=board.Columns/2,cy=board.Rows/2;
                for(int x=-6;x<=6;x++)for(int y=-5;y<=5;y++)
                    if(Math.Abs(x)==6||Math.Abs(y)==5)
                        Assert.That(board.IsSolid(P(cx+x,cy+y)),Is.EqualTo(Math.Abs(x)>1&&Math.Abs(y)>1));
                var units=SizeExperimentFixture.Units(map);
                Assert.That(units.Take(5).Select(u=>u.Position.X),Is.EqualTo(new[]{2,2,1,1,2}));
                Assert.That(units.Skip(5).Select(u=>u.Position.X-cx),Is.EqualTo(new[]{1,1,2,2,1}));
                Assert.That(units.Select(u=>u.Position.Y-cy),Is.EqualTo(new[]{0,-1,-2,2,1,0,-1,-2,2,1}));
            }
        }

        // Query and execute real per-activation paths; no boosted Movement or graph bypass.
        private static BattleState MoveTo(BattleState state,UnitId actor,GridPosition destination)
        {
            state=ToActor(state,actor);
            string before=Snapshot(state);
            var path=Pathfinder.FindPath(state,actor,destination);
            Assert.That(path.Found,Is.True,$"No route to {destination.X},{destination.Y}");
            CollectionAssert.AreEqual(path.Steps,Pathfinder.FindPath(state,actor,destination).Steps);
            Assert.That(Snapshot(state),Is.EqualTo(before));
            var move=BattleResolver.Apply(state,new MoveCommand(actor,path.Steps));
            Assert.That(move.IsApplied,Is.True);state=move.State;
            Assert.That(state.FindUnit(actor).Position,Is.EqualTo(destination));
            if(state.FindUnit(actor).IsActive&&!state.Outcome.IsEnded)
                state=BattleResolver.Apply(state,new EndActivationCommand(actor)).State;
            return state;
        }

        [TestCase(SizeExperimentMap.Siege_31x25_Medium)]
        [TestCase(SizeExperimentMap.Siege_35x27_Large)]
        public void ActualFixtureAttackerCanApproachAndEnterWestCrossing(SizeExperimentMap map)
        {
            var board=SizeExperimentFixture.Board(map);int cx=board.Columns/2,cy=board.Rows/2;
            var state=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,board).State;
            var actor=new UnitId(5);
            state=MoveTo(state,actor,P(8,cy));
            state=MoveTo(state,actor,P(cx-4,cy));
            Assert.That(state.FindUnit(actor).IsActive,Is.True);
            Assert.That(board.IsRetreatZone(Side.East,P(cx-6,cy)),Is.False);
        }

        [TestCase(SizeExperimentMap.Siege_31x25_Medium)]
        [TestCase(SizeExperimentMap.Siege_35x27_Large)]
        public void ActualDefenderCanLeaveInteriorAndPhysicallyEscapeOuterEdge(SizeExperimentMap map)
        {
            var board=SizeExperimentFixture.Board(map);int cx=board.Columns/2,cy=board.Rows/2;
            var state=BattleResolver.StartBattle(SizeExperimentFixture.Units(map),20260921,board).State;
            var actor=new UnitId(10);var hp=state.FindUnit(actor).Hp;var armor=state.FindUnit(actor).Armor;
            state=MoveTo(state,actor,P(cx,cy+3));
            state=MoveTo(state,actor,P(cx,cy+7));
            Assert.That(state.FindUnit(actor).IsActive,Is.True); // Crossings are not escape endpoints.
            state=MoveTo(state,actor,P(cx,board.Rows-1));
            Assert.That(state.FindUnit(actor).Status,Is.EqualTo(UnitStatus.Escaped));
            Assert.That(state.FindUnit(actor).Hp,Is.EqualTo(hp));Assert.That(state.FindUnit(actor).Armor,Is.EqualTo(armor));
            Assert.That(state.Outcome.IsEnded,Is.False);
        }

        [TestCase(SizeExperimentMap.Siege_31x25_Medium)]
        [TestCase(SizeExperimentMap.Siege_35x27_Large)]
        public void ExteriorRoutesExistOnBothFlanksAndAttackerCannotUseDefenderEdge(SizeExperimentMap map)
        {
            var board=SizeExperimentFixture.Board(map);int cx=board.Columns/2,cy=board.Rows/2;
            foreach(int sign in new[]{-1,1})
            {
                var state=BattleResolver.StartBattle(new[]{Unit(1,UnitProfile.ElfWarriorTI,x:cx-7,y:cy+sign*2),
                    Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:2,y:2)},2,board).State;
                state=MoveTo(state,Attacker,P(cx-7,cy+sign*6));
                state=MoveTo(state,Attacker,P(cx-1,cy+sign*6));
                state=MoveTo(state,Attacker,P(cx+5,cy+sign*6));
                state=MoveTo(state,Attacker,P(cx+7,cy+sign*2));
                Assert.That(state.FindUnit(Attacker).IsActive,Is.True);
            }
            var edgeState=BattleResolver.StartBattle(new[]{Unit(1,UnitProfile.ElfWarriorTI,x:cx,y:cy+7),
                Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:2,y:2)},2,board).State;
            edgeState=MoveTo(edgeState,Attacker,P(cx,board.Rows-1));
            Assert.That(edgeState.FindUnit(Attacker).Status,Is.EqualTo(UnitStatus.Active));
        }

        [TestCase(SizeExperimentMap.Siege_31x25_Medium)]
        [TestCase(SizeExperimentMap.Siege_35x27_Large)]
        public void StaticRingBlocksButCrossingsPreserveOrdinaryRangedAndOaRules(SizeExperimentMap map)
        {
            var board=SizeExperimentFixture.Board(map);int cx=board.Columns/2,cy=board.Rows/2;
            var state=BattleResolver.StartBattle(new[]{Unit(1,UnitProfile.HumanArcherTI,x:cx-7,y:cy),
                Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:cx-3,y:cy)},2,board).State;
            Assert.That(BattleResolver.PreviewAttack(state,Attack()).IsLegal,Is.True);
            Assert.That(LineOfSight.IsClear(state,P(cx-7,cy+2),P(cx-3,cy+2)),Is.False);
            Assert.That(MovementRules.ValidateStep(state,Attacker,P(cx-7,cy+2),P(cx-6,cy+2)),Is.EqualTo(CommandError.SolidCell));
            state=BattleResolver.StartBattle(new[]{Unit(1,UnitProfile.ElfWarriorTI,x:cx-6,y:cy),
                Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:cx-5,y:cy)},2,board).State;
            var exit=new MoveCommand(Attacker,new[]{P(cx-7,cy)});
            Assert.That(ZoneOfControl.Reactors(state,Attacker,P(cx-6,cy),P(cx-7,cy)).Count,Is.EqualTo(1));
            var result=BattleResolver.Apply(state,exit);
            Assert.That(result.IsApplied,Is.True);
            Assert.That(result.Events.Count(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.EqualTo(1));
        }
    }
}
