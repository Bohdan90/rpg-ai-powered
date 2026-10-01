using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;
using static RPG.Tests.GridTestFixtures;

namespace RPG.Tests
{
    public class RangedCornerTests
    {
        [TestCase(-2,1,2,1,false)] // Horizontal boundary.
        [TestCase(-2,-1,2,-1,false)]
        [TestCase(1,-2,1,2,false)] // Vertical boundary.
        [TestCase(-1,-2,-1,2,false)]
        [TestCase(-2,0,0,2,false)] // Single corner touch.
        [TestCase(-2,0,2,0,true)] // Interior crossing.
        [TestCase(-2,1,2,0,true)] // Leaving boundary and entering interior.
        [TestCase(-4,0,-2,0,false)] // Finite segment stops before wall.
        public void OpenCellClippingDistinguishesBoundaryFromInterior(int sx,int sy,int tx,int ty,bool crosses)
        {
            Assert.That(LineOfSight.CrossesCellInterior(sx,sy,tx,ty,P(0,0)),Is.EqualTo(crosses));
            Assert.That(LineOfSight.CrossesCellInterior(tx,ty,sx,sy,P(0,0)),Is.EqualTo(crosses));
        }

        [Test]
        public void DoubleWallPointTouchIsSealedAndInteriorWallAlsoBlocks()
        {
            var units = new[] { Unit(1,UnitProfile.HumanArcherTI,x:2,y:2), Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:4,y:4) };
            var state = BattleResolver.StartBattle(units,2,new Battlefield(new[] { P(3,2),P(2,3) })).State;
            Assert.That(BattleResolver.PreviewAttack(state,Attack()).IsLegal,Is.False);
            AssertRejected(state,Attack(),CommandError.BlockedLineOfSight);
            var blocked = BattleResolver.StartBattle(units,2,new Battlefield(new[] { P(3,2),P(2,3),P(3,3) })).State;
            AssertRejected(blocked,Attack(),CommandError.BlockedLineOfSight);
        }

        [TestCase(-1,-1)]
        [TestCase(-1,1)]
        [TestCase(1,-1)]
        [TestCase(1,1)]
        public void SealedVertexIsSymmetricInEveryDiagonal(int dx,int dy)
        {
            var source=P(5,5); var target=P(5+2*dx,5+2*dy);
            var units=new[] { Unit(1,UnitProfile.HumanArcherTI,x:5,y:5), Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:target.X,y:target.Y) };
            var state=BattleResolver.StartBattle(units,2,new Battlefield(new[] { P(5+dx,5),P(5,5+dy) })).State;
            Assert.That(LineOfSight.IsClear(state,source,target),Is.False);
            Assert.That(LineOfSight.IsClear(state,target,source),Is.False);
            var exposed=BattleResolver.StartBattle(units,2,new Battlefield(new[] { P(5+dx,5) })).State;
            Assert.That(LineOfSight.IsClear(exposed,source,target),Is.True);
            Assert.That(LineOfSight.IsClear(exposed,target,source),Is.True);
        }

        [Test]
        public void UnitCornerCoverRemainsInclusiveAndNonStackingAlongsideGrazedWall()
        {
            var state = BattleResolver.StartBattle(new[] { Unit(1,UnitProfile.HumanArcherTI,x:2,y:2),
                Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:5,y:5),
                Unit(3,UnitProfile.HumanWarriorTI,Side.East,x:5,y:4),
                Unit(4,UnitProfile.HumanWarriorTI,x:4,y:5)
            },2,new Battlefield(new[] { P(3,2) })).State;
            var preview = BattleResolver.PreviewAttack(state,Attack());
            Assert.That(preview.IsLegal,Is.True); Assert.That(preview.Cover,Is.EqualTo(CoverLevel.Light));
            Assert.That(preview.CoverAccuracyModifier,Is.EqualTo(-15));
        }

        [TestCase(9,7,true)] // User case: corner touch only.
        [TestCase(10,7,false)] // Same shot enters this cell's interior.
        [TestCase(8,7,true)] // Clear of the firing segment.
        public void UserShotPreviewAndExecutionAgreeWithSolidInterior(int wx, int wy, bool legal)
        {
            var state = ToActor(BattleResolver.StartBattle(new[] {
                Unit(1, UnitProfile.HumanArcherTI, x:12, y:5),
                Unit(2, UnitProfile.ElfWarriorTI, Side.East, x:9, y:8)
            }, 2, new Battlefield(19,13,new[] { P(wx,wy) })).State, Attacker);
            string before = Snapshot(state);
            Assert.That(LineOfSight.IsClear(state,P(12,5),P(9,8)),Is.EqualTo(legal));
            Assert.That(LineOfSight.IsClear(state,P(9,8),P(12,5)),Is.EqualTo(legal));
            var preview = BattleResolver.PreviewAttack(state, Attack());
            Assert.That(preview.IsLegal,Is.EqualTo(legal));
            Assert.That(Snapshot(state),Is.EqualTo(before));
            var result = BattleResolver.Apply(state,Attack());
            Assert.That(result.IsApplied,Is.EqualTo(legal));
            if(!legal) { Assert.That(result.Error,Is.EqualTo(CommandError.BlockedLineOfSight)); Assert.That(Snapshot(result.State),Is.EqualTo(before)); }
            else Assert.That(preview.Cover,Is.EqualTo(CoverLevel.None)); // No invented wall cover.
        }
    }
}
