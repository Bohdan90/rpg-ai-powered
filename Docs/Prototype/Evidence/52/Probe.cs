using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using RPG.Core;
using RPG.Presentation;
[InitializeOnLoad] public static class RealmProbe
{
    const string Root="/private/tmp/realm-operations-06/";
    static double last;static string mode="m1",previous="",plan="";static GridPosition? target;static string spell="";static bool ff;
    static RealmProbe(){EditorApplication.update+=Tick;}
    public static void Launch(){EditorSceneManager.OpenScene("Assets/_Project/Scenes/TacticalGraybox.unity");EditorApplication.EnterPlaymode();}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup-last<.1)return;last=EditorApplication.timeSinceStartup;
        var p=UnityEngine.Object.FindAnyObjectByType<BattlePresenter>();if(p==null)return;if(p.State==null){EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/TacticalGraybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));return;}
        if(p.HudRoot.userData==null){p.HudRoot.userData="probe";p.HudRoot.RegisterCallback<PointerDownEvent>(e=>File.WriteAllText(Root+"pointer.txt",e.position+" target="+((VisualElement)e.target).name),TrickleDown.TrickleDown);}
        if(File.Exists(Root+"command.txt"))
        {
            string c=File.ReadAllText(Root+"command.txt").Trim();File.Delete(Root+"command.txt");var game=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));game.maximized=true;game.Focus();
            if(c.StartsWith("scroll:")){var selector=c.Substring(7);var e=p.HudRoot.Q(selector);if(selector.StartsWith("text:"))e=p.HudRoot.Query<Button>().ToList().FirstOrDefault(b=>b.text.StartsWith(selector.Substring(5)));if(selector.StartsWith("label:"))e=p.HudRoot.Query<DropdownField>().ToList().FirstOrDefault(b=>b.label==selector.Substring(6));if(selector.StartsWith("toggle:"))e=p.HudRoot.Query<Toggle>().ToList().FirstOrDefault(t=>t.label.StartsWith(selector.Substring(7)));e?.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(e);}
            if(c=="recreate"){EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/TacticalGraybox.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));return;}
            if(c.StartsWith("mode:"))mode=c.Substring(5);
            if(c.StartsWith("cell:")){var xy=c.Substring(5).Split(",");target=new GridPosition(int.Parse(xy[0]),int.Parse(xy[1]));}
            if(c=="plan"&&!p.State.Outcome.IsEnded)
            {
                // Read-only legal suggestion. The driver still clicks the ordinary human controls.
                var a=TacticalAi.Choose(p.State).Command;plan=a.GetType().Name;spell="";ff=false;if(a is CastCommand cast){target=cast.Cell;spell=cast.Spell.ToString();ff=cast.FriendlyFireConfirmed;}
                if(a is MoveCommand m)target=m.Path.Last();if(a is BasicAttackCommand b)target=p.State.FindUnit(b.Target).Position;
            }
            if(c=="escape"&&!p.State.Outcome.IsEnded)
            {
                var u=p.State.FindUnit(p.State.CurrentUnitId.Value);var b=p.State.Battlefield;
                var cells=Enumerable.Range(0,b.Columns*b.Rows).Select(k=>new GridPosition(k%b.Columns,k/b.Columns));
                var exits=cells.Where(g=>b.IsRetreatZone(u,g)).ToArray();
                var paths=cells.Select(g=>new{g,r=Pathfinder.FindPath(p.State,u.Id,g)}).Where(a=>a.r.Found&&a.r.Cost>0&&exits.Length>0).OrderBy(a=>exits.Min(e=>e.DistanceTo(a.g))).ThenBy(a=>a.r.Cost).ToArray();
                plan=paths.Length>0?"MoveCommand":"EndActivationCommand";if(paths.Length>0)target=paths[0].g;
            }
            if(c.StartsWith("fixture:")) { Setup(p,c.Substring(8)); }
            if(c=="old-replays"){
                var paths=Directory.GetFiles("Docs/Prototype/Evidence/5051/manual-complete","*.jsonl",SearchOption.AllDirectories).Where(path=>path.Contains("-replay/"));
                File.WriteAllLines(Root+"results/accepted-baseline-replays.txt",paths.Select(path=>path+" | "+ReplayFiles.Verify(path).Matches+" | "+ReplayFiles.Verify(path).Message));
            }
            if(c=="export"){string path=p.ExportReplay(Root+"results/"+mode+"-replay");File.WriteAllText(Root+"results/"+mode+"-verification.txt",p.VerifyReplay(path).Message);}

        }
        var s=p.Duel;bool battle=s?.Encounter!=null;
        var data=new Data{gw=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).position.width,rw=p.HudRoot.worldBound.width,rh=p.HudRoot.worldBound.height,sw=Screen.width,sh=Screen.height,tx=target?.X??-1,ty=target?.Y??-1,mode=mode,battle=battle,ended=p.State.Outcome.IsEnded,round=p.State.Round,records=p.Journal.Records.Count,ai=p.PlayerVsAi,aiTurn=p.IsAiTurn,plan=plan,
            contact=s?.PendingContact?.Target.Formation.Side.ToString()??"",world=s!=null&&s.CanSave?s.CaptureSave():null,
            hash=s!=null&&s.CanSave?s.CaptureSave().checksum:BattleStateHash.Compute(p.State),active=p.State.CurrentUnitId?.Value??-1,spell=spell,ff=ff,persistentPath=Application.persistentDataPath,
            gx=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).position.x,gy=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).position.y,
            buttons=p.HudRoot.Query<Button>().ToList().Where(b=>b.enabledInHierarchy&&b.resolvedStyle.display!=DisplayStyle.None&&b.worldBound.width>0).Select(b=>new Btn{name=b.name,text=b.text,x=b.worldBound.center.x,y=b.worldBound.center.y}).ToArray(),
            fields=p.HudRoot.Query<DropdownField>().ToList().Select(d=>new Field{label=d.label,name=d.name,index=d.index,choices=d.choices.ToArray(),x=d.worldBound.center.x,y=d.worldBound.center.y}).ToArray(),
            toggles=p.HudRoot.Query<Toggle>().ToList().Select(t=>new Check{label=t.label,value=t.value,x=t.worldBound.center.x,y=t.worldBound.center.y}).ToArray(),
            units=p.State.Units.Select(u=>new Unit{id=u.Id.Value,side=u.Side.ToString(),x=u.Position.X,y=u.Position.Y,hp=u.Hp,armor=u.Armor,profile=u.Profile.Id.ToString(),action=u.ActionAvailable,movement=u.MovementRemaining,burn=u.BurnStacks,frozen=u.IsFrozen,barrier=u.TemporaryBarrier,expiry=u.BarrierActivations,fire=u.FireProtection,exhausted=u.IsExhausted,status=u.Status.ToString(),persistent=battle?s.Encounter.Ids[u.Id]:""}).ToArray(),
            hud=p.DuelSaveMessage+"\n"+p.Message+"\n"+string.Join("\n",p.HudRoot.Query<Label>().ToList().Where(l=>l.text.Length>0).Select(l=>l.text))};
        if(target.HasValue){var camera=p.GetComponentInChildren<Camera>();var surface=p.HudRoot.Q("board-input").worldBound;var v=camera.WorldToViewportPoint(new Vector3(target.Value.X,0,target.Value.Y));data.x=surface.x+v.x*surface.width;data.y=surface.y+(1-v.y)*surface.height;}
        string json=JsonUtility.ToJson(data);File.WriteAllText(Root+"state.json",json);if(data.hash!=previous){previous=data.hash;File.AppendAllText(Root+"results/"+mode+"-trace.jsonl",json+"\n");}
    }
    // Labelled initial-state fixtures only. All subsequent game commands are physical GUI input.
    static void Setup(BattlePresenter p,string name)
    {
        var data=new CrossroadsScenario(realm:true).CaptureSave();
        var west=data.realm.armies.Single(a=>a.id=="realm06-West-army-1");
        var arc=data.realm.armies.Single(a=>a.id=="realm06-West-army-2");
        var east=data.realm.armies.Single(a=>a.id=="realm06-East-army-1");
        var eastArc=data.realm.armies.Single(a=>a.id=="realm06-East-army-2");
        string caption="AUTHORED rare-case initial fixture: "+name+". No played history claimed. Subsequent commands use ordinary GUI controls.";
        if(name=="commanderless") {
            west.node=1;west.units[0].hp=0;west.units[0].status=2;west.provisions="20";
            arc.provisions="18";arc.units[0].hp=18;arc.units[1].hp=19;arc.units[1].armor=2;
        }else if(name=="joint") {west.node=6;arc.node=3;east.node=7;eastArc.node=11;}
        else if(name=="inspection") {west.node=3;arc.node=1;west.units[1].hp=19;west.units[1].armor=4;}
        else throw new Exception("Unknown 06 fixture");
        data.events=new[]{caption};data.checksum=data.ComputeHash();data.Restore();
        string file=Root+"results/"+name+"-authored-start.json";File.WriteAllText(file,JsonUtility.ToJson(data,true));
        if(!p.LoadDuel(file))throw new Exception(p.DuelSaveMessage);File.WriteAllText(Root+"results/"+mode+"-origin.txt",caption);
    }
    [Serializable]class Btn{public string name,text;public float x,y;}
    [Serializable]class Unit{public int id,x,y,hp,armor,movement,burn,barrier,expiry;public bool action,frozen,fire,exhausted;public string side,status,persistent,profile;}
    [Serializable]class Field{public string label,name;public int index;public string[] choices;public float x,y;}
    [Serializable]class Check{public string label;public bool value;public float x,y;}
    [Serializable]class Data{public string mode,hash,plan,contact,hud,spell,persistentPath;public bool ff;public float gx,gy,gw;public Field[] fields;public Check[] toggles;public bool battle,ended,ai,aiTurn;public int round,records,active;public float x,y,rw,rh;public int sw,sh,tx,ty;public Btn[] buttons;public Unit[] units;public CrossroadsSaveData world;}
}
