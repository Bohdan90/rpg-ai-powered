using System;
using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class EngagementCastingTests
    {
        static readonly UnitId C=new UnitId(1),E=new UnitId(2);
        static BattleState State(UnitProfile profile=null,int rules=4,UnitProfile enemy=null)
        =>BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,profile??UnitProfile.IceMageTII,x:8,y:8,hp:20),BattleTestFixtures.Unit(2,enemy??UnitProfile.HumanWarriorTI,Side.East,9,8)},23,fireRulesVersion:rules).State,C);
        [TestCase(SpellId.FireStream,true)][TestCase(SpellId.FireArmor,true)][TestCase(SpellId.Fireball,false)]
        [TestCase(SpellId.IceShard,false)][TestCase(SpellId.IceShield,true)][TestCase(SpellId.Freeze,false)][TestCase(SpellId.CloseHeal,true)]
        public void ExplicitAssignmentsGovernPreviewAndCommit(SpellId spell,bool allowed)
        {
            var profile=spell==SpellId.CloseHeal?UnitProfile.HumanHealerTI:spell<=SpellId.Fireball?UnitProfile.FireMageTII:UnitProfile.IceMageTII;
            var s=State(profile);var actor=s.FindUnit(C);var cast=new CastCommand(C,spell,SpellRules.Helpful(spell)?actor.Position:s.FindUnit(E).Position,true);
            Assert.That(SpellRules.EngagementCastingFor(spell),Is.EqualTo(allowed?EngagementCasting.Allowed:EngagementCasting.Blocked));Assert.That(ZoneOfControl.Sources(s,Side.West,actor.Position).Count,Is.EqualTo(1));
            string hash=BattleStateHash.Compute(s);uint rng=s.RngState;var p=BattleResolver.PreviewSpell(s,cast);Assert.That(p.IsLegal,Is.EqualTo(allowed));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            var result=BattleResolver.Apply(s,cast);Assert.That(result.IsApplied,Is.EqualTo(allowed));
            if(!allowed){Assert.That(p.Error,Is.EqualTo(CommandError.BlockedWhileEngaged));Assert.That(result.State,Is.SameAs(s));Assert.That(s.RngState,Is.EqualTo(rng));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));Assert.That(actor.ActionAvailable,Is.True);Assert.That(SpellRules.Used(actor,spell),Is.Zero);}
        }
        [Test] public void MandatoryDataIsExhaustiveAndUnknownSpellNeverGetsDefault()
        {SpellRules.ValidateData();Assert.That(Enum.IsDefined(typeof(EngagementCasting),default(EngagementCasting)),Is.False);foreach(SpellId spell in Enum.GetValues(typeof(SpellId)))Assert.That(Enum.IsDefined(typeof(EngagementCasting),SpellRules.EngagementCastingFor(spell)),Is.True);Assert.Throws<InvalidOperationException>(()=>SpellRules.EngagementCastingFor((SpellId)999));}
        [TestCase(UnitProfileId.FireMageTII)][TestCase(UnitProfileId.IceMageTII)]
        public void StaffRemainsLegal(UnitProfileId profile){var s=State(UnitProfile.Get(profile));Assert.That(BattleResolver.Validate(s,new BasicAttackCommand(C,E)),Is.EqualTo(CommandError.None));}
        [Test] public void AdjacentArcherWithoutMeleeZocDoesNotBlock()
        {var s=State(enemy:UnitProfile.HumanArcherTI);Assert.That(ZoneOfControl.Sources(s,Side.West,s.FindUnit(C).Position),Is.Empty);Assert.That(BattleResolver.PreviewSpell(s,new CastCommand(C,SpellId.IceShard,s.FindUnit(E).Position)).IsLegal,Is.True);}
        [Test] public void SealedCornerAdjacencyIsNotEngagement()
        {
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.IceMageTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,9,9)},1,new Battlefield(23,17,new[]{new GridPosition(9,8),new GridPosition(8,9)})).State,C);
            Assert.That(BattleResolver.IsSpellEngagementBlocked(s,C,SpellId.IceShard),Is.False);
        }
        [Test] public void MultipleEngagersAndSpentOaStillBlock()
        {
            var s=BattleTestFixtures.ToActor(BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.IceMageTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,9,8),BattleTestFixtures.Unit(3,UnitProfile.ElfWarriorTI,Side.East,8,9)},1,new Battlefield(23,17)).State,C);
            s.FindUnit(E).OpportunityAttackAvailable=false;Assert.That(ZoneOfControl.Sources(s,Side.West,s.FindUnit(C).Position).Count,Is.EqualTo(2));Assert.That(BattleResolver.Validate(s,new CastCommand(C,SpellId.IceShard,s.FindUnit(E).Position)),Is.EqualTo(CommandError.BlockedWhileEngaged));
        }
        [Test] public void LawfulDisengagementRestoresCastAndJournalReplays()
        {
            var s=State();var j=new BattleJournal(s,"engagement-disengage","controlled");var c=new CastCommand(C,SpellId.IceShard,s.FindUnit(E).Position);string before=BattleStateHash.Compute(s);
            Assert.That(j.Apply(c).IsApplied,Is.False);Assert.That(BattleStateHash.Compute(j.State),Is.EqualTo(before));
            var movement=j.Apply(new MoveCommand(C,new[]{new GridPosition(7,8)}));Assert.That(movement.IsApplied,Is.True);Assert.That(j.State.FindUnit(C).IsActive,Is.True);Assert.That(BattleResolver.IsSpellEngagementBlocked(j.State,C,SpellId.IceShard),Is.False);Assert.That(j.Apply(c).IsApplied,Is.True);
            Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
        }
        [Test] public void OtherBlockersRemainIndependentAndInvalidCommandsPure()
        {
            var s=State();var u=s.FindUnit(C);u.IsSilenced=true;u.ExhaustedActivations=2;u.FreezeUsed=2;var c=new CastCommand(C,SpellId.Freeze,s.FindUnit(E).Position);string hash=BattleStateHash.Compute(s);
            var p=BattleResolver.PreviewSpell(s,c);Assert.That(p.Blockers,Does.Contain(CommandError.Silenced));Assert.That(p.Blockers,Does.Contain(CommandError.Exhausted));Assert.That(p.Blockers,Does.Contain(CommandError.SourceBudgetSpent));Assert.That(p.Blockers,Does.Contain(CommandError.BlockedWhileEngaged));Assert.That(BattleResolver.Apply(s,c).IsApplied,Is.False);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            u.ActionAvailable=false;Assert.That(BattleResolver.PreviewSpell(s,c).Blockers,Does.Contain(CommandError.NoAction));Assert.That(BattleResolver.Validate(s,new CastCommand(C,(SpellId)999,u.Position)),Is.Not.EqualTo(CommandError.None));
        }
        [Test] public void AiFiltersBlockedSpellsThroughCoreAndStillHasAllowedShield()
        {
            var s=State();var candidates=SpellAi.Candidates(s,s.FindUnit(C)).ToArray();Assert.That(candidates.Any(c=>c.Spell==SpellId.IceShield),Is.True);Assert.That(candidates.Any(c=>c.Spell==SpellId.IceShard||c.Spell==SpellId.Freeze),Is.False);
            string hash=BattleStateHash.Compute(s);var chosen=TacticalAi.Choose(s).Command;Assert.That(BattleResolver.Validate(s,chosen),Is.EqualTo(CommandError.None));Assert.That(chosen is CastCommand cast&&(cast.Spell==SpellId.IceShard||cast.Spell==SpellId.Freeze),Is.False);Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
        }
        [Test] public void HistoricalRulesThreeRemainReplayableWithoutNewEngagementLock()
        {
            var old=State(rules:3);var j=new BattleJournal(old,"legacy-engaged-ice","regression");Assert.That(j.Apply(new CastCommand(C,SpellId.IceShard,old.FindUnit(E).Position)).IsApplied,Is.True);Assert.That(ReplayVerification.Verify(j.Header,j.Records,j.Footer()).Matches,Is.True);
            Assert.That(j.Header.configVersion,Is.EqualTo(ReplayHeader.Input03Config));Assert.That(ReplaySnapshot.Capture(State()).fireRulesVersion,Is.EqualTo(4));
        }
    }
}
