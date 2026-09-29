using System.Linq;
using NUnit.Framework;
using RPG.Core;
using static RPG.Tests.BattleTestFixtures;

namespace RPG.Tests
{
    public class ArcherEngagementTests
    {
        private static GridPosition P(int x,int y) => new GridPosition(x,y);
        private static BasicAttackCommand Strike() => new BasicAttackCommand(Attacker,Target,kind:BasicAttackKind.MeleeStrike);
        private static BattleState Setup(UnitProfile enemy, GridPosition target, params GridPosition[] walls) =>
            ToActor(BattleResolver.StartBattle(new[] {
                Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),
                Unit(2,enemy,Side.East,x:target.X,y:target.Y,armor:4,facing:Facing.East)
            },2,new Battlefield(19,13,walls)).State,Attacker);

        [TestCase(UnitProfileId.HumanWarriorTI)]
        [TestCase(UnitProfileId.ElfWarriorTI)]
        public void MeleeSourceLocksBowButAllowsFiveDamageFallback(UnitProfileId profile)
        {
            var state=Setup(profile==UnitProfileId.HumanWarriorTI?UnitProfile.HumanWarriorTI:UnitProfile.ElfWarriorTI,P(5,4));
            string before=Snapshot(state);
            Assert.That(BattleResolver.IsArcherEngaged(state,Attacker),Is.True);
            Assert.That(BattleResolver.PreviewAttack(state,Attack()).Error,Is.EqualTo(CommandError.Engaged));
            var invalid=BattleResolver.Apply(state,Attack());
            Assert.That(invalid.State,Is.SameAs(state)); Assert.That(invalid.Events,Is.Empty);
            var preview=BattleResolver.PreviewAttack(state,Strike());
            Assert.That(preview.IsLegal,Is.True); Assert.That(preview.MaximumRange,Is.EqualTo(1));
            Assert.That(preview.PhysicalDamage,Is.EqualTo(5)); Assert.That(preview.BaseAccuracy,Is.EqualTo(80));
            Assert.That(preview.AimModifier,Is.Zero); Assert.That(preview.SteadyAim,Is.False);
            Assert.That(preview.DistanceModifier,Is.Zero); Assert.That(preview.Cover,Is.EqualTo(CoverLevel.None));
            Assert.That(Snapshot(state),Is.EqualTo(before));
            var result=BattleResolver.Apply(state,Strike());
            Assert.That(result.IsApplied,Is.True);
            Assert.That(result.Events.Single(e=>e.Kind==BattleEventKind.ContactRolled).ChancePercent,Is.EqualTo(preview.ContactChance));
            Assert.That(result.State.FindUnit(Target).Armor,Is.Zero);
            Assert.That(result.State.FindUnit(Target).Hp,Is.EqualTo(state.FindUnit(Target).Hp-1));
            Assert.That(result.State.FindUnit(Attacker).ActionAvailable,Is.False);
            Assert.That(result.State.FindUnit(Attacker).MovementRemaining,Is.EqualTo(4));
            Assert.That(BattleResolver.Validate(result.State,Strike()),Is.EqualTo(CommandError.NoAction));
        }

        [Test]
        public void FallbackNeverGrantsZocOrOaEvenAfterRefresh()
        {
            var state=Setup(UnitProfile.HumanWarriorTI,P(5,4));
            Assert.That(ZoneOfControl.Exerts(state,state.FindUnit(Attacker),P(5,4)),Is.False);
            Assert.That(state.FindUnit(Attacker).OpportunityAttackAvailable,Is.False);
            state=BattleResolver.Apply(state,Strike()).State;
            state=BattleResolver.Apply(state,new EndActivationCommand(Attacker)).State;
            state=ToActor(state,Attacker);
            Assert.That(state.FindUnit(Attacker).OpportunityAttackAvailable,Is.False);
            Assert.That(ZoneOfControl.Sources(state,Side.East,P(5,4)),Is.Empty);
            Assert.That(ZoneOfControl.Reactors(state,Target,P(5,4),P(6,4)),Is.Empty);
        }

        [Test]
        public void AdjacentArcherDoesNotLockBowAndUnengagedFallbackIsUnavailable()
        {
            var state=Setup(UnitProfile.HumanArcherTI,P(5,4));
            Assert.That(BattleResolver.IsArcherEngaged(state,Attacker),Is.False);
            Assert.That(BattleResolver.PreviewAttack(state,Attack()).PhysicalDamage,Is.EqualTo(10));
            Assert.That(BattleResolver.Apply(state,Attack()).IsApplied,Is.True);
            Assert.That(BattleResolver.Validate(state,Strike()),Is.EqualTo(CommandError.MeleeStrikeUnavailable));
        }

