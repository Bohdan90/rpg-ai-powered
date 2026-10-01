using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class CombatVarietyAcceptanceTests
    {
        static readonly UnitId Actor=new UnitId(1), Target=new UnitId(2);
        static BattleState Setup(UnitProfile profile,uint seed=2,int hp=40)
        {
            var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,profile,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,10,8,hp:hp),BattleTestFixtures.Unit(3,UnitProfile.HumanWarriorTI,Side.East,15,8)},seed,new Battlefield(23,17)).State;
            return BattleTestFixtures.ToActor(s,Actor);
        }
        static BattleState Apply(BattleState s,BattleCommand c){var r=BattleResolver.Apply(s,c);Assert.That(r.IsApplied,Is.True,r.Error.ToString());return r.State;}
        [Test] public void FreezeActuallyDeniesActivationThenExpiresWithoutBonusTurn()
        {
            var s=Setup(UnitProfile.IceMageTII);s=Apply(s,new CastCommand(Actor,SpellId.Freeze,new GridPosition(10,8)));
            Assert.That(s.FindUnit(Target).IsFrozen,Is.True);
            s=BattleTestFixtures.ToActor(Apply(s,new EndActivationCommand(Actor)),Target);
            var u=s.FindUnit(Target);Assert.That(u.ActionAvailable,Is.False);Assert.That(u.MovementRemaining,Is.Zero);Assert.That(u.OpportunityAttackAvailable,Is.False);
            var r=BattleResolver.Apply(s,new EndActivationCommand(Target));Assert.That(r.IsApplied,Is.True);
            Assert.That(r.State.FindUnit(Target).IsFrozen,Is.False);Assert.That(r.State.CurrentUnitId,Is.Not.EqualTo(Target));
            Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.ActivationEnded&&e.Actor==Target),Is.EqualTo(1));
        }
        [Test] public void FailedDirectedContactDoesNotBreakFreezeOrSpendAnotherRoll()
        {
            BattleResult miss=null;
            for(uint seed=1;seed<100&&miss==null;seed++){
                var s=Setup(UnitProfile.IceMageTI,seed);s.FindUnit(Target).FrozenActivations=1;
                var r=BattleResolver.Apply(s,new CastCommand(Actor,SpellId.IceShard,new GridPosition(10,8)));
                if(r.Events.Any(e=>e.Kind==BattleEventKind.AttackMissed))miss=r;
            }
            Assert.That(miss,Is.Not.Null);Assert.That(miss.State.FindUnit(Target).IsFrozen,Is.True);Assert.That(miss.State.FindUnit(Target).Hp,Is.EqualTo(40));
            Assert.That(miss.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.EqualTo(1));
        }
        [Test] public void BurnReapplicationAtCapRefreshesTicksWithoutImmediateTick()
        {
            BattleResult applied=null;
            for(uint seed=1;seed<100&&applied==null;seed++){
                var s=Setup(UnitProfile.FireMageTI,seed);s.FindUnit(Target).BurnStacks=3;s.FindUnit(Target).BurnTicks=1;
                var r=BattleResolver.Apply(s,new CastCommand(Actor,SpellId.FireStream,new GridPosition(9,8)));
                if(r.Events.Any(e=>e.Kind==BattleEventKind.BurnApplied))applied=r;
            }
            Assert.That(applied,Is.Not.Null);var u=applied.State.FindUnit(Target);
            Assert.That(u.BurnStacks,Is.EqualTo(3));Assert.That(u.BurnTicks,Is.EqualTo(2));Assert.That(u.Hp,Is.EqualTo(30));
            Assert.That(applied.Events.Any(e=>e.Kind==BattleEventKind.BurnTick),Is.False);
        }
        [Test] public void BurnDeathSkipsDeadActivationAndPreservesArmor()
        {
            var s=Setup(UnitProfile.FireMageTI,hp:2);var u=s.FindUnit(Target);u.BurnStacks=1;u.BurnTicks=2;
            for(int i=0;i<10&&s.FindUnit(Target).IsActive;i++)s=Apply(s,new EndActivationCommand(s.CurrentUnitId.Value));
            Assert.That(s.FindUnit(Target).Status,Is.EqualTo(UnitStatus.Dead));Assert.That(s.FindUnit(Target).Armor,Is.EqualTo(16));Assert.That(s.CurrentUnitId,Is.Not.EqualTo(Target));
        }
        [Test] public void FireArmorRetaliatesOnceAfterPhysicalHitEvenWithoutBarrierAndDoesNotRecurse()
        {
            var s=Setup(UnitProfile.HumanWarriorTI);s.FindUnit(Target).Position=new GridPosition(9,8);s.FindUnit(Target).Facing=Facing.East;
            s.FindUnit(Target).FireProtection=true;s.FindUnit(Target).BarrierActivations=2;
            s.FindUnit(Actor).FireProtection=true;s.FindUnit(Actor).BarrierActivations=2;
            var r=BattleResolver.Apply(s,new BasicAttackCommand(Actor,Target));Assert.That(r.IsApplied,Is.True);
            Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.BurnApplied),Is.EqualTo(1));Assert.That(r.State.FindUnit(Actor).BurnStacks,Is.EqualTo(1));Assert.That(r.State.FindUnit(Target).BurnStacks,Is.Zero);
        }
        [Test] public void FireballCannotPropagateThroughSealedCorner()
        {
            var board=new Battlefield(23,17,new[]{new GridPosition(11,8),new GridPosition(10,9)});
            var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.FireMageTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,11,9)},2,board).State;
            s=BattleTestFixtures.ToActor(s,Actor);var c=new CastCommand(Actor,SpellId.Fireball,new GridPosition(10,8));var p=BattleResolver.PreviewSpell(s,c);
            Assert.That(p.IsLegal,Is.True);Assert.That(p.Targets.Contains(Target),Is.False);Assert.That(Apply(s,c).FindUnit(Target).Hp,Is.EqualTo(40));
        }
        [TestCase(SpellId.FireStream,11)][TestCase(SpellId.Fireball,16)][TestCase(SpellId.IceShard,13)]
        public void TierTwoScalesOnlyDirectMagnitude(SpellId spell,int expected)
        {Assert.That(SpellRules.Magnitude(spell==SpellId.IceShard?UnitProfile.IceMageTII:UnitProfile.FireMageTII,spell),Is.EqualTo(expected));}
        [Test] public void HealRejectsFullDeadHostileAndEmptyTargetsWithoutMutation()
        {
            var s=Setup(UnitProfile.HumanHealerTI);s.FindUnit(Target).Position=new GridPosition(9,8);var hash=BattleStateHash.Compute(s);
            foreach(var cell in new[]{new GridPosition(8,8),new GridPosition(8,9),new GridPosition(9,8)}){
                var r=BattleResolver.Apply(s,new CastCommand(Actor,SpellId.CloseHeal,cell));Assert.That(r.IsApplied,Is.False);Assert.That(r.State,Is.SameAs(s));Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            }
            s.FindUnit(Target).Position=new GridPosition(9,8);s.FindUnit(Target).Hp=0;s.FindUnit(Target).Status=UnitStatus.Dead;
            Assert.That(BattleResolver.Validate(s,new CastCommand(Actor,SpellId.CloseHeal,new GridPosition(9,8))),Is.EqualTo(CommandError.TargetNotFound));
        }
        [Test] public void FullHpCleanseUsesOneBudgetAndLeavesArmorAndOtherConditions()
        {
            var s=Setup(UnitProfile.HumanHealerTI);var u=s.FindUnit(Actor);u.BurnStacks=3;u.BurnTicks=2;u.PoisonStacks=2;u.BleedStacks=1;
            s=Apply(s,new CastCommand(Actor,SpellId.CloseHeal,u.Position));u=s.FindUnit(Actor);
            Assert.That(u.Hp,Is.EqualTo(28));Assert.That(u.Armor,Is.Zero);Assert.That(u.BurnStacks,Is.Zero);Assert.That(u.PoisonStacks,Is.EqualTo(2));Assert.That(u.BleedStacks,Is.EqualTo(1));Assert.That(u.CloseHealUsed,Is.EqualTo(1));
        }
        [Test] public void CloseHealBudgetAndDamagedArmorCarryThroughTwoResolvedBattlesWithoutRefresh()
        {
            var healer=new PersistentCharacter("same-healer",UnitProfile.HumanHealerTI,true);
            var ally=new PersistentCharacter("same-wounded",UnitProfile.HumanWarriorTI,hp:17,armor:5);
            var enemy=new PersistentCharacter("same-enemy",UnitProfile.HumanWarriorTI,true);
            var west=new PersistentFormation("west",Side.West,new[]{healer,ally});var east=new PersistentFormation("east",Side.East,new[]{enemy});
            var positions=new[]{new PersistentDeployment(healer.CharacterId,Actor,new GridPosition(1,7),Facing.East),new PersistentDeployment(ally.CharacterId,Target,new GridPosition(1,8),Facing.East),new PersistentDeployment(enemy.CharacterId,new UnitId(3),new GridPosition(15,8),Facing.West)};
            for(int n=1;n<=2;n++){
                int before=ally.Hp;var battle=PersistentBattle.Start(west,east,positions,2,new Battlefield(23,17));var state=battle.State;
                Assert.That(state.FindUnit(Target).Hp,Is.EqualTo(before));Assert.That(state.FindUnit(Target).Armor,Is.EqualTo(5));Assert.That(state.FindUnit(Actor).CloseHealUsed,Is.EqualTo(n-1));
                state=BattleTestFixtures.ToActor(state,Actor);state=Apply(state,new CastCommand(Actor,SpellId.CloseHeal,new GridPosition(1,8)));
                for(int i=0;i<30&&!state.Outcome.IsEnded;i++){
                    var u=state.FindUnit(state.CurrentUnitId.Value);
                    state=u.Side==Side.West&&u.CanMove&&u.MovementRemaining>0?Apply(state,new MoveCommand(u.Id,new[]{new GridPosition(0,u.Position.Y)})):Apply(state,new EndActivationCommand(u.Id));
                }
                Assert.That(state.Outcome.Reason,Is.EqualTo(BattleEndReason.Withdrawal));battle.Resolve(state);
                Assert.That(healer.CloseHealUsed,Is.EqualTo(n));Assert.That(ally.Hp,Is.EqualTo(System.Math.Min(40,before+14)));Assert.That(ally.Armor,Is.EqualTo(5));Assert.That(west.Members.Select(c=>c.CharacterId),Is.EqualTo(new[]{"same-healer","same-wounded"}));
            }
        }
        [Test] public void TrainingPromotionItselfKeepsIdentityXpAndCurrentPools()
        {
            var c=new PersistentCharacter("veteran",UnitProfile.FireMageTI,hp:7,personalXp:20);c.TrainMageTierII();
            Assert.That(c.Profile,Is.EqualTo(UnitProfile.FireMageTII));Assert.That(c.Hp,Is.EqualTo(7));Assert.That(c.Armor,Is.Zero);Assert.That(c.PersonalXp,Is.EqualTo(20));Assert.That(c.CharacterId,Is.EqualTo("veteran"));
        }

        [Test] public void EveryKitActionHasLegalAiCandidatesWithoutStateOrRngMutation()
        {
            foreach(var profile in new[]{UnitProfile.FireMageTII,UnitProfile.IceMageTII,UnitProfile.HumanHealerTI}) {
                var s=Setup(profile);s.FindUnit(Actor).Hp-=5;string hash=BattleStateHash.Compute(s);
                var candidates=SpellAi.Candidates(s,s.FindUnit(Actor)).ToArray();
                foreach(var spell in SpellRules.Kit(profile))Assert.That(candidates.Any(c=>c.Spell==spell),Is.True,spell.ToString());
                foreach(var c in candidates){Assert.That(BattleResolver.Validate(s,c),Is.EqualTo(CommandError.None));SpellAi.Value(s,c);}
                Assert.That(BattleStateHash.Compute(s),Is.EqualTo(hash));
            }
        }
        [Test] public void ExhaustionAllowsOrdinaryStaffDefendAndSpellButBlocksExertion()
        {
            var s=Setup(UnitProfile.IceMageTII);s.FindUnit(Target).Position=new GridPosition(9,8);
            s=Apply(s,new CastCommand(Actor,SpellId.IceShield,new GridPosition(8,8)));
            s=BattleTestFixtures.ToActor(Apply(s,new EndActivationCommand(Actor)),Actor);
            Assert.That(s.FindUnit(Actor).IsExhausted,Is.True);
            Assert.That(BattleResolver.Validate(s,new BasicAttackCommand(Actor,Target)),Is.EqualTo(CommandError.None));
            Assert.That(BattleResolver.Validate(s,new DefendCommand(Actor)),Is.EqualTo(CommandError.None));
            // Keep the Exhausted assertion independent of the new Engagement blocker.
            s.FindUnit(Target).Position=new GridPosition(10,8);
            Assert.That(BattleResolver.Validate(s,new CastCommand(Actor,SpellId.IceShard,new GridPosition(10,8))),Is.EqualTo(CommandError.None));
            Assert.That(BattleResolver.Validate(s,new CastCommand(Actor,SpellId.Freeze,new GridPosition(10,8))),Is.EqualTo(CommandError.Exhausted));
        }
        [Test] public void GracefulExitAfterMissIsConsumedAndSecondExitCanProvoke()
        {
            BattleState missed=null;
            for(uint seed=1;seed<3000&&missed==null;seed++){
                var s=Setup(UnitProfile.ElfWarriorTII,seed);s.FindUnit(Target).Position=new GridPosition(9,8);
                var r=BattleResolver.Apply(s,new BasicAttackCommand(Actor,Target));
                if(r.Events.Count(e=>e.Kind==BattleEventKind.AttackMissed)==2)missed=r.State;
            }
            Assert.That(missed,Is.Not.Null);Assert.That(missed.FindUnit(Actor).GracefulExitTarget,Is.EqualTo(Target));
            var first=BattleResolver.Apply(missed,new MoveCommand(Actor,new[]{new GridPosition(7,8)}));Assert.That(first.IsApplied,Is.True);
            Assert.That(first.Events.Any(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.False);
            var back=Apply(first.State,new MoveCommand(Actor,new[]{new GridPosition(8,8)}));
            var second=BattleResolver.Apply(back,new MoveCommand(Actor,new[]{new GridPosition(7,8)}));Assert.That(second.IsApplied,Is.True);
            Assert.That(second.Events.Any(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered&&e.Actor==Target),Is.True);
        }
    }
}
