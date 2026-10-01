using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class CombatVarietyTests
    {
        private static UnitState U(int id,UnitProfile p,Side side,int x,int y,int? hp=null,int? armor=null)=>new UnitState(new UnitId(id),side,p,new GridPosition(x,y),side==Side.West?Facing.East:Facing.West,hp,armor);
        private static BattleState State(UnitProfile p)=>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{U(1,p,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,10,8),U(3,UnitProfile.HumanWarriorTI,Side.East,12,8)},2,new Battlefield(23,17)).State,new UnitId(1));
        [Test] public void OldProfilesAndNewKitAreSeparate(){Assert.That(UnitProfile.HumanArcherTI.Range,Is.EqualTo(10));Assert.That(SpellRules.Has(UnitProfile.FireMageTI,SpellId.Fireball),Is.False);Assert.That(UnitProfile.ElfWarriorTII.HasMeleeBasic,Is.True);Assert.That(UnitProfile.FireMageTI.HasMeleeBasic,Is.False);Assert.That(UnitProfile.FireMageTII.MagicPower,Is.EqualTo(1.15m));}
        [Test] public void PreviewAndInvalidCastPreserveStateAndRng(){var s=State(UnitProfile.FireMageTI);var hash=BattleStateHash.Compute(s);var c=new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(10,8));Assert.That(BattleResolver.PreviewSpell(s,c).Error,Is.EqualTo(CommandError.AbilityUnavailable));Assert.That(BattleResolver.Apply(s,c).State,Is.SameAs(s));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));}
        [Test] public void StreamBypassesArmorAndStopsAtThreeCells(){var s=State(UnitProfile.FireMageTI);var r=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.FireStream,new GridPosition(9,8)));Assert.That(r.IsApplied,Is.True);Assert.That(r.State.FindUnit(new UnitId(2)).Hp,Is.EqualTo(30));Assert.That(r.State.FindUnit(new UnitId(2)).Armor,Is.EqualTo(16));Assert.That(r.State.FindUnit(new UnitId(3)).Hp,Is.EqualTo(40));Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.Zero);}
        [Test] public void FriendlyFireRequiresExplicitConfirmation(){var s=State(UnitProfile.FireMageTII);var c=new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(9,8));Assert.That(BattleResolver.Validate(s,c),Is.EqualTo(CommandError.FriendlyFireNotConfirmed));var r=BattleResolver.Apply(s,new CastCommand(c.Actor,c.Spell,c.Cell,true));Assert.That(r.State.FindUnit(c.Actor).Hp,Is.EqualTo(16));Assert.That(r.State.FindUnit(c.Actor).FireballUsed,Is.EqualTo(1));}
        [Test] public void ExhaustionBlocksExertionButNotOrdinarySpellOnNextActivation(){var s=State(UnitProfile.FireMageTII);s=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.FireArmor,new GridPosition(8,8))).State;s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(new UnitId(1))).State,new UnitId(1));Assert.That(BattleResolver.Validate(s,new CastCommand(new UnitId(1),SpellId.FireArmor,new GridPosition(8,8))),Is.EqualTo(CommandError.Exhausted));Assert.That(BattleResolver.Validate(s,new CastCommand(new UnitId(1),SpellId.FireStream,new GridPosition(9,8))),Is.EqualTo(CommandError.None));}
        [Test] public void HealClampsAndCleansesBurnFirst(){var s=State(UnitProfile.HumanHealerTI);var u=s.FindUnit(new UnitId(1));u.Hp=25;u.BurnStacks=2;u.BurnTicks=2;u.PoisonStacks=1;var r=BattleResolver.Apply(s,new CastCommand(u.Id,SpellId.CloseHeal,u.Position));var healed=r.State.FindUnit(u.Id);Assert.That(healed.Hp,Is.EqualTo(28));Assert.That(healed.BurnStacks,Is.Zero);Assert.That(healed.PoisonStacks,Is.EqualTo(1));Assert.That(healed.CloseHealUsed,Is.EqualTo(1));}
        [Test] public void SpellSequenceReplaysBudgetsAndStatuses(){var j=new BattleJournal(State(UnitProfile.FireMageTII),"Lab","test");j.Apply(new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(10,8),true));for(int i=0;i<6&&!j.State.Outcome.IsEnded;i++)j.Apply(new EndActivationCommand(j.State.CurrentUnitId.Value));var check=ReplayVerification.Verify(j.Header,j.Records,j.Footer());Assert.That(check.Matches,Is.True,check.Message);}
        [Test] public void FreezeDeniesOnlyUntilEndOfNextActivation(){var s=State(UnitProfile.IceMageTII);var r=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.Freeze,new GridPosition(10,8)));Assert.That(r.IsApplied,Is.True);Assert.That(r.State.FindUnit(new UnitId(1)).FreezeUsed,Is.EqualTo(1));}
        [Test] public void SourceBudgetIsValidatedBeforeCommit(){var s=State(UnitProfile.FireMageTII);s.FindUnit(new UnitId(1)).FireballUsed=2;Assert.That(BattleResolver.Validate(s,new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(10,8))),Is.EqualTo(CommandError.SourceBudgetSpent));}

        [Test] public void DualHitUsesTwoContactChecksButOaUsesOne()
        {
            var s=State(UnitProfile.ElfWarriorTII);s.FindUnit(new UnitId(2)).Position=new GridPosition(9,8);
            var r=BattleResolver.Apply(s,new BasicAttackCommand(new UnitId(1),new UnitId(2)));
            Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.EqualTo(2));
            Assert.That(r.State.FindUnit(new UnitId(1)).GracefulExitTarget,Is.EqualTo(new UnitId(2)));
            var move=new MoveCommand(new UnitId(1),new[]{new GridPosition(7,8)});
            Assert.That(OpportunityAttackPreview.Query(r.State,move).Exposures.SelectMany(e=>e.Threats).All(t=>!t.WouldReact),Is.True);
            var exit=BattleResolver.Apply(r.State,move);Assert.That(exit.IsApplied,Is.True);Assert.That(exit.Events.Any(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.False);
        }
        [Test] public void GracefulExitDoesNotSuppressSecondEnemy()
        {
            var s=State(UnitProfile.ElfWarriorTII);s.FindUnit(new UnitId(2)).Position=new GridPosition(9,8);s.FindUnit(new UnitId(3)).Position=new GridPosition(8,9);
            s=BattleResolver.Apply(s,new BasicAttackCommand(new UnitId(1),new UnitId(2))).State;
            var c=new MoveCommand(new UnitId(1),new[]{new GridPosition(7,7)});
            var r=BattleResolver.Apply(s,c);Assert.That(r.IsApplied,Is.True);
            Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.EqualTo(1));
            Assert.That(r.Events.Single(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered).Actor,Is.EqualTo(new UnitId(3)));
        }
        [Test] public void ShieldExpiresAtSecondSubsequentActivationAndDoesNotRepairArmor()
        {
            var s=State(UnitProfile.IceMageTI);var id=new UnitId(1);
            s=BattleResolver.Apply(s,new CastCommand(id,SpellId.IceShield,new GridPosition(8,8))).State;
            s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(id)).State,id);
            Assert.That(s.FindUnit(id).TemporaryBarrier,Is.EqualTo(10));
            s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(id)).State,id);
            Assert.That(s.FindUnit(id).TemporaryBarrier,Is.Zero);Assert.That(s.FindUnit(id).Armor,Is.Zero);
        }
        [Test] public void BurnTicksTwiceNotOnApplicationAndCannotBreakFreeze()
        {
            var s=State(UnitProfile.HumanWarriorTI);var u=s.FindUnit(new UnitId(2));u.BurnStacks=3;u.BurnTicks=2;u.FrozenActivations=1;
            s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(new UnitId(1))).State,u.Id);
            Assert.That(s.FindUnit(u.Id).Hp,Is.EqualTo(34));Assert.That(s.FindUnit(u.Id).IsFrozen,Is.True);
            Assert.That(s.FindUnit(u.Id).ActionAvailable,Is.False);Assert.That(s.FindUnit(u.Id).MovementRemaining,Is.Zero);
            s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(u.Id)).State,u.Id);
            Assert.That(s.FindUnit(u.Id).Hp,Is.EqualTo(28));Assert.That(s.FindUnit(u.Id).BurnStacks,Is.Zero);
        }
        [Test] public void PositiveDirectElementalDamageBreaksFreezeAndBarrierAbsorbsFirst()
        {
            var s=State(UnitProfile.FireMageTI);var target=s.FindUnit(new UnitId(2));target.TemporaryBarrier=6;target.FrozenActivations=1;
            var r=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.FireStream,new GridPosition(9,8)));
            target=r.State.FindUnit(target.Id);Assert.That(target.TemporaryBarrier,Is.Zero);Assert.That(target.Hp,Is.EqualTo(36));Assert.That(target.IsFrozen,Is.False);Assert.That(target.Armor,Is.EqualTo(16));
        }
        [Test] public void StreamSealedCornerAndWallBlockPropagation()
        {
            var s=BattleResolver.StartBattle(new[]{U(1,UnitProfile.FireMageTI,Side.West,8,8),U(2,UnitProfile.HumanWarriorTI,Side.East,10,10)},2,new Battlefield(23,17,new[]{new GridPosition(9,8),new GridPosition(8,9)})).State;
            s=BattleTestFixtures.ToActor(s,new UnitId(1));var p=BattleResolver.PreviewSpell(s,new CastCommand(new UnitId(1),SpellId.FireStream,new GridPosition(9,9)));
            Assert.That(p.IsLegal,Is.False);Assert.That(p.Targets.Count,Is.Zero);
        }
        [Test] public void SpellAiUsesResolverWithoutPreviewMutation()
        {
            var s=State(UnitProfile.FireMageTII);string hash=BattleStateHash.Compute(s);var d=TacticalAi.Choose(s);
            Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));Assert.That(BattleResolver.Validate(s,d.Command),Is.EqualTo(CommandError.None));
        }

        [TestCase(CombatLabMatch.FireVsIce)][TestCase(CombatLabMatch.SupportVsFire)][TestCase(CombatLabMatch.MobileBlades)]
        public void ControlledLabAiMatchCompletesLegallyAndReplays(CombatLabMatch match)
        {
            var journal=new BattleJournal(BattleResolver.StartBattle(CombatLab.Units(match),5051,CombatLab.Board(match)).State,"controlled-Lab-"+match,"test");
            for(int i=0;i<600&&!journal.State.Outcome.IsEnded;i++){var d=TacticalAi.Choose(journal.State);Assert.That(journal.Apply(d.Command,"AI",d.Explanation).IsApplied,Is.True);}
            Assert.That(journal.State.Outcome.IsEnded,Is.True,"Bounded no-stall check "+match);
            var replay=ReplayVerification.Verify(journal.Header,journal.Records,journal.Footer());Assert.That(replay.Matches,Is.True,replay.Message);
            TestContext.WriteLine(match+": "+journal.State.Round+" rounds, "+journal.Records.Count+" commands, "+journal.State.Outcome.Reason+", casts "+journal.Records.SelectMany(r=>r.events).Count(e=>e.kind=="SpellCast"));
        }
    }
}