        [TestCase(UnitStatus.Dead)]
        [TestCase(UnitStatus.Escaped)]
        public void InactiveMeleeSourceDoesNotEngage(UnitStatus status)
        {
            var state=Setup(UnitProfile.HumanWarriorTI,P(5,4));
            state.FindUnit(Target).Status=status;
            Assert.That(BattleResolver.IsArcherEngaged(state,Attacker),Is.False);
        }

        [Test]
        public void OneSourceSufficesAndSpentOaStillLocksBow()
        {
            var state=ToActor(BattleResolver.StartBattle(new[] {
                Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),
                Unit(2,UnitProfile.HumanArcherTI,Side.East,x:5,y:4),
                Unit(3,UnitProfile.HumanWarriorTI,Side.East,x:4,y:5)
            },2).State,Attacker);
            state.FindUnit(new UnitId(3)).OpportunityAttackAvailable=false;
            Assert.That(BattleResolver.Validate(state,Attack()),Is.EqualTo(CommandError.Engaged));
            Assert.That(BattleResolver.Validate(state,Strike()),Is.EqualTo(CommandError.None));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EngagementUsesExistingCornerTruth(bool sealedCorner)
        {
            var walls=sealedCorner?new[]{P(5,4),P(4,5)}:new[]{P(5,4)};
            var state=Setup(UnitProfile.HumanWarriorTI,P(5,5),walls);
            Assert.That(BattleResolver.IsArcherEngaged(state,Attacker),Is.EqualTo(!sealedCorner));
            Assert.That(BattleResolver.Validate(state,Strike()),Is.EqualTo(sealedCorner?CommandError.MeleeStrikeUnavailable:CommandError.None));
        }

        [Test]
        public void SurvivingDisengagementTriggersOaAndRestoresBowWithoutAim()
        {
            var state=Setup(UnitProfile.ElfWarriorTI,P(5,4));
            var result=BattleResolver.Apply(state,new MoveCommand(Attacker,new[]{P(3,4)}));
            Assert.That(result.IsApplied,Is.True);
            Assert.That(result.Events.Count(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.EqualTo(1));
            Assert.That(result.State.FindUnit(Attacker).IsActive,Is.True);
            var preview=BattleResolver.PreviewAttack(result.State,Attack());
            Assert.That(preview.IsLegal,Is.True); Assert.That(preview.MaximumRange,Is.EqualTo(10));
            Assert.That(preview.AimModifier,Is.Zero); Assert.That(preview.PhysicalDamage,Is.EqualTo(10));
            Assert.That(BattleResolver.Apply(result.State,Attack()).IsApplied,Is.True);
        }

        [Test]
        public void LeavingOnlyOneOfTwoSourcesDoesNotUnlockBow()
        {
            var state=ToActor(BattleResolver.StartBattle(new[] {
                Unit(1,UnitProfile.HumanArcherTI,x:4,y:4),
                Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:5,y:4),
                Unit(3,UnitProfile.HumanWarriorTI,Side.East,x:3,y:5)
            },2).State,Attacker);
            var moved=BattleResolver.Apply(state,new MoveCommand(Attacker,new[]{P(3,4)}));
            Assert.That(moved.IsApplied,Is.True);
            Assert.That(BattleResolver.Validate(moved.State,Attack()),Is.EqualTo(CommandError.Engaged));
        }

        [Test]
        public void FallbackUsesOrdinaryGuardAndResistance()
        {
            var state=Setup(UnitProfile.HumanWarriorTI,P(5,4));
            state.FindUnit(Target).Facing=Facing.West; state.FindUnit(Target).IsDefending=true;
            var preview=BattleResolver.PreviewAttack(state,Strike());
            Assert.That(preview.GuardChance,Is.EqualTo(15)); Assert.That(preview.PhysicalDamage,Is.EqualTo(3));
            var result=BattleResolver.Apply(state,Strike());
            Assert.That(result.Events.Any(e=>e.Kind==BattleEventKind.GuardRolled),Is.True);
            Assert.That(result.Events.Any(e=>e.Kind==BattleEventKind.SteadyAimApplied),Is.False);
        }

        [Test]
        public void FallbackCannotBypassRangeOrBeUsedByWarrior()
        {
            var state=Setup(UnitProfile.HumanWarriorTI,P(6,4));
            Assert.That(BattleResolver.Validate(state,Strike()),Is.EqualTo(CommandError.MeleeStrikeUnavailable));
            state=ToActor(BattleResolver.StartBattle(new[] {
                Unit(1,UnitProfile.HumanArcherTI,x:4,y:4), Unit(2,UnitProfile.HumanWarriorTI,Side.East,x:6,y:4),
                Unit(3,UnitProfile.HumanWarriorTI,Side.East,x:4,y:5)
            },2).State,Attacker);
            Assert.That(BattleResolver.Validate(state,Strike()),Is.EqualTo(CommandError.OutOfRange));
            Assert.That(BattleResolver.Validate(Duel(),Strike()),Is.EqualTo(CommandError.MeleeStrikeUnavailable));
        }
    }
}
