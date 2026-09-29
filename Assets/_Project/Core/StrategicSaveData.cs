using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace RPG.Core
{
    // Data-only prototype snapshot. JSON/file transport belongs to Presentation.
    [Serializable] public sealed class StrategicSaveCharacter
    {
        public string id, personalXp, commandXp;
        public int profile, hp, armor, status, personalLevel, commandLevel, commandRank, recoveryRemainder;
        public bool commander;
        internal static StrategicSaveCharacter Capture(PersistentCharacter c) => new StrategicSaveCharacter {
            id=c.CharacterId,profile=(int)c.Profile.Id,hp=c.Hp,armor=c.Armor,status=(int)c.Status,commander=c.IsCommander,
            personalXp=c.PersonalXp.ToString(CultureInfo.InvariantCulture),commandXp=c.CommandXp.ToString(CultureInfo.InvariantCulture),
            personalLevel=c.PersonalLevel,commandLevel=c.CommandLevel,commandRank=(int)c.CommandRank,recoveryRemainder=c.FieldRecoveryRemainderHundredths };
        internal PersistentCharacter Restore(PersistentCharacter expected)
        {
            StrategicSaveData.Require(id==expected.CharacterId && profile==(int)expected.Profile.Id && commander==expected.IsCommander,"Roster identity/profile mismatch.");
            StrategicSaveData.Require(Enum.IsDefined(typeof(PersistentCharacterStatus),status),"Invalid character status.");
            var p=Xp(personalXp);var c=Xp(commandXp);
            StrategicSaveData.Require(commandLevel>=1&&commandLevel<=1+(int)(c/10m),"Invalid Command level.");
            var restored=new PersistentCharacter(id,expected.Profile,commander,hp,armor,(PersistentCharacterStatus)status,p,c,commandLevel,recoveryRemainder);
            StrategicSaveData.Require(restored.PersonalLevel==personalLevel&&restored.CommandLevel==commandLevel&&(int)restored.CommandRank==commandRank,"Derived progression mismatch.");
            StrategicSaveData.Require(commander||c==0,"Non-Commander Command XP.");
            return restored;
        }
        private static decimal Xp(string text)
        {
            StrategicSaveData.Require(decimal.TryParse(text,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value)&&value>=0&&value<=1000000000m,"Invalid XP.");
            return value;
        }
        internal void Write(BinaryWriter w)
        {w.Write(id);w.Write(profile);w.Write(commander);w.Write(hp);w.Write(armor);w.Write(status);w.Write(personalXp);w.Write(personalLevel);w.Write(commandXp);w.Write(commandLevel);w.Write(commandRank);w.Write(recoveryRemainder);}
    }
    [Serializable] public sealed class StrategicSaveFormation
    {
        public string id;
        public int side;
        public bool commanderless, rosterLocked;
        public StrategicSaveCharacter[] members;
        internal static StrategicSaveFormation Capture(PersistentFormation f)=>new StrategicSaveFormation {
            id=f.FormationId,side=(int)f.Side,commanderless=f.Commanderless,rosterLocked=f.RosterLocked,members=f.Members.Select(StrategicSaveCharacter.Capture).ToArray() };
        internal PersistentFormation Restore(PersistentFormation expected)
        {
            StrategicSaveData.Require(id==expected.FormationId&&side==(int)expected.Side&&members!=null&&members.Length==expected.Members.Count&&members.All(c=>c!=null),"Invalid formation.");
            var f=new PersistentFormation(id,(Side)side,members.Select((c,i)=>c.Restore(expected.Members[i])));
            StrategicSaveData.Require(f.Commanderless==commanderless&&f.RosterLocked==rosterLocked,"Commander state mismatch.");return f;
        }
        internal void Write(BinaryWriter w)
        {w.Write(id);w.Write(side);w.Write(commanderless);w.Write(rosterLocked);w.Write(members.Length);foreach(var c in members)c.Write(w);}
    }
    [Serializable] public sealed class StrategicSaveActor
    {
        public int kind, node, tempo, objective, patrolIndex;
        public bool raidArmed, interceptPending;
        public StrategicSaveFormation formation;
        internal static StrategicSaveActor Capture(StrategicActor a)=>new StrategicSaveActor {kind=(int)a.Kind,node=a.Node,tempo=a.Tempo,
            objective=(int)a.Objective,patrolIndex=a.PatrolIndex,raidArmed=a.RaidArmed,interceptPending=a.InterceptPending,formation=StrategicSaveFormation.Capture(a.Formation)};
        internal void Write(BinaryWriter w)
        {w.Write(kind);w.Write(node);w.Write(tempo);w.Write(objective);w.Write(patrolIndex);w.Write(raidArmed);w.Write(interceptPending);formation.Write(w);}
    }
    [Serializable] public sealed class StrategicSaveResolution
    {
        // Strings preserve decimal precision through Unity JSON, which does not serialize decimal.
        public string[] west, east;
        public bool westVictory, eastVictory;
        private static string[] Values(PersistenceSideResolution r)=>new[]{r.Pool,r.PersonalReserve,r.CommandReserve,r.EarnedEnemyWeight}.Select(v=>v.ToString(CultureInfo.InvariantCulture)).ToArray();
        internal static StrategicSaveResolution Capture(PersistenceBattleResolution r)=>new StrategicSaveResolution {west=Values(r.West),east=Values(r.East),westVictory=r.West.Victory,eastVictory=r.East.Victory};
        private static PersistenceSideResolution SideResult(Side side,string[] values,bool won)
        {
            StrategicSaveData.Require(values!=null&&values.Length==4,"Invalid XP resolution.");
            var v=values.Select(s=>decimal.Parse(s,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture)).ToArray();
            StrategicSaveData.Require(v.All(x=>x>=0),"Negative XP resolution.");return new PersistenceSideResolution(side,v[0],v[1],v[2],v[3],won);
        }
        internal PersistenceBattleResolution Restore()=>new PersistenceBattleResolution(SideResult(Side.West,west,westVictory),SideResult(Side.East,east,eastVictory));
        internal void Write(BinaryWriter w){foreach(var s in west)w.Write(s);foreach(var s in east)w.Write(s);w.Write(westVictory);w.Write(eastVictory);}
    }
    [Serializable] public sealed class StrategicSaveData
    {
        public const int CurrentVersion=1;
        public const string Mission="Mission01-WP02-v1";
        public int version,refresh,playerNode,tempo,provisions,maxProvisions,waystationFood,waystation,village,result,actorCursor,battleNumber;
        public uint seed;
        public bool hungry,portalInvestigated,worldPhase,hasLastResolution;
        public string mission,checksum,lastBattleSummary;
        public string[] events;
        public StrategicSaveFormation player;
        public StrategicSaveActor[] actors;
        public StrategicSaveResolution lastResolution;
        internal static void Require(bool condition,string message){if(!condition)throw new InvalidDataException(message);}
        public StrategicScenario Restore()
        {
            Require(version==CurrentVersion&&mission==Mission,"Unsupported save/schema or mission version.");
            Require(!string.IsNullOrEmpty(checksum),"Missing save checksum.");
            // Build and validate an entirely separate session; never write into a live scenario.
            var restored=new StrategicScenario(this);
            Require(string.Equals(checksum,ComputeHash(),StringComparison.Ordinal),"Save checksum mismatch.");
            return restored;
        }
        public string ComputeHash()
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream))
            {
                w.Write(version);w.Write(mission);w.Write(seed);
                foreach(int n in new[]{refresh,playerNode,tempo,provisions,maxProvisions,waystationFood,waystation,village,result,actorCursor,battleNumber})w.Write(n);
                w.Write(hungry);w.Write(portalInvestigated);w.Write(worldPhase);w.Write(lastBattleSummary);
                player.Write(w);w.Write(actors.Length);foreach(var a in actors)a.Write(w);
                w.Write(events.Length);foreach(var e in events)w.Write(e);
                w.Write(hasLastResolution);if(hasLastResolution)lastResolution.Write(w);
                w.Flush();using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }
    }
    public sealed partial class StrategicScenario
    {
        public bool CanSave => Encounter==null&&(!worldPhase||Result!=StrategicMissionResult.Ongoing);
        public StrategicSaveData CaptureSave()
        {
            if(!CanSave)throw new InvalidOperationException("Save only from a stable strategic map, outside battle.");
            var d=new StrategicSaveData {version=StrategicSaveData.CurrentVersion,mission=StrategicSaveData.Mission,seed=Seed,refresh=Refresh,
                playerNode=PlayerNode,tempo=Tempo,provisions=Provisions,maxProvisions=MaxProvisions,waystationFood=WaystationFood,
                waystation=(int)Waystation,village=(int)Village,result=(int)Result,hungry=Hungry,portalInvestigated=PortalInvestigated,
                worldPhase=worldPhase,actorCursor=actorCursor,battleNumber=battleNumber,lastBattleSummary=LastBattleSummary,events=events.ToArray(),
                player=StrategicSaveFormation.Capture(Player),actors=Actors.Select(StrategicSaveActor.Capture).ToArray(),
                hasLastResolution=LastBattleResolution.HasValue,lastResolution=LastBattleResolution.HasValue?StrategicSaveResolution.Capture(LastBattleResolution.Value):null};
            d.checksum=d.ComputeHash();return d;
        }
        internal StrategicScenario(StrategicSaveData d):this(d.seed)
        {
            StrategicSaveData.Require(d.refresh>=1&&d.refresh<=1000000&&d.battleNumber>=0&&d.battleNumber<=1000000,"Invalid turn counters.");
            StrategicSaveData.Require(Graph.Node(d.playerNode)!=null&&d.playerNode!=11&&d.tempo>=-40&&d.tempo<=100,"Invalid player position/Tempo.");
            StrategicSaveData.Require(d.maxProvisions==MaxProvisions,"Save uses a different Provisions tuning. Start a new Mission 01 (30 Provisions); current session unchanged.");
            StrategicSaveData.Require(d.provisions>=0&&d.provisions<=MaxProvisions&&d.waystationFood>=0&&d.waystationFood<=12,"Invalid supply.");
            StrategicSaveData.Require(Enum.IsDefined(typeof(StrategicSiteCondition),d.waystation)&&Enum.IsDefined(typeof(StrategicSiteCondition),d.village)
                &&Enum.IsDefined(typeof(StrategicMissionResult),d.result),"Invalid mission/site state.");
            StrategicSaveData.Require(d.actorCursor>=0&&d.actorCursor<=4&&(!d.worldPhase||d.result!=(int)StrategicMissionResult.Ongoing),"Unstable world phase cannot be loaded.");
            StrategicSaveData.Require(d.player!=null&&d.actors!=null&&d.actors.Length==Actors.Count&&d.actors.All(a=>a!=null&&a.formation!=null),"Incomplete formations.");
            StrategicSaveData.Require(d.events!=null&&d.events.Length<=20000&&d.events.All(e=>e!=null&&e.Length<=4096)&&d.lastBattleSummary!=null,"Invalid event history.");
            Player=d.player.Restore(Player);
            foreach(var a in Actors)
            {
                var saved=d.actors[(int)a.Kind];
                StrategicSaveData.Require(saved.kind==(int)a.Kind&&Graph.Node(saved.node)!=null&&saved.tempo>=-40&&saved.tempo<=100
                    &&Enum.IsDefined(typeof(StrategicObjective),saved.objective)&&saved.patrolIndex>=0&&saved.patrolIndex<4,"Invalid actor state.");
                a.RestoreFormation(saved.formation.Restore(a.Formation));a.Node=saved.node;a.Tempo=saved.tempo;a.Objective=(StrategicObjective)saved.objective;
                a.PatrolIndex=saved.patrolIndex;a.RaidArmed=saved.raidArmed;a.InterceptPending=saved.interceptPending;
            }
            Refresh=d.refresh;PlayerNode=d.playerNode;Tempo=d.tempo;Provisions=d.provisions;WaystationFood=d.waystationFood;
            Waystation=(StrategicSiteCondition)d.waystation;Village=(StrategicSiteCondition)d.village;Result=(StrategicMissionResult)d.result;
            Hungry=d.hungry;PortalInvestigated=d.portalInvestigated;worldPhase=d.worldPhase;actorCursor=d.actorCursor;battleNumber=d.battleNumber;
            StrategicSaveData.Require(!d.hasLastResolution||d.lastResolution!=null,"Missing XP resolution.");
            LastBattleSummary=d.lastBattleSummary;LastBattleResolution=d.hasLastResolution?d.lastResolution.Restore():(PersistenceBattleResolution?)null;events.Clear();events.AddRange(d.events);
        }
    }
}
