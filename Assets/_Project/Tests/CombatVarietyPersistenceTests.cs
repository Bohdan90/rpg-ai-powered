using System.Linq;
using NUnit.Framework;
using RPG.Core;
namespace RPG.Tests
{
    public class CombatVarietyPersistenceTests
    {
        private static BattleState EscapeCaster(PersistentBattle battle)
        {
            var s=BattleTestFixtures.ToActor(battle.State,new UnitId(1));
            var cast=new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(11,8));
            Assert.That(BattleResolver.Validate(s,cast),Is.EqualTo(CommandError.None));s=BattleResolver.Apply(s,cast).State;
            for(int i=0;i<30&&!s.Outcome.IsEnded;i++) {
                var u=s.FindUnit(s.CurrentUnitId.Value);
                if(u.Id.Value==1&&u.ActionAvailable&&u.MovementRemaining>0){int x=System.Math.Max(0,u.Position.X-u.MovementRemaining);var path=Pathfinder.FindPath(s,u.Id,new GridPosition(x,8));Assert.That(path.Found,Is.True);s=BattleResolver.Apply(s,new MoveCommand(u.Id,path.Steps)).State;}
                else s=BattleResolver.Apply(s,new EndActivationCommand(u.Id)).State;
            }
            Assert.That(s.Outcome.Reason,Is.EqualTo(BattleEndReason.Withdrawal));return s;
        }
        [Test] public void TwoActualBattlesNoRefreshCarryIdentityDamageAndSourceUseWithoutTemporaryEffects()
        {
            var mage=new PersistentCharacter("same-mage",UnitProfile.FireMageTII,true,personalXp:20);
            var target=new PersistentCharacter("same-warrior",UnitProfile.HumanWarriorTI,true);
            var west=new PersistentFormation("west",Side.West,new[]{mage});var east=new PersistentFormation("east",Side.East,new[]{target});
            var positions=new[]{new PersistentDeployment(mage.CharacterId,new UnitId(1),new GridPosition(8,8),Facing.East),new PersistentDeployment(target.CharacterId,new UnitId(2),new GridPosition(11,8),Facing.West)};
            int hp=target.Hp;
            for(int n=1;n<=2;n++) {
                var battle=PersistentBattle.Start(west,east,positions,2u,new Battlefield(23,17));
                Assert.That(battle.State.FindUnit(new UnitId(2)).Hp,Is.EqualTo(hp));Assert.That(battle.State.FindUnit(new UnitId(1)).IsExhausted,Is.False);
                battle.Resolve(EscapeCaster(battle));Assert.That(mage.FireballUsed,Is.EqualTo(n));Assert.That(mage.Status,Is.EqualTo(PersistentCharacterStatus.EscapedSafe));Assert.That(target.Armor,Is.EqualTo(16));Assert.That(target.Hp,Is.LessThan(hp));hp=target.Hp;
            }
            var third=PersistentBattle.Start(west,east,positions,99,new Battlefield(23,17));var state=BattleTestFixtures.ToActor(third.State,new UnitId(1));
            Assert.That(BattleResolver.Validate(state,new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(11,8))),Is.EqualTo(CommandError.SourceBudgetSpent));
        }
        [Test] public void DualHitStopsAfterLethalFirstAndOaRemainsSingleHit()
        {
            var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanArcherTI,Side.East,9,8,hp:1,armor:0)},2,new Battlefield(23,17)).State;
            s=BattleTestFixtures.ToActor(s,new UnitId(1));var r=BattleResolver.Apply(s,new BasicAttackCommand(new UnitId(1),new UnitId(2)));
            Assert.That(r.State.FindUnit(new UnitId(2)).Status,Is.EqualTo(UnitStatus.Dead));Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.EqualTo(1));
            s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.ElfWarriorTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,9,8)},2,new Battlefield(23,17)).State;
            s=BattleTestFixtures.ToActor(s,new UnitId(2));r=BattleResolver.Apply(s,new MoveCommand(new UnitId(2),new[]{new GridPosition(10,8)}));Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.OpportunityAttackTriggered),Is.EqualTo(1));Assert.That(r.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.EqualTo(1));
        }
        [Test] public void ShieldReplacementClearsFireRetaliationAndDoesNotStack()
        {
            var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.IceMageTI,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.FireMageTI,Side.West,9,8),BattleTestFixtures.Unit(3,UnitProfile.HumanWarriorTI,Side.East,12,8)},2,new Battlefield(23,17)).State;
            s=BattleTestFixtures.ToActor(s,new UnitId(2));s=BattleResolver.Apply(s,new CastCommand(new UnitId(2),SpellId.FireArmor,new GridPosition(9,8))).State;
            s=BattleTestFixtures.ToActor(BattleResolver.Apply(s,new EndActivationCommand(new UnitId(2))).State,new UnitId(1));s=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.IceShield,new GridPosition(9,8))).State;
            var target=s.FindUnit(new UnitId(2));Assert.That(target.TemporaryBarrier,Is.EqualTo(10));Assert.That(target.FireProtection,Is.False);
        }
        [Test] public void FreezeMissStillCommitsBudgetAndExertionWithoutSecondRoll()
        {
            BattleResult miss=null;
            for(uint seed=1;seed<100&&miss==null;seed++) {
                var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.IceMageTII,x:8,y:8),BattleTestFixtures.Unit(2,UnitProfile.ElfWarriorTI,Side.East,10,8)},seed,new Battlefield(23,17)).State;
                s=BattleTestFixtures.ToActor(s,new UnitId(1));var r=BattleResolver.Apply(s,new CastCommand(new UnitId(1),SpellId.Freeze,new GridPosition(10,8)));if(r.Events.Any(e=>e.Kind==BattleEventKind.AttackMissed))miss=r;
            }
            Assert.That(miss,Is.Not.Null);Assert.That(miss.State.FindUnit(new UnitId(1)).FreezeUsed,Is.EqualTo(1));Assert.That(miss.State.FindUnit(new UnitId(1)).IsExhausted,Is.True);Assert.That(miss.Events.Count(e=>e.Kind==BattleEventKind.ContactRolled),Is.EqualTo(1));Assert.That(miss.Events.Any(e=>e.Kind==BattleEventKind.GuardRolled),Is.False);
        }

        [Test] public void ConfirmedAreaCanCauseActualMutualEliminationAndReplay()
        {
            var s=BattleResolver.StartBattle(new[]{BattleTestFixtures.Unit(1,UnitProfile.FireMageTII,x:8,y:8,hp:1),BattleTestFixtures.Unit(2,UnitProfile.HumanWarriorTI,Side.East,10,8,hp:1)},2,new Battlefield(23,17)).State;
            s=BattleTestFixtures.ToActor(s,new UnitId(1));var j=new BattleJournal(s,"mutual-area","test");
            Assert.That(j.Apply(new CastCommand(new UnitId(1),SpellId.Fireball,new GridPosition(9,8),true)).IsApplied,Is.True);
            Assert.That(j.State.Outcome.Reason,Is.EqualTo(BattleEndReason.MutualElimination));Assert.That(j.State.Outcome.VictorySide,Is.Null);
            var check=ReplayVerification.Verify(j.Header,j.Records,j.Footer());Assert.That(check.Matches,Is.True,check.Message);
        }
    }
}
