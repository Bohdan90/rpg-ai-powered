using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation
{
    // Low-fi Mission 01 view. Every action delegates to the Core session.
    public sealed class StrategicHud
    {
        public VisualElement Root { get; }
        private readonly BattlePresenter presenter;
        private readonly VisualElement map;
        private readonly Label status,preview,roster,events,saveMessage;
        private readonly Button save,load;
        private readonly Button move,interact,end;
        private readonly VisualElement attacks;
        private int selected;
        public StrategicHud(VisualElement parent,BattlePresenter presenter)
        {
            this.presenter=presenter;
            selected=presenter.World.PlayerNode;
            Root=new VisualElement {name="strategic-world"};Root.style.position=Position.Absolute;
            Root.style.left=Root.style.right=Root.style.top=Root.style.bottom=0;Root.style.backgroundColor=new Color(.055f,.085f,.11f);
            Root.style.flexDirection=FlexDirection.Row;Root.style.paddingLeft=Root.style.paddingRight=12;parent.Add(Root);
            var left=new ScrollView();left.style.width=Length.Percent(63);Root.Add(left);
            Label(left,"THE GATE THAT WOKE · Mission 01",22);
            Label(left,"Local scout reports reveal hostile positions. Select a node to preview; confirm movement separately.",13);
            map=new VisualElement {name="strategic-map"};map.style.height=510;map.style.flexShrink=0;left.Add(map);
            map.generateVisualContent+=DrawEdges;
            foreach(var node in StrategicGraph.Mission01.Nodes)
            {
                int id=node.Id;var button=Button(map,node.Id.ToString("00")+" "+node.Name,"world-node-"+id,()=>Select(id));
                button.style.position=Position.Absolute;button.style.width=112;button.style.height=52;button.style.fontSize=12;button.style.whiteSpace=WhiteSpace.Normal;
                button.style.color=Color.white;
                button.style.left=Length.Percent(node.X*11.8f);button.style.top=440-node.Y*64;
            }
            preview=Label(left,"Select the formation or a destination.",14);preview.name="world-preview";
            move=Button(left,"Move along preview (stop for Hard Guard)","world-move",()=>{if(!presenter.MoveOnWorld(selected))Refresh();});
            interact=Button(left,"Investigate Portal · 5 Tempo, ends activation","world-interact",()=>{presenter.World.InteractPortal();presenter.WorldChanged();});
            end=Button(left,"End strategic activation / advance world","world-end",()=>{presenter.World.EndActivation();presenter.WorldChanged();});
            attacks=new VisualElement();left.Add(attacks);
            var right=new ScrollView();right.style.flexGrow=1;right.style.paddingLeft=14;Root.Add(right);
            status=Label(right,"",16);status.name="world-status";
            save=Button(right,"Save Mission 01 (one slot)","world-save",()=>presenter.SaveStrategic());
            load=Button(right,"Load saved Mission 01","world-load-slot",()=>presenter.LoadStrategic());
            saveMessage=Label(right,"",12);saveMessage.name="world-save-status";
            Button(right,"Select persistent formation","world-select-player",()=>Select(presenter.World.PlayerNode));
            roster=Label(right,"",13);roster.name="world-roster";
            events=Label(right,"",12);events.name="world-events";
            Button(right,"Restart entire Mission 01","world-restart",presenter.StartStrategicScenario);
            Refresh();
        }
        private void DrawEdges(MeshGenerationContext context)
        {
            var painter=context.painter2D;painter.lineWidth=2;painter.strokeColor=new Color(.3f,.42f,.49f);
            foreach(var e in StrategicGraph.Mission01.Edges)
            {var a=map.Q<Button>("world-node-"+e.A);var b=map.Q<Button>("world-node-"+e.B);painter.BeginPath();painter.MoveTo(a.layout.center);painter.LineTo(b.layout.center);painter.Stroke();}
        }
        public void Select(int node){selected=node;Refresh();}
        public void Refresh()
        {
            var s=presenter.World;if(s==null)return;
            save.SetEnabled(s.CanSave);load.SetEnabled(s.CanSave);saveMessage.text=presenter.StrategicSaveMessage;
            Root.style.display=s.Encounter==null?DisplayStyle.Flex:DisplayStyle.None;
            var p=s.PreviewMove(selected);preview.text=selected==s.PlayerNode?"Persistent formation selected. Choose a connected destination.":
                string.Join(" → ",p.Path.Select(n=>n.ToString("00")))+"\nTempo cost "+p.Cost+(p.Encounter.HasValue?" · Hard Guard conflict / Attack Cost follows":"")+"\n"+(p.Reason??"Legal path");
            move.SetEnabled(p.IsLegal);interact.SetEnabled(s.CanInteractPortal);end.SetEnabled(s.IsPlayerActivation);
            string result=s.Result==StrategicMissionResult.Ongoing?"Investigate Portal; protect Village; resolve B; return to Keep."
                :s.Result==StrategicMissionResult.CouncilAssistanceRequested?"SUCCESS · Council assistance requested. Portal remains active.":"DEFEAT · "+(s.Result==StrategicMissionResult.VillageRavaged?"Riverside Village Ravaged":"No continuing formation");
            status.text="Refresh "+s.Refresh+" · Tempo "+s.Tempo+"\nProvisions "+s.Provisions+" / 36 · Consumption "+s.Consumption+(s.Hungry?" · HUNGRY: movement ×1.25":"")
                +"\n"+result+"\nVillage "+s.Village+" · Waystation "+s.Waystation+" (Food "+s.WaystationFood+")"
                +"\nPortal investigated: "+s.PortalInvestigated+" · Village threat resolved: "+s.VillageThreatResolved
                +"\nBridge cleared: "+s.BridgeGuardDefeated+"\n"+s.LastBattleSummary;
            roster.text="\nPERSISTENT FORMATION · "+(s.Player.Commanderless?"Commanderless / roster locked":"Commander-led")+"\n"+string.Join("\n",s.Player.Members.Select(c=>
                c.CharacterId+" · "+c.Profile.Id+" · "+c.Status+"\nHP "+c.Hp+"/"+c.Profile.MaxHp+" · Armor "+c.Armor+"/"+c.Profile.MaxArmor
                +" · XP "+c.PersonalXp.ToString("0.##")+" L"+c.PersonalLevel+(c.IsCommander?" · Command XP "+c.CommandXp.ToString("0.##")+" L"+c.CommandLevel+" Rank "+c.CommandRank:"")));
            events.text="\nSCOUT REPORTS / WORLD EVENTS\n"+string.Join("\n",s.Actors.Select(a=>a.Kind+": "+a.Objective+" @ "+a.Node+(a.RaidArmed?" · RAID ARMED":"")))+"\n\n"+string.Join("\n",s.Events.Reverse().Take(18));
            attacks.Clear();foreach(var a in s.Actors.Where(a=>s.CanAttack(a.Kind)))
            {var kind=a.Kind;Button(attacks,"Attack "+kind+" · costs up to 50 Tempo","world-attack-"+kind,()=>{s.Attack(kind);presenter.WorldChanged();});}
            foreach(var node in s.Graph.Nodes)
            {
                var button=map.Q<Button>("world-node-"+node.Id);var occupants=s.Actors.Where(a=>a.Active&&a.Node==node.Id).Select(a=>a.Kind.ToString());
                button.text=node.Id.ToString("00")+" "+node.Name+(s.PlayerNode==node.Id?"\n◆ PLAYER":"")+(occupants.Any()?"\n"+string.Join(" / ",occupants):"");
                button.style.backgroundColor=s.PlayerNode==node.Id?new Color(.16f,.4f,.57f):occupants.Any()?new Color(.5f,.23f,.16f):new Color(.18f,.24f,.28f);
                button.tooltip=node.Name+"; adjacent: "+string.Join(", ",s.Graph.Neighbors(node.Id).Select(n=>n+" ("+s.Graph.Cost(node.Id,n,s.Hungry)+")"));
            }
            map.MarkDirtyRepaint();
        }
        private static Label Label(VisualElement parent,string text,int size)
        {var l=new Label(text);l.style.whiteSpace=WhiteSpace.Normal;l.style.color=new Color(.9f,.94f,.97f);l.style.fontSize=size;l.style.marginBottom=8;parent.Add(l);return l;}
        private static Button Button(VisualElement parent,string text,string name,System.Action action)
        {var b=new Button(action){text=text,name=name};b.style.minHeight=32;parent.Add(b);return b;}
    }
}
