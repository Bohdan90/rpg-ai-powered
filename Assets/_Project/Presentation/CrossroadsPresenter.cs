using System;
using System.IO;
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
        public void StartDuel(Side first=Side.West)=>ShowDuel(new CrossroadsScenario(first));
        private void ShowDuel(CrossroadsScenario s)
        {
            World=null;loadedEncounter=null;worldHud?.Root.RemoveFromHierarchy();persistence=null;PlayerVsAi=false;
            Duel=s;loadedDuel=null;duelHud?.Root.RemoveFromHierarchy();duelHud=new CrossroadsHud(hud.Root,this);DuelChanged();
        }
        public void DuelChanged()
        {
            if(Duel==null)return;
            if(Duel.Encounter!=null&&Duel.Encounter!=loadedDuel)
            {
                loadedDuel=Duel.Encounter;Fixture=SizeExperimentMap.Field_23x17_Full_9v9;State=loadedDuel.Battle.State;
                PlayerVsAi=false;LastAttackOutcome="";Journal=new BattleJournal(State,"Crossroads_Hotseat",Application.version+" / Unity "+Application.unityVersion,"West human","East human");
                log.Clear();Message="Crossroads · "+loadedDuel.Attacker+" attacks. Tactical Hotseat; same persistent IDs.";
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
                string full=Path.GetFullPath(path??DuelSlot);Directory.CreateDirectory(Path.GetDirectoryName(full));temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
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
                var file=new FileInfo(path??DuelSlot);if(!file.Exists||file.Length==0||file.Length>1024*1024)throw new InvalidDataException("Missing/invalid Crossroads save.");
                var data=JsonUtility.FromJson<CrossroadsSaveData>(File.ReadAllText(file.FullName));
                if(data==null)throw new InvalidDataException("Missing save data.");var candidate=data.Restore();
                DuelSaveMessage="Loaded Crossroads R"+candidate.Refresh;ShowDuel(candidate);return true;
            }
            catch(Exception e){DuelSaveMessage="Load failed; current session unchanged: "+e.Message;duelHud?.Refresh();return false;}
        }
    }
}
