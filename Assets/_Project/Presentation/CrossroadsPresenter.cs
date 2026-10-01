using System;
using System.IO;
using System.Linq;
using RPG.Core;
using UnityEngine;

namespace RPG.Presentation
{
    public sealed partial class BattlePresenter
    {
        public CrossroadsScenario Duel { get; private set; }
        private CrossroadsHud duelHud;
        private DuelEncounter loadedDuel;
        public string DuelSaveMessage { get; private set; }="Stable map only · one Crossroads slot.";
        public static string DuelSlot=>Path.Combine(Application.persistentDataPath,"Crossroads","manual.json");
        public static string IncidentSlot=>Path.Combine(Application.persistentDataPath,"CrossroadsIncident04","manual.json");
        public static string CitySlot(bool combined)=>Path.Combine(Application.persistentDataPath,combined?"CityCombat05B":"CityFoundations05A","manual.json");
        public void StartCity(bool combined=false,CombatPreset west=CombatPreset.Fire,CombatPreset east=CombatPreset.Ice,Side first=Side.West)=>ShowDuel(new CrossroadsScenario(first,foundations:true,combined:combined,westPreset:west,eastPreset:east));
        public static string SeamlessSlot=>Path.Combine(Application.persistentDataPath,"SeamlessWorlds07","manual.json");
        public SeamlessMapView SeamlessMap=>duelHud?.SeamlessView;
        public void StartSeamless(Side first=Side.West,bool temporary=false,CombatPreset west=CombatPreset.Fire,CombatPreset east=CombatPreset.Ice)=>ShowDuel(SeamlessWorlds.Create(first,temporary,west,east));
        public void StartSeamlessOpaqueProbe()=>ShowDuel(SeamlessWorlds.CreateOpaqueProbe());
        public static string RealmSlot=>Path.Combine(Application.persistentDataPath,"RealmOperations06","manual.json");
        public void StartRealm(Side first=Side.West,CombatPreset west=CombatPreset.Fire,CombatPreset east=CombatPreset.Ice)=>ShowDuel(new CrossroadsScenario(first,realm:true,westPreset:west,eastPreset:east));
        private string CurrentDuelSlot=>Duel?.Seamless!=null?SeamlessSlot:Duel?.Realm!=null?RealmSlot:Duel?.Foundations!=null?CitySlot(Duel.Foundations.Combined):Duel?.Incident!=null?IncidentSlot:DuelSlot;
        public void StartIncident(Side first=Side.West,bool enabled=true)=>ShowDuel(new CrossroadsScenario(first,incident:true,incidentsEnabled:enabled));
        public bool LoadIncident()=>LoadDuel(IncidentSlot);
        public void StartDuel(Side first=Side.West,bool economy=true)=>ShowDuel(new CrossroadsScenario(first,economy:economy));
        private void ShowDuel(CrossroadsScenario s)
        {
            World=null;loadedEncounter=null;worldHud?.Root.RemoveFromHierarchy();persistence=null;PlayerVsAi=false;
            Duel=s;loadedDuel=null;duelHud?.Root.RemoveFromHierarchy();duelHud=new CrossroadsHud(hud.Root,this);DuelChanged();
        }
        public string TacticalSideLabel(Side? side)
        {
            if(!side.HasValue)return "None";var e=Duel?.Encounter;if(e==null||e.Participants==null)return side.ToString();
            return string.Join(" + ",e.Participants.Where(f=>e.TacticalSides[f.Formation.FormationId]==side).Select(f=>Duel.Realm!=null?f.Formation.FormationId:f==Duel.West?"West human":f==Duel.East?"East human":f.Formation.FormationId.EndsWith("A")?"Raider A AI":"Raider B AI"));
        }
        public void DuelChanged()
        {
            if(Duel==null)return;
            if(Duel.Encounter!=null&&Duel.Encounter!=loadedDuel)
            {
                loadedDuel=Duel.Encounter;Fixture=SizeExperimentMap.Field_23x17_Full_9v9;State=loadedDuel.Battle.State;
                PlayerVsAi=loadedDuel.HasRaiders;AiSide=loadedDuel.AiSide;LastAttackOutcome="";Journal=new BattleJournal(State,"Crossroads_Hotseat",Application.version+" / Unity "+Application.unityVersion,PlayerVsAi&&AiSide==Side.West?"Raider AI":"Human",PlayerVsAi&&AiSide==Side.East?"Raider AI":"Human");
                log.Clear();Message=(Duel.Seamless!=null?"Seamless Worlds07 · "+SeamlessWorlds.Name(loadedDuel.WorldId)+" · ":"Crossroads · ")+(Duel.Incident==null?loadedDuel.Attacker.ToString():loadedDuel.Lead.Formation.FormationId)+" attacks. "+(PlayerVsAi?"Human vs raider AI":"Tactical Hotseat")+"; same persistent IDs.";
                grid.Resize(State.Battlefield);hud.Resize(State.Battlefield);FitBoard();ClearPreview();Refresh();
            }
            duelHud.Refresh();
        }
        public bool SaveDuel(string path=null)
        {
            string temp=null;
            try
            {
                if(Duel==null||!Duel.CanSave)throw new InvalidOperationException("Save only from stable Crossroads map.");
                string full=Path.GetFullPath(path??CurrentDuelSlot);Directory.CreateDirectory(Path.GetDirectoryName(full));temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
                var bytes=System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(Duel.CaptureSave(),true));
                if(bytes.Length>1024*1024)throw new InvalidDataException("Save exceeds prototype size limit.");
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);
                DuelSaveMessage="Saved Crossroads R"+Duel.Refresh;duelHud.Refresh();return true;
            }
            catch(Exception e){DuelSaveMessage="Save failed: "+e.Message;duelHud?.Refresh();return false;}
            finally{if(temp!=null&&File.Exists(temp))File.Delete(temp);}
        }
        public bool LoadDuel(string path=null)
        {
            try
            {
                if((Duel!=null&&!Duel.CanSave)||(World!=null&&!World.CanSave))throw new InvalidOperationException("Finish tactical battle first.");
                var file=new FileInfo(path??CurrentDuelSlot);if(!file.Exists||file.Length==0||file.Length>1024*1024)throw new InvalidDataException("Missing/invalid Crossroads save.");
                var data=JsonUtility.FromJson<CrossroadsSaveData>(File.ReadAllText(file.FullName));
                if(data==null)throw new InvalidDataException("Missing save data.");var candidate=data.Restore();
                DuelSaveMessage="Loaded Crossroads R"+candidate.Refresh;ShowDuel(candidate);return true;
            }
            catch(Exception e){DuelSaveMessage="Load failed; current session unchanged: "+e.Message;Message=DuelSaveMessage;Refresh();duelHud?.Refresh();return false;}
        }
    }
}
