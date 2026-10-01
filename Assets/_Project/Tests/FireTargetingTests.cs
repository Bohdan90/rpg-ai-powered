using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class FireTargetingTests
    {
        static readonly UnitId A=new UnitId(1), Ally=new UnitId(3);
        static GridPosition P(int x,int y)=>new GridPosition(x,y);
        static BattleState State(UnitProfile recipient=null,int version=2,params GridPosition[] solids)
            =>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.FireMageTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,14,12),BattleTestFixtures.Unit(3,recipient??UnitProfile.HumanWarriorTI,x:7,y:8),BattleTestFixtures.Unit(4,UnitProfile.IceMageTII,x:7,y:7)},2,new Battlefield(23,17,solids),fireRulesVersion:version).State,A);
        static BattleState Cast(BattleState s,SpellId spell,GridPosition cell,bool ff=false){var r=BattleResolver.Apply(s,new CastCommand(s.CurrentUnitId.Value,spell,cell,ff));Assert.That(r.IsApplied,Is.True,r.Error.ToString());return r.State;}
        static BattleState Next(BattleState s,UnitId id)=>BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(s.CurrentUnitId.Value)).State,id);
        [Test] public void EveryOffsetRotationAndMirrorUsesExactThinThreeStepRaster()
        {
            var s=State();s.FindUnit(Ally).Position=P(2,2);s.FindUnit(new UnitId(4)).Position=P(3,2);
            for(int dx=-3;dx<=3;dx++)for(int dy=-3;dy<=3;dy++)if(dx!=0||dy!=0){
                int m=Math.Max(Math.Abs(dx),Math.Abs(dy));var aim=P(8+dx,8+dy);var p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,aim));
                var expected=Enumerable.Range(1,3).Select(k=>P(8+(int)Math.Round((decimal)k*dx/m,MidpointRounding.AwayFromZero),8+(int)Math.Round((decimal)k*dy/m,MidpointRounding.AwayFromZero))).ToArray();
                Assert.That(p.IsLegal,Is.True,aim.ToString());Assert.That(p.Cells,Is.EqualTo(expected),aim.ToString());Assert.That(p.Cells.Distinct().Count(),Is.EqualTo(3));Assert.That(p.Cells,Does.Contain(aim));
            }
            foreach(var aim in new[]{P(12,8),P(12,12),P(8,4)})Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireStream,aim,true)),Is.EqualTo(CommandError.OutOfRange));
            Assert.That(BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(12,9))).Blockers,Has.No.Member(CommandError.BlockedLineOfSight));
        }
        [Test] public void TrueCenterlineCannotJumpSolidOmittedByRoundedSamples()
        {
            // (2,1) first sample (1,1), but actual line first enters (1,0).
            var s=State(solids:new[]{P(9,8)});var p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(10,9)));
            Assert.That(p.Error,Is.EqualTo(CommandError.BlockedLineOfSight));Assert.That(p.Cells,Is.Empty);
            // Obstruction after a legal selected near cell truncates continuation.
            s=State(solids:new[]{P(10,8)});p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(9,8)));
            Assert.That(p.IsLegal,Is.True);Assert.That(p.Cells,Is.EqualTo(new[]{P(9,8)}));
        }
        [Test] public void SingleTouchedCornerIsLegalSealedCornerAndMapEdgeStopStream()
        {
            var s=State(solids:new[]{P(9,8)});Assert.That(BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(11,11))).Cells.Count,Is.EqualTo(3));
            s=State(solids:new[]{P(9,8),P(8,9)});Assert.That(BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(11,11))).Cells,Is.Empty);
            s=State();s.FindUnit(A).Position=P(21,8);var p=BattleResolver.PreviewSpell(s,new CastCommand(A,SpellId.FireStream,P(22,8)));Assert.That(p.IsLegal,Is.True);Assert.That(p.Cells,Is.EqualTo(new[]{P(22,8)}));
        }
        [Test] public void EmptyOrOccupiedAimContinuesThroughUnitsExactPreviewFriendlyFireAndNoMutation()
        {
            var s=State();s.FindUnit(Ally).Position=P(9,9);s.FindUnit(new UnitId(2)).Position=P(11,10);string hash=BattleStateHash.Compute(s);
            var c=new CastCommand(A,SpellId.FireStream,P(10,9));var p=BattleResolver.PreviewSpell(s,c);Assert.That(p.Error,Is.EqualTo(CommandError.FriendlyFireNotConfirmed));Assert.That(p.Targets,Is.EquivalentTo(new[]{Ally,new UnitId(2)}));Assert.That(p.Targets,Has.No.Member(A));Assert.That(BattleResolver.Apply(s,c).State,Is.SameAs(s));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            c=new CastCommand(A,SpellId.FireStream,P(10,9),true);var r=BattleResolver.Apply(s,c);Assert.That(r.IsApplied,Is.True);
            foreach(var u in s.Units)Assert.That(r.State.FindUnit(u.Id).Hp,Is.EqualTo(u.Hp-(p.Targets.Contains(u.Id)?11:0)));
            Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.DamageApplied),Is.EqualTo(2));
            s.FindUnit(new UnitId(2)).Position=P(10,9);Assert.That(BattleResolver.PreviewSpell(s,c).Cells,Is.EqualTo(p.Cells));
        }
        [TestCase(UnitProfileId.HumanWarriorTI)][TestCase(UnitProfileId.HumanArcherTI)][TestCase(UnitProfileId.ElfWarriorTI)][TestCase(UnitProfileId.FireMageTI)][TestCase(UnitProfileId.IceMageTII)]
        public void AlliedAnyProfileGetsProtectionCasterAlonePays(UnitProfileId profile)
        {
            var s=State(UnitProfile.Get(profile));var u=s.FindUnit(Ally);bool action=u.ActionAvailable;int hp=u.Hp,armor=u.Armor;var r=Cast(s,SpellId.FireArmor,u.Position);
            u=r.FindUnit(Ally);Assert.That(u.TemporaryBarrier,Is.EqualTo(6));Assert.That(u.FireProtection,Is.True);Assert.That(u.BarrierActivations,Is.EqualTo(2));Assert.That(u.IsExhausted,Is.False);Assert.That(u.ActionAvailable,Is.EqualTo(action));Assert.That(u.Hp,Is.EqualTo(hp));Assert.That(u.Armor,Is.EqualTo(armor));Assert.That(r.FindUnit(A).ActionAvailable,Is.False);Assert.That(r.FindUnit(A).IsExhausted,Is.True);Assert.That(r.FindUnit(A).TemporaryBarrier,Is.Zero);
        }
        [Test] public void SelfAndThirdDiagonalAreLegalButEnemyDeadEscapedEmptyFourthAndWallAreNot()
        {
            var s=State();Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireArmor,P(8,8))),Is.EqualTo(CommandError.None));s.FindUnit(Ally).Position=P(11,11);Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireArmor,P(11,11))),Is.EqualTo(CommandError.None));
            foreach(var status in new[]{UnitStatus.Dead,UnitStatus.Escaped}){s.FindUnit(Ally).Status=status;var h=BattleStateHash.Compute(s);Assert.That(BattleResolver.Apply(s,new CastCommand(A,SpellId.FireArmor,P(11,11))).State,Is.SameAs(s));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(h));}s.FindUnit(Ally).Status=UnitStatus.Active;
            foreach(var cell in new[]{P(12,8),P(9,9),P(14,12)})Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireArmor,cell)),Is.Not.EqualTo(CommandError.None));
            s.FindUnit(new UnitId(2)).Position=P(9,8);Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireArmor,P(9,8))),Is.EqualTo(CommandError.InvalidSpellTarget));
            s=State(solids:new[]{P(9,8)});s.FindUnit(Ally).Position=P(10,8);Assert.That(BattleResolver.Validate(s,new CastCommand(A,SpellId.FireArmor,P(10,8))),Is.EqualTo(CommandError.BlockedLineOfSight));
        }
        [TestCase(false)][TestCase(true)] public void ExpiresAtRecipientsSecondSubsequentStartRegardlessOfCaster(bool recipientAlreadyActed)
        {
            var s=State(recipientAlreadyActed?UnitProfile.HumanArcherTI:UnitProfile.HumanWarriorTI);s=Cast(s,SpellId.FireArmor,s.FindUnit(Ally).Position);
            // Scenario mutation models source death/Silence and recipient displacement, not a played action.
            s.FindUnit(A).IsSilenced=true;s.FindUnit(Ally).Position=P(2,2);
            s=Next(s,Ally);s.FindUnit(A).Hp=0;s.FindUnit(A).Status=UnitStatus.Dead;Assert.That(s.FindUnit(Ally).BarrierActivations,Is.EqualTo(1));Assert.That(s.FindUnit(Ally).FireProtection,Is.True);
            s=Next(s,Ally);Assert.That(s.FindUnit(Ally).BarrierActivations,Is.Zero);Assert.That(s.FindUnit(Ally).FireProtection,Is.False);Assert.That(s.FindUnit(Ally).TemporaryBarrier,Is.Zero);
        }
        [Test] public void ShieldSlotReplacesBothWaysPreservingIndependentBarrierAndExpiresOnlyItsOwnGrant()
        {
            var s=State();s.FindUnit(Ally).TemporaryBarrier=4;s=Cast(s,SpellId.FireArmor,s.FindUnit(Ally).Position);Assert.That(s.FindUnit(Ally).TemporaryBarrier,Is.EqualTo(10));
            s=Next(s,new UnitId(4));s=Cast(s,SpellId.IceShield,s.FindUnit(Ally).Position);Assert.That(s.FindUnit(Ally).TemporaryBarrier,Is.EqualTo(14));Assert.That(s.FindUnit(Ally).FireProtection,Is.False);
            s=Next(s,A);s=Next(s,A);s=Cast(s,SpellId.FireArmor,s.FindUnit(Ally).Position);Assert.That(s.FindUnit(Ally).TemporaryBarrier,Is.EqualTo(10));Assert.That(s.FindUnit(Ally).FireProtection,Is.True);
            s=Next(s,Ally);s=Next(s,Ally);Assert.That(s.FindUnit(Ally).TemporaryBarrier,Is.EqualTo(4));
        }
        [Test] public void NewAndLegacyReplayKeepDifferentRulesAndRecipientIsVerified()
        {
            foreach(int version in new[]{1,2}){
                var s=State(version:version);var j=new BattleJournal(s,"Fire targeting compatibility","test");j.Apply(new CastCommand(A,SpellId.FireStream,P(10,9)),"Player");Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);Assert.That(j.State.FindUnit(A).ActionAvailable,Is.EqualTo(version==1));
                Assert.That(ReplaySnapshot.Capture(j.State).Restore().FireRulesVersion,Is.EqualTo(version));
                if(version==1){j.Header.initial.fireRulesVersion=0;Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);}
            }
            var n=new BattleJournal(State(),"Recipient identity","test");n.Apply(new CastCommand(A,SpellId.FireArmor,P(7,8)),"Player");Assert.That(n.Records[0].command.target,Is.EqualTo(3));Assert.That(ReplayVerification.Verify(n.Header,n.Records,n.Footer()).Matches,Is.True);n.Records[0].command.target=4;Assert.That(ReplayVerification.Verify(n.Header,n.Records,n.Footer()).Matches,Is.False);
        }
        [Test] public void AiEnumeratesOffAxisAndAlliedRecipientsWithoutMutationAndPricesShieldReplacement()
        {
            var s=State();s.FindUnit(new UnitId(2)).Position=P(10,9);string hash=BattleStateHash.Compute(s);var choices=SpellAi.Candidates(s,s.FindUnit(A)).ToArray();Assert.That(choices.Any(c=>c.Spell==SpellId.FireStream&&c.Cell==P(10,9)),Is.True);Assert.That(choices.Any(c=>c.Spell==SpellId.FireArmor&&c.Cell==P(7,8)),Is.True);
            var c=new CastCommand(A,SpellId.FireArmor,P(7,8));double plain=SpellAi.Value(s,c);Assert.That(plain,Is.GreaterThan(0));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));s.FindUnit(Ally).TemporaryBarrier=10;s.FindUnit(Ally).PackageBarrier=10;Assert.That(SpellAi.Value(s,c),Is.Zero);
        }
        [Test] public void AiActuallyChoosesOffAxisStreamAndCanPreferAlliedShield()
        {
            var s=State();s.FindUnit(A).MovementRemaining=0;s.FindUnit(A).ExhaustedActivations=1;s.FindUnit(new UnitId(2)).Position=P(10,9);string h=BattleStateHash.Compute(s);
            var d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<CastCommand>());var c=(CastCommand)d.Command;Assert.That(c.Spell,Is.EqualTo(SpellId.FireStream));Assert.That(BattleResolver.PreviewSpell(s,c).Targets,Does.Contain(new UnitId(2)));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(h));Assert.That(BattleResolver.Apply(s,c).IsApplied,Is.True);
            s=State();s.FindUnit(A).MovementRemaining=0;s.FindUnit(A).TemporaryBarrier=10;s.FindUnit(A).PackageBarrier=10;s.FindUnit(A).FireballUsed=2;
            d=TacticalAi.Choose(s);Assert.That(d.Command,Is.TypeOf<CastCommand>());c=(CastCommand)d.Command;Assert.That(c.Spell,Is.EqualTo(SpellId.FireArmor));Assert.That(c.Cell,Is.Not.EqualTo(s.FindUnit(A).Position));Assert.That(BattleResolver.Apply(s,c).IsApplied,Is.True);
        }
        [Test] public void ReplacingLiveIceSlotAndShieldingAnotherRecipientDoesNotEraseExistingRecipients()
        {
            var s=State();var u=s.FindUnit(Ally);u.TemporaryBarrier=14;u.PackageBarrier=10;u.BarrierActivations=2; // Ice Shield + independent 4.
            var other=s.FindUnit(new UnitId(4));other.TemporaryBarrier=6;other.PackageBarrier=6;other.BarrierActivations=2;other.FireProtection=true;
            s=Cast(s,SpellId.FireArmor,u.Position);u=s.FindUnit(Ally);Assert.That(u.TemporaryBarrier,Is.EqualTo(10));Assert.That(u.PackageBarrier,Is.EqualTo(6));Assert.That(u.FireProtection,Is.True);Assert.That(s.FindUnit(new UnitId(4)).FireProtection,Is.True);Assert.That(s.FindUnit(new UnitId(4)).TemporaryBarrier,Is.EqualTo(6));
            var restored=ReplaySnapshot.Capture(s).Restore();Assert.That(BattleStateHash.Compute(restored),Is.EqualTo(BattleStateHash.Compute(s)));
        }
        [Test] public void DepletedBarrierRetainsFireStateAndDeathOrBattleEndCleansIt()
        {
            var s=State();s.FindUnit(Ally).Position=P(9,9);s.FindUnit(new UnitId(2)).Position=P(10,9);s=Cast(s,SpellId.FireArmor,P(9,9));s=Next(s,new UnitId(2));
            var r=BattleResolver.Apply(s,new BasicAttackCommand(new UnitId(2),Ally));Assert.That(r.IsApplied,Is.True);Assert.That(r.State.FindUnit(Ally).TemporaryBarrier,Is.Zero);Assert.That(r.State.FindUnit(Ally).FireProtection,Is.True);Assert.That(r.State.FindUnit(new UnitId(2)).BurnStacks,Is.EqualTo(1));Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.BurnApplied),Is.EqualTo(1));
            s=State();s.FindUnit(Ally).Hp=1;s.FindUnit(Ally).BurnStacks=1;s.FindUnit(Ally).BurnTicks=1;s=Cast(s,SpellId.FireArmor,s.FindUnit(Ally).Position);s.FindUnit(Ally).TemporaryBarrier=0;s.FindUnit(Ally).PackageBarrier=0;
            s=Next(s,new UnitId(2));s=Next(s,A);Assert.That(s.FindUnit(Ally).Status,Is.EqualTo(UnitStatus.Dead));Assert.That(s.FindUnit(Ally).FireProtection,Is.False);
            s=State();s.FindUnit(new UnitId(2)).Hp=1;s.FindUnit(new UnitId(2)).Position=P(10,9);s.FindUnit(Ally).TemporaryBarrier=6;s.FindUnit(Ally).PackageBarrier=6;s.FindUnit(Ally).FireProtection=true;s.FindUnit(Ally).BarrierActivations=2;
            s=Cast(s,SpellId.FireStream,P(10,9));Assert.That(s.Outcome.IsEnded,Is.True);Assert.That(s.Units.All(x=>x.TemporaryBarrier==0&&!x.FireProtection),Is.True);
        }
    }
}
