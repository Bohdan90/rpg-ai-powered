using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RPG.Core
{
    [Serializable] public sealed class ReplayCell
    {
        public int x,y;
        public ReplayCell() { }
        public ReplayCell(GridPosition p) { x=p.X;y=p.Y; }
        public GridPosition Position()=>new GridPosition(x,y);
    }
    [Serializable] public sealed class ReplayUnit
    {
        public int initiative; // Zero/absent retains pre-08B authored Initiative.
        public int retreatEdge=-1;
        public int id,side,profile,x,y,facing,hp,armor,status,movement,spent;
        public bool action,oa,defending,postSpellMovement;
        public uint tie;
        public int TemporaryBarrier,BarrierActivations,BurnStacks,BurnTicks,PoisonStacks,BleedStacks,FrozenActivations,ExhaustedActivations,FireballUsed,FreezeUsed,CloseHealUsed;
        public bool silenced;
        public int packageBarrier;
        public bool fireProtection; public int exitTarget;
        public static ReplayUnit Capture(UnitState u)=>new ReplayUnit { initiative=u.Profile.Initiative,retreatEdge=u.OwnRetreatEdge.HasValue?(int)u.OwnRetreatEdge.Value:-1,id=u.Id.Value,side=(int)u.Side,profile=(int)u.Profile.Id,
            x=u.Position.X,y=u.Position.Y,facing=(int)u.Facing,hp=u.Hp,armor=u.Armor,status=(int)u.Status,
            movement=u.MovementRemaining,spent=u.MovementSpentThisActivation,action=u.ActionAvailable,oa=u.OpportunityAttackAvailable,
            postSpellMovement=u.PostSpellMovement,defending=u.IsDefending,tie=u.TieKey,silenced=u.IsSilenced,packageBarrier=u.PackageBarrier,fireProtection=u.FireProtection,exitTarget=u.GracefulExitTarget?.Value??0,
            TemporaryBarrier=u.TemporaryBarrier,BarrierActivations=u.BarrierActivations,BurnStacks=u.BurnStacks,BurnTicks=u.BurnTicks,PoisonStacks=u.PoisonStacks,BleedStacks=u.BleedStacks,FrozenActivations=u.FrozenActivations,ExhaustedActivations=u.ExhaustedActivations,FireballUsed=u.FireballUsed,FreezeUsed=u.FreezeUsed,CloseHealUsed=u.CloseHealUsed };
        internal UnitState Restore()
        {
            var p=UnitProfile.ReplayProfile((UnitProfileId)profile,initiative);
            return new UnitState(new UnitId(id),(Side)side,p,new GridPosition(x,y),(Facing)facing,hp,armor,(UnitStatus)status,retreatEdge<0?(RetreatEdge?)null:(RetreatEdge)retreatEdge) {
                PostSpellMovement=postSpellMovement,MovementRemaining=movement,MovementSpentThisActivation=spent,ActionAvailable=action,OpportunityAttackAvailable=oa,IsDefending=defending,TieKey=tie,IsSilenced=silenced,PackageBarrier=packageBarrier,FireProtection=fireProtection,GracefulExitTarget=exitTarget==0?(UnitId?)null:new UnitId(exitTarget),TemporaryBarrier=TemporaryBarrier,BarrierActivations=BarrierActivations,BurnStacks=BurnStacks,BurnTicks=BurnTicks,PoisonStacks=PoisonStacks,BleedStacks=BleedStacks,FrozenActivations=FrozenActivations,ExhaustedActivations=ExhaustedActivations,FireballUsed=FireballUsed,FreezeUsed=FreezeUsed,CloseHealUsed=CloseHealUsed };
        }
    }
    [Serializable] public sealed class ReplaySnapshot
    {
        public int fireRulesVersion; // Absent in v1 recordings => legacy rules, never latest.
        public int columns,rows,round,currentActor,priorityIndex,winner=-1,loser=-1,outcome;
        public uint seed,rng;
        public bool eastPerimeter;
        public ReplayCell[] solids;
        public int[] priority;
        public ReplayUnit[] units;
        public static ReplaySnapshot Capture(BattleState s)=>new ReplaySnapshot {
            fireRulesVersion=s.FireRulesVersion,columns=s.Battlefield.Columns,rows=s.Battlefield.Rows,eastPerimeter=s.Battlefield.EastRetreatUsesPerimeter,
            solids=s.Battlefield.SolidCells.OrderBy(p=>p.X).ThenBy(p=>p.Y).Select(p=>new ReplayCell(p)).ToArray(),
            seed=s.InitialSeed,rng=s.RngState,round=s.Round,currentActor=s.CurrentUnitId?.Value??0,priorityIndex=s.PriorityIndex,
            priority=s.PriorityOrder.Select(id=>id.Value).ToArray(),units=s.Units.Select(ReplayUnit.Capture).ToArray(),
            winner=s.Outcome.VictorySide.HasValue?(int)s.Outcome.VictorySide.Value:-1,
            loser=s.Outcome.DefeatedSide.HasValue?(int)s.Outcome.DefeatedSide.Value:-1,outcome=(int)s.Outcome.Reason };
        public BattleState Restore()
        {
            if(fireRulesVersion<3&&units.Any(u=>u.postSpellMovement))throw new InvalidDataException("Post-cast movement requires rules3");
            if(fireRulesVersion<0||fireRulesVersion>4)throw new InvalidDataException("Unsupported Fire rules version");
            var s=new BattleState(units.Select(u=>u.Restore()),seed,new Battlefield(columns,rows,solids.Select(p=>p.Position()),eastPerimeter),completedMutualElimination:outcome==(int)BattleEndReason.MutualElimination);
            s.FireRulesVersion=fireRulesVersion==0?1:fireRulesVersion;
            // Constructor seeds priority for fresh battles; replay restores the explicit initial snapshot.
            foreach(var data in units) { var u=s.FindUnit(new UnitId(data.id));u.TieKey=data.tie;u.OpportunityAttackAvailable=data.oa; }
            s.Random=new CombatRandom(rng);s.Round=round;s.CurrentUnitId=currentActor==0?(UnitId?)null:new UnitId(currentActor);
            s.PriorityIndex=priorityIndex;s.PriorityOrder.Clear();s.PriorityOrder.AddRange(priority.Select(id=>new UnitId(id)));
            s.Outcome=outcome==0?BattleOutcome.Ongoing:outcome==(int)BattleEndReason.MutualElimination?BattleOutcome.Draw:new BattleOutcome((Side)winner,(Side)loser,(BattleEndReason)outcome);return s;
        }
    }
    public static class BattleStateHash
    {
        public static string Compute(BattleState state)
        {
            var s=ReplaySnapshot.Capture(state);
            using(var stream=new MemoryStream())
            using(var w=new BinaryWriter(stream))
            {
                w.Write(2);w.Write(s.columns);w.Write(s.rows);w.Write(s.eastPerimeter);w.Write(s.solids.Length);
                foreach(var p in s.solids){w.Write(p.x);w.Write(p.y);}
                w.Write(s.seed);w.Write(s.rng);w.Write(s.round);w.Write(s.currentActor);w.Write(s.priorityIndex);
                w.Write(s.winner);w.Write(s.loser);w.Write(s.outcome);w.Write(s.priority.Length);foreach(var id in s.priority)w.Write(id);
                w.Write(s.units.Length);
                foreach(var u in s.units.OrderBy(u=>u.id))
                {
                    foreach(int v in new[]{u.id,u.side,u.profile,u.x,u.y,u.facing,u.hp,u.armor,u.status,u.movement,u.spent,u.retreatEdge})w.Write(v);
                    w.Write(u.action);w.Write(u.oa);w.Write(u.defending);w.Write(u.tie);
                    if(state.Units.Any(v=>v.Profile.IsCaster||v.Profile.HasGracefulExit)) {
                        w.Write(u.TemporaryBarrier);w.Write(u.BarrierActivations);w.Write(u.BurnStacks);w.Write(u.BurnTicks);w.Write(u.PoisonStacks);w.Write(u.BleedStacks);w.Write(u.FrozenActivations);w.Write(u.ExhaustedActivations);w.Write(u.FireballUsed);w.Write(u.FreezeUsed);w.Write(u.CloseHealUsed);w.Write(u.fireProtection);w.Write(u.exitTarget);
                    }
                    var p=state.FindUnit(new UnitId(u.id)).Profile;
                    foreach(int v in new[]{p.MaxHp,p.MaxArmor,p.Movement,p.Initiative,p.Accuracy,p.Dodge,p.Guard,p.BasicDamage,p.Range,p.FrontalEvasion,p.CoverSize})w.Write(v);
                }
                // Optional extension preserves existing no-Silence replay hashes byte-for-byte.
                if(s.units.Any(u=>u.silenced)){w.Write("Silence");foreach(var u in s.units.Where(u=>u.silenced).OrderBy(u=>u.id))w.Write(u.id);}
                if(state.FireRulesVersion>=2){w.Write("FireTargeting02");w.Write(state.FireRulesVersion);foreach(var u in s.units.OrderBy(u=>u.id))w.Write(u.packageBarrier);}
                if(state.FireRulesVersion>=3){w.Write("TacticalInput03");foreach(var u in s.units.OrderBy(u=>u.id))w.Write(u.postSpellMovement);}
                w.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }
    }
    [Serializable] public sealed class ReplayCommand
    {
        public string kind;
        public int actor,target,attackKind,facing,spell,cx,cy;
        public bool friendly,hasFacing;
        public ReplayCell[] path;
        public static ReplayCommand Capture(BattleCommand c,BattleState state=null)
        {
            var d=new ReplayCommand{kind=c?.GetType().Name??"Null",actor=c?.Actor.Value??0};
            if(c is BasicAttackCommand a){d.target=a.Target.Value;d.attackKind=(int)a.Kind;d.friendly=a.FriendlyFireConfirmed;}
            if(c is CastCommand cast){d.spell=(int)cast.Spell;d.cx=cast.Cell.X;d.cy=cast.Cell.Y;d.friendly=cast.FriendlyFireConfirmed;if(state?.FireRulesVersion>=2&&SpellRules.Helpful(cast.Spell))d.target=state.OccupantAt(cast.Cell)?.Id.Value??0;}
            if(c is MoveCommand m)d.path=m.Path?.Select(p=>new ReplayCell(p)).ToArray();
            if(c is EndActivationCommand e){d.hasFacing=e.FinalFacing.HasValue;d.facing=(int)(e.FinalFacing??Facing.North);}return d;
        }
        public BattleCommand Restore()
        {
            if(kind=="Null")return null;
            var id=new UnitId(actor);
            switch(kind){case nameof(BasicAttackCommand):return new BasicAttackCommand(id,new UnitId(target),friendly,(BasicAttackKind)attackKind);
                case nameof(CastCommand):return new CastCommand(id,(SpellId)spell,new GridPosition(cx,cy),friendly);
                case nameof(MoveCommand):return new MoveCommand(id,path?.Select(p=>p.Position()));
                case nameof(DefendCommand):return new DefendCommand(id);
                case nameof(EndActivationCommand):return new EndActivationCommand(id,hasFacing?(Facing?)facing:null);
                default:throw new InvalidDataException("Unknown command "+kind);}
        }
    }
    [Serializable] public sealed class ReplayEvent
    {
        public string kind;
        public int round,actor,target,amount,before,after,chance,roll,outcome,winner=-1,loser=-1;
        public ReplayCell from,to;
        public bool hasFrom,hasTo;
        public static ReplayEvent Capture(BattleEvent e)=>new ReplayEvent{kind=e.Kind.ToString(),round=e.Round,
            actor=e.Actor?.Value??0,target=e.Target?.Value??0,amount=e.Amount,before=e.Before,after=e.After,chance=e.ChancePercent,roll=e.Roll,
            hasFrom=e.From.HasValue,hasTo=e.To.HasValue,from=e.From.HasValue?new ReplayCell(e.From.Value):null,to=e.To.HasValue?new ReplayCell(e.To.Value):null,
            outcome=(int)(e.Outcome?.Reason??BattleEndReason.None),winner=e.Outcome?.VictorySide.HasValue==true?(int)e.Outcome.Value.VictorySide.Value:-1,
            loser=e.Outcome?.DefeatedSide.HasValue==true?(int)e.Outcome.Value.DefeatedSide.Value:-1};
    }
    [Serializable] public sealed class ReplayChange { public ReplayUnit before,after;public int hpDelta,armorDelta; }
    [Serializable] public sealed class ReplayHeader
    {
        public const string LegacyConfig="GateC-v0.1-HA10-fallback5-AI1-directional-retreat-51-spells1";
        public const string Input03Config="GateC-v0.1-51-spells3-tacticalinput03";
        public const string EngagementConfig="GateC-v0.1-51-spells4-engagementcasting";
        public static string ConfigFor(int rules)=>rules==1?LegacyConfig:rules==2?CurrentConfig:rules==3?Input03Config:EngagementConfig;
        public const string CurrentConfig="GateC-v0.1-HA10-fallback5-AI1-directional-retreat-51-spells2-firetargeting02";
        public string type="header",configVersion=CurrentConfig,buildVersion,fixture,westController,eastController,initialHash;
        public int formatVersion=2;
        public uint seed;
        public ReplaySnapshot initial;
    }
    [Serializable] public sealed class ReplayRecord
    {
        public string type="command",controller,error,stateHash,aiDecision;
        public int sequence,successfulSequence,round,activeActor;
        public int contactChance=-1,guardChance=-1;
        public bool applied;
        public uint rngBefore,rngAfter;
        public ReplayCommand command;
        public ReplayEvent[] events;
        public ReplayChange[] changes;
    }
    [Serializable] public sealed class ReplayFooter
    {
        public string type="footer",finalHash;
        public int attempts,successfulCommands;
        public ReplaySnapshot final;
    }
    [Serializable] public sealed class SessionTelemetry
    {
        public string type="session",fixture;
        public int rounds,activations,attacks,oaExits,oaTriggers,defends,escapes,deaths,invalidAttempts,cancelledPreviews,attacksBeforeFirstHpLoss;
        public bool firstHpLossObserved;
        internal void Observe(IEnumerable<BattleEvent> events)
        {
            foreach(var e in events)
            {
                switch(e.Kind) {
                    case BattleEventKind.ActivationStarted:activations++;break;
                    case BattleEventKind.ContactRolled:attacks++;if(!firstHpLossObserved)attacksBeforeFirstHpLoss++;break;
                    case BattleEventKind.HpLost:if(!firstHpLossObserved){firstHpLossObserved=true;attacksBeforeFirstHpLoss=Math.Max(0,attacksBeforeFirstHpLoss-1);}break;
                    case BattleEventKind.ZoCExitDetected:oaExits++;break;case BattleEventKind.OpportunityAttackTriggered:oaTriggers++;break;
                    case BattleEventKind.DefendApplied:defends++;break;case BattleEventKind.UnitEscaped:escapes++;break;case BattleEventKind.UnitDied:deaths++;break;
                }
            }
        }
    }
    public sealed class BattleJournal
    {
        public ReplayHeader Header { get; }
        private readonly List<ReplayRecord> records=new List<ReplayRecord>();
        public IReadOnlyList<ReplayRecord> Records=>records.AsReadOnly();
        public BattleState State { get; private set; }
        public SessionTelemetry Session { get; }
        private int successful;
        public BattleJournal(BattleState initial,string fixture,string build,string west="Player",string east="Player")
        {
            State=initial;Header=new ReplayHeader{configVersion=ReplayHeader.ConfigFor(initial.FireRulesVersion),fixture=fixture,buildVersion=build,westController=west,eastController=east,
                seed=initial.InitialSeed,initial=ReplaySnapshot.Capture(initial),initialHash=BattleStateHash.Compute(initial)};
            Session=new SessionTelemetry{fixture=fixture,rounds=initial.Round,activations=initial.CurrentUnitId.HasValue?1:0};
        }
        public BattleResult Apply(BattleCommand command,string controller="Player",string aiDecision=null)
        {
            var before=State;var result=BattleResolver.Apply(before,command);State=result.State;
            if(result.IsApplied)successful++;else Session.invalidAttempts++;
            var record=new ReplayRecord{sequence=records.Count+1,successfulSequence=result.IsApplied?successful:0,round=before.Round,
                activeActor=before.CurrentUnitId?.Value??0,controller=controller,aiDecision=aiDecision,command=ReplayCommand.Capture(command,before),
                applied=result.IsApplied,error=result.Error.ToString(),rngBefore=before.RngState,rngAfter=State.RngState,
                stateHash=BattleStateHash.Compute(State),events=result.Events.Select(ReplayEvent.Capture).ToArray(),
                changes=before.Units.Select(u=>new ReplayChange{before=ReplayUnit.Capture(u),after=ReplayUnit.Capture(State.FindUnit(u.Id)),
                    hpDelta=State.FindUnit(u.Id).Hp-u.Hp,armorDelta=State.FindUnit(u.Id).Armor-u.Armor}).ToArray()};
            if(command is BasicAttackCommand attack)
            {
                var preview=BattleResolver.PreviewAttack(before,attack);
                if(preview.IsLegal){record.contactChance=preview.ContactChance;record.guardChance=preview.GuardChance;}
            }
            records.Add(record);Session.rounds=State.Round;Session.Observe(result.Events);return result;
        }
        public ReplayFooter Footer()=>new ReplayFooter{attempts=records.Count,successfulCommands=successful,
            finalHash=BattleStateHash.Compute(State),final=ReplaySnapshot.Capture(State)};
    }
    public sealed class ReplayVerification
    {
        public bool Matches { get; internal set; }
        public int DivergentSequence { get; internal set; }
        public string Message { get; internal set; }
        public BattleState State { get; internal set; }
        public static ReplayVerification Failure(string message)=>new ReplayVerification { Message=message };
        public static ReplayVerification Verify(ReplayHeader header,IEnumerable<ReplayRecord> records,ReplayFooter footer)
        {
            var result=new ReplayVerification();int sequence=0,successful=0;
            try
            {
                if(header.formatVersion!=2||(header.configVersion!=ReplayHeader.CurrentConfig&&header.configVersion!=ReplayHeader.LegacyConfig&&header.configVersion!=ReplayHeader.Input03Config&&header.configVersion!=ReplayHeader.EngagementConfig))throw new InvalidDataException("Unsupported replay/config version");
                var state=header.initial.Restore();result.State=state;
                if(header.configVersion!=ReplayHeader.ConfigFor(state.FireRulesVersion))throw new InvalidDataException("Rules/config mismatch");
                if(header.seed!=state.InitialSeed||BattleStateHash.Compute(state)!=header.initialHash)throw new InvalidDataException("Initial state/config hash mismatch");
                foreach(var record in records)
                {
                    sequence++;result.DivergentSequence=sequence;
                    if(record.sequence!=sequence||record.round!=state.Round||record.activeActor!=(state.CurrentUnitId?.Value??0)||record.rngBefore!=state.RngState)
                        throw new InvalidDataException("Command sequence/before-state mismatch");
                    if(state.FireRulesVersion>=2&&record.command.kind==nameof(CastCommand)&&SpellRules.Helpful((SpellId)record.command.spell)
                        &&record.command.target!=(state.OccupantAt(new GridPosition(record.command.cx,record.command.cy))?.Id.Value??0))throw new InvalidDataException("Effect recipient mismatch");
                    var applied=BattleResolver.Apply(state,record.command.Restore());
                    if(applied.IsApplied)successful++;
                    if(applied.IsApplied!=record.applied||applied.Error.ToString()!=record.error||record.successfulSequence!=(applied.IsApplied?successful:0))
                        throw new InvalidDataException("Command legality mismatch");
                    state=applied.State;result.State=state;
                    if(state.RngState!=record.rngAfter||BattleStateHash.Compute(state)!=record.stateHash)throw new InvalidDataException("State/RNG hash mismatch");
                    // Recorded rolls are diagnostics only; resolver regenerated every roll above.
                }
                result.DivergentSequence=sequence+1;
                if(footer.attempts!=sequence||footer.successfulCommands!=successful||footer.finalHash!=BattleStateHash.Compute(state)
                    ||BattleStateHash.Compute(footer.final.Restore())!=footer.finalHash)throw new InvalidDataException("Final state/footer mismatch");
                result.Matches=true;result.DivergentSequence=0;result.Message="Replay matches: "+successful+" successful commands, "+sequence+" attempts.";
            }
            catch(Exception e){result.Message="Replay divergence at sequence "+result.DivergentSequence+": "+e.Message;}
            return result;
        }
    }
}
