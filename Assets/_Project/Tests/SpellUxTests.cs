using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class SpellUxTests
    {
        static readonly UnitId A=new UnitId(1),B=new UnitId(2);
        static BattleState State(UnitProfile profile,params GridPosition[] solids)
        {return BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,profile,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,11,11),BattleTestFixtures.Unit(3,UnitProfile.HumanWarriorTI,Side.West,solids.Contains(new GridPosition(9,8))?7:9,8)},2,new Battlefield(23,17,solids)).State,A);}
        [Test] public void FireArmorExplainsSelfBeforeRangeAndNeverSpendsOnAlly()
        {
            var s=State(UnitProfile.FireMageTI);string hash=BattleStateHash.Compute(s);var c=new CastCommand(A,SpellId.FireArmor,new GridPosition(9,8));var p=BattleResolver.PreviewSpell(s,c);
            Assert.That(p.Error,Is.EqualTo(CommandError.SelfOnly));Assert.That(p.Blockers.Contains(CommandError.OutOfRange),Is.False);Assert.That(BattleResolver.Apply(s,c).State,Is.SameAs(s));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            Assert.That(BattleResolver.SpellAimCells(s,A,SpellId.FireArmor),Is.EqualTo(new[]{new GridPosition(8,8)}));
        }
        [TestCase(1,0)][TestCase(1,1)][TestCase(0,1)][TestCase(-1,1)][TestCase(-1,0)][TestCase(-1,-1)][TestCase(0,-1)][TestCase(1,-1)]
        public void StreamEightRaysIncludeThirdCellButNotFourth(int dx,int dy)
        {
            var s=State(UnitProfile.FireMageTI);var third=new GridPosition(8+3*dx,8+3*dy);var fourth=new GridPosition(8+4*dx,8+4*dy);
            var c=new CastCommand(A,SpellId.FireStream,third,true);var p=BattleResolver.PreviewSpell(s,c);
            Assert.That(p.IsLegal,Is.True);Assert.That(p.Cells,Is.EqualTo(Enumerable.Range(1,3).Select(i=>new GridPosition(8+i*dx,8+i*dy))));
            Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireStream,fourth,true)),Is.EqualTo(CommandError.OutOfRange));
            Assert.That(BattleResolver.SpellAimCells(s,A,SpellId.FireStream),Does.Contain(third));Assert.That(p.Cells.Contains(fourth),Is.False);
        }
        [Test] public void StreamOffRayAndSealedCornerAreDistinctAndWallsTruncateExactFootprint()
        {
            var s=State(UnitProfile.FireMageTI);Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireStream,new GridPosition(10,9))),Is.EqualTo(CommandError.OutsideSpellLine));
            s=State(UnitProfile.FireMageTI,new GridPosition(10,8));var p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,new GridPosition(9,8),true));Assert.That(p.IsLegal,Is.True);Assert.That(p.Cells,Is.EqualTo(new[]{new GridPosition(9,8)}));Assert.That(p.BlockedCells,Does.Contain(new GridPosition(10,8)));
            s=State(UnitProfile.FireMageTI,new GridPosition(9,8),new GridPosition(8,9));p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,new GridPosition(11,11)));Assert.That(p.Error,Is.EqualTo(CommandError.BlockedLineOfSight));Assert.That(p.Cells,Is.Empty);
            s=State(UnitProfile.FireMageTI,new GridPosition(9,8));Assert.That(BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,new GridPosition(11,11))).IsLegal,Is.True,"Spell corner semantics must not be replaced by movement's stricter corner rule.");
        }
        [TestCase(14,8)][TestCase(11,11)]
        public void FireballEmptyOrOccupiedCenterUsesSameCoreFootprintAndCosts(int x,int y)
        {
            var s=State(UnitProfile.FireMageTII);var c=new CastCommand(A,SpellId.Fireball,new GridPosition(x,y));var p=BattleResolver.PreviewSpell(s,c);Assert.That(p.IsLegal,Is.True);Assert.That(p.Cells.Count,Is.EqualTo(9));
            var r=BattleResolver.Apply(s,c);Assert.That(r.IsApplied,Is.True);Assert.That(r.State.FindUnit(A).FireballUsed,Is.EqualTo(1));Assert.That(r.State.FindUnit(A).ActionAvailable,Is.False);
        }
        [Test] public void CenterRangeIsSeparateFromBlastAndFriendlyFireNeedsConsent()
        {
            var s=State(UnitProfile.FireMageTII);var hash=BattleStateHash.Compute(s);var c=new CastCommand(A,SpellId.Fireball,new GridPosition(9,8));var p=BattleResolver.PreviewSpell(s,c);
            Assert.That(p.Error,Is.EqualTo(CommandError.FriendlyFireNotConfirmed));Assert.That(p.Targets,Does.Contain(A));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.Fireball,new GridPosition(17,8))),Is.EqualTo(CommandError.OutOfRange));
            Assert.That(BattleResolver.SpellAimCells(s,A,SpellId.Fireball),Does.Contain(new GridPosition(16,8)));Assert.That(BattleResolver.SpellAimCells(s,A,SpellId.Fireball).Contains(new GridPosition(17,8)),Is.False);
        }
        [Test] public void SilenceBlocksSpellsButNotStaffMovementOrDefendAndReplays()
        {
            var s=State(UnitProfile.IceMageTII);s.FindUnit(A).IsSilenced=true;s.FindUnit(B).Position=new GridPosition(8,9);var hash=BattleStateHash.Compute(s);
            foreach(var spell in SpellRules.Kit(s.FindUnit(A).Profile))Assert.That(BattleResolver.Validate(s,new CastCommand(A,spell,new GridPosition(8,9))),Is.EqualTo(CommandError.Silenced));
            Assert.That(BattleResolver.Validate(s,new BasicAttackCommand(A,B)),Is.EqualTo(CommandError.None));Assert.That(BattleResolver.Validate(s,new DefendCommand(A)),Is.EqualTo(CommandError.None));Assert.That(BattleResolver.Validate(s,new MoveCommand(A,new[]{new GridPosition(7,8)})),Is.EqualTo(CommandError.None));
            var restored=ReplaySnapshot.Capture(s).Restore();Assert.That(restored.FindUnit(A).IsSilenced,Is.True);Assert.That(BattleStateHash.Compute(restored),Is.EqualTo(hash));
            var j=new BattleJournal(s,"Silence fixture","test");j.Apply(new CastCommand(A,SpellId.IceShard,new GridPosition(8,9)),"Player");j.Apply(new BasicAttackCommand(A,B),"Player");Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
        }
        [TestCase(SpellId.IceShard,8,false)][TestCase(SpellId.Freeze,6,false)][TestCase(SpellId.IceShield,4,true)][TestCase(SpellId.CloseHeal,1,true)]
        public void DirectedSpellEnvelopeMatchesExactRangeAndRecipient(SpellId spell,int range,bool friendly)
        {
            var profile=spell==SpellId.CloseHeal?UnitProfile.HumanHealerTI:UnitProfile.IceMageTII;
            var s=State(profile);var target=s.FindUnit(B);target.Position=new GridPosition(8+range,8);
            if(friendly){target=s.FindUnit(new UnitId(3));target.Position=new GridPosition(8+range,8);s.FindUnit(B).Position=new GridPosition(18,14);target.Hp=10;}
            var cell=target.Position;Assert.That(BattleResolver.Validate(s,new CastCommand(A,spell,cell)),Is.EqualTo(CommandError.None));
            Assert.That(BattleResolver.SpellAimCells(s,A,spell),Does.Contain(cell));
            Assert.That(BattleResolver.Validate(s,new CastCommand(A,spell,new GridPosition(9+range,8))),Is.EqualTo(CommandError.OutOfRange));
            Assert.That(BattleResolver.SpellAimCells(s,A,spell).Contains(new GridPosition(9+range,8)),Is.False);
        }
        [Test] public void AllCurrentBlockersAreAvailableWithoutMutationAndExhaustionDoesNotBlockPrimary()
        {
            var s=State(UnitProfile.FireMageTII);var a=s.FindUnit(A);a.IsSilenced=true;a.ActionAvailable=false;a.ExhaustedActivations=1;a.FireballUsed=2;string hash=BattleStateHash.Compute(s);
            var p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.Fireball,new GridPosition(20,8)));Assert.That(p.Blockers,Is.SupersetOf(new[]{CommandError.NoAction,CommandError.Silenced,CommandError.Exhausted,CommandError.SourceBudgetSpent,CommandError.OutOfRange}));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            a.IsSilenced=false;a.ActionAvailable=true;Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireStream,new GridPosition(11,11))),Is.EqualTo(CommandError.None));
        }
    }
}
