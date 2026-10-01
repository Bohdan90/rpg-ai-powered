using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Core;
using UnityEngine;
using UnityEngine.UIElements;
namespace RPG.Presentation
{
    // View-only canvas: no physical rule uses these offsets, scale or residency.
    public sealed class SeamlessMapView
    {
        public VisualElement Root {get;} public VisualElement Canvas {get;}
        public WorldAddress? Selected {get;private set;}
        public Vector2 ValleyOffset {get;set;}=new Vector2(1400,380);
        public float ValleyRotation {get;set;}
        public bool ValleyVisible {get;set;}=true;
        private readonly BattlePresenter p;private readonly Action<WorldAddress> select;private readonly Action changed;
        private readonly Label info,journeyInfo;private readonly Button traverse,setDestination,cancelDestination,continueTravel;private readonly VisualElement viewport,events,tools;
        private readonly Dictionary<WorldAddress,Button> buttons=new Dictionary<WorldAddress,Button>();
        private readonly Dictionary<Side,(Vector2,float)> views=new Dictionary<Side,(Vector2,float)>();
        private Vector2 pan=new Vector2(30,10),lastPointer,dragStart;private float zoom=.55f;
        private (Vector2,float)? previous;private bool dragging,moved;private int pointer;private Side? viewer;
        private WorldObservation observation;private bool observationPaused;private int observationStep;
        private string confirmPortal;private WorldAddress confirmOrigin;private int confirmRevision;private string confirmState;
        public SeamlessMapView(VisualElement parent,BattlePresenter presenter,Action<WorldAddress> selection,Action onChanged)
        {
            p=presenter;select=selection;changed=onChanged;if(p.Duel.Seamless.ProductionTopology)ValleyOffset=new Vector2(3300,0);Root=new VisualElement{name="seamless-map"};parent.Add(Root);
            tools=new VisualElement();tools.style.flexDirection=FlexDirection.Row;tools.style.flexWrap=Wrap.Wrap;Root.Add(tools);
            Add(tools,"Fit Known Worlds","worlds-fit",Fit);Add(tools,"Focus selected army","worlds-army",()=>{var f=p.Duel.Realm.Selected(p.Duel.ActiveSide);if(f!=null)Focus(f.Address);});
            Add(tools,"Previous view","worlds-previous",()=>{if(previous.HasValue){var v=previous.Value;previous=(pan,zoom);pan=v.Item1;zoom=v.Item2;Transform();}});
            Add(tools,"Focus known counterpart","worlds-counterpart",()=>{var f=p.Duel.Realm.Selected(p.Duel.ActiveSide);var l=SeamlessWorlds.Portals.FirstOrDefault(x=>x.Contains(f.Address));if(l!=null&&p.Duel.Seamless.Knowledge(p.Duel.ActiveSide).KnowsLink(l.Id))Focus(l.Other(f.Address));});
            traverse=Add(tools,"Traverse Portal · preview 20 Tempo attempt","worlds-traverse",Traverse);traverse.style.backgroundColor=new Color(.45f,.25f,.65f);traverse.style.fontSize=18;
            setDestination=Add(tools,"Set / Change Destination","worlds-destination",()=>{if(Selected.HasValue&&p.Duel.Seamless.SetDestination(p.Duel.ActiveSide,Selected.Value))changed();});
            continueTravel=Add(tools,"Continue Travel","worlds-continue-travel",ContinueTravel);
            cancelDestination=Add(tools,"Cancel Destination","worlds-cancel-destination",()=>{var f=p.Duel.Realm.Selected(p.Duel.ActiveSide);if(f!=null&&p.Duel.Seamless.CancelDestination(p.Duel.ActiveSide,f.Formation.FormationId))changed();});
            journeyInfo=new Label{name="worlds-journey"};journeyInfo.style.whiteSpace=WhiteSpace.Normal;journeyInfo.style.color=Color.white;Root.Add(journeyInfo);
            info=new Label();info.style.whiteSpace=WhiteSpace.Normal;info.style.color=Color.white;Root.Add(info);
            viewport=new VisualElement{name="worlds-viewport"};viewport.style.height=590;viewport.style.overflow=Overflow.Hidden;viewport.style.backgroundColor=new Color(.025f,.027f,.043f);Root.Add(viewport);
            Canvas=new VisualElement{name="worlds-canvas",pickingMode=PickingMode.Ignore};Canvas.style.position=UnityEngine.UIElements.Position.Absolute;Canvas.style.width=4700;Canvas.style.height=2400;Canvas.style.transformOrigin=new TransformOrigin(Length.Percent(0),Length.Percent(0),0);viewport.Add(Canvas);Canvas.generateVisualContent+=Draw;
            viewport.RegisterCallback<WheelEvent>(e=>{float next=Mathf.Clamp(zoom*(e.delta.y>0?.86f:1.16f),.15f,1.8f);var cursor=e.localMousePosition;pan=cursor-(cursor-pan)*(next/zoom);zoom=next;confirmPortal=null;Transform();e.StopPropagation();});
            viewport.RegisterCallback<PointerDownEvent>(e=>{if(e.button!=0&&e.button!=2)return;dragging=true;moved=false;pointer=e.pointerId;lastPointer=dragStart=e.position;},TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerMoveEvent>(e=>{if(!dragging)return;Vector2 at=e.position;if(!moved&&Vector2.Distance(at,dragStart)>5){moved=true;viewport.CapturePointer(pointer);confirmPortal=null;}if(moved){pan+=at-lastPointer;Transform();e.StopPropagation();}lastPointer=at;});
            viewport.RegisterCallback<PointerUpEvent>(e=>{if(!dragging)return;dragging=false;if(viewport.HasPointerCapture(pointer))viewport.ReleasePointer(pointer);if(moved)e.StopImmediatePropagation();},TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerCaptureOutEvent>(_=>dragging=false);
            if(p.Duel.Seamless.ProductionTopology){for(int z=0;z<ProductionRoads.ZoneNodes.Length;z++){var nodes=ProductionRoads.ZoneNodes[z].Select(n=>Position(new WorldAddress(WorldId.Frontier,n))).ToArray();var label=new Label(ProductionRoads.Zones[z]){pickingMode=PickingMode.Ignore};label.style.position=UnityEngine.UIElements.Position.Absolute;label.style.left=nodes.Min(v=>v.x);label.style.top=nodes.Min(v=>v.y)-100;label.style.color=new Color(.8f,.8f,.65f);label.style.fontSize=22;Canvas.Add(label);}}
            events=new VisualElement{name="world-observations"};Root.Add(events);
            viewport.RegisterCallback<GeometryChangedEvent>(e=>{if(e.oldRect.width<=0&&e.newRect.width>0)Fit();});
            Root.schedule.Execute(()=>{if(observation!=null&&!observationPaused&&observationStep<observation.nodes.Length-1){observationStep++;Canvas.MarkDirtyRepaint();}}).Every(400);Transform();
        }
        public Vector2 Position(WorldAddress a)
        {
            var n=p.Duel.GraphFor(a.World).Node(a.Node);var local=a.World==WorldId.Frontier?new Vector2(80+n.X*120,(p.Duel.Seamless.ProductionTopology?1300:800)-n.Y*95):new Vector2(n.X*115,-n.Y*95);
            if(a.World==WorldId.Frontier)return local;float r=ValleyRotation*Mathf.Deg2Rad;return ValleyOffset+new Vector2(local.x*Mathf.Cos(r)-local.y*Mathf.Sin(r),local.x*Mathf.Sin(r)+local.y*Mathf.Cos(r));
        }
        public void Focus(WorldAddress a){if(p.Duel.Seamless.Knowledge(p.Duel.ActiveSide).At(a)==KnowledgeLevel.Unexplored)return;previous=(pan,zoom);zoom=.72f;pan=new Vector2(Mathf.Max(200,viewport.resolvedStyle.width/2),270)-Position(a)*zoom;Transform();}
        public void Fit()
        {
            var k=p.Duel.Seamless.Knowledge(p.Duel.ActiveSide);var points=k.KnownNodes.Select(Position).ToArray();if(points.Length==0)return;previous=(pan,zoom);var min=new Vector2(points.Min(x=>x.x)-70,points.Min(x=>x.y)-80);var max=new Vector2(points.Max(x=>x.x)+110,points.Max(x=>x.y)+100);zoom=Mathf.Clamp(Mathf.Min(Mathf.Max(300,viewport.resolvedStyle.width)/(max.x-min.x),540/(max.y-min.y)),.15f,1.2f);pan=new Vector2(0,20)-min*zoom;Transform();
        }
        private void Transform(){Canvas.transform.position=pan;Canvas.transform.scale=new Vector3(zoom,zoom,1);if(viewer.HasValue)views[viewer.Value]=(pan,zoom);Canvas.MarkDirtyRepaint();}
        public void CancelInputs(){confirmPortal=null;dragging=false;if(viewport.HasPointerCapture(pointer))viewport.ReleasePointer(pointer);}
        private void ContinueTravel(){if(p.Duel.Seamless.ContinueTravel(p.Duel.ActiveSide))changed();}
        private void Traverse()
        {
            var w=p.Duel;var f=w.Realm.Selected(w.ActiveSide);if(f==null)return;var q=w.Seamless.PreviewTraverse(w.ActiveSide,f.Formation.FormationId,f.Address);
            if(!q.Legal){info.text=q.Reason;confirmPortal=null;return;}
            if(confirmPortal==f.Formation.FormationId&&confirmOrigin==f.Address&&confirmRevision==w.Seamless.Revision&&confirmState==w.CaptureSave().checksum){w.Seamless.Traverse(w.ActiveSide,confirmPortal,confirmOrigin,confirmRevision);confirmPortal=null;changed();return;}
            confirmPortal=f.Formation.FormationId;confirmOrigin=f.Address;confirmRevision=w.Seamless.Revision;confirmState=w.CaptureSave().checksum;traverse.text="Confirm Traverse Portal · spend 20 Tempo";info.text="CONFIRM: "+q.Destination+" · "+q.RouteState+". Attempt spends20 Tempo even if exit is blocked. Click Traverse again.";
        }
        public void ShowObservation(int sequence)
        {
            var s=p.Duel.Seamless;var e=s.Knowledge(p.Duel.ActiveSide).History.FirstOrDefault(x=>x.sequence==sequence);if(e==null)return;observation=e;observationPaused=false;observationStep=0;Focus(new WorldAddress((WorldId)e.world,e.nodes[0]));s.MarkRead(p.Duel.ActiveSide,sequence,(WorldId)e.world);Canvas.MarkDirtyRepaint();info.text="HISTORICAL R"+e.refresh+" · "+e.description+" · no simulation replay";
        }
        public void Refresh()
        {
            var w=p.Duel;var s=w.Seamless;var side=w.ActiveSide;
            if(w.HandoffPending||w.PendingContact!=null){Root.style.display=DisplayStyle.None;confirmPortal=null;observation=null;return;}
            Root.style.display=DisplayStyle.Flex;
            if(viewer!=side){Selected=null;confirmPortal=null;observation=null;previous=null;viewer=side;if(views.TryGetValue(side,out var v)){pan=v.Item1;zoom=v.Item2;}else{pan=new Vector2(30,10);zoom=.55f;}Transform();}
            var k=s.Knowledge(side);var addresses=k.KnownNodes.Where(a=>(a.World!=WorldId.StoneValley||ValleyVisible)&&(!s.ProductionTopology||(s.DenseTravel?a.World==WorldId.Frontier?ProductionRoads.Marker(a):a.Node<1000:ProductionRoads.Marker(a))||s.DenseTravel&&k.At(a)==KnowledgeLevel.CurrentlyObserved&&w.GraphFor(a.World).Neighbors(a.Node).Any(n=>k.At(new WorldAddress(a.World,n))==KnowledgeLevel.Unexplored)||w.Realm.Armies.Any(f=>f.Formation.Side==side&&f.Continues&&f.Address==a)||k.LastKnown.Any(f=>f.Address==a))).ToHashSet();
            foreach(var a in buttons.Keys.Where(a=>!addresses.Contains(a)).ToArray()){buttons[a].RemoveFromHierarchy();buttons.Remove(a);}
            foreach(var a in addresses.OrderBy(a=>a)){
                if(!buttons.TryGetValue(a,out var b)){var captured=a;b=Add(Canvas,"","world-node-"+a,()=>{if(moved)return;Selected=captured;confirmPortal=null;select(captured);changed();});b.style.position=UnityEngine.UIElements.Position.Absolute;b.style.width=112;b.style.height=76;b.style.fontSize=12;buttons.Add(a,b);}
                var at=Position(a);bool junction=s.ProductionTopology&&a.World==WorldId.Frontier&&!ProductionRoads.PointsOfInterest.Contains(a.Node)&&!w.Realm.Armies.Any(f=>f.Continues&&f.Address==a&&f.Formation.Side==side)&&!k.LastKnown.Any(f=>f.Address==a);
                b.style.width=junction?24:112;b.style.height=junction?24:76;b.style.left=at.x-(junction?12:56);b.style.top=at.y-(junction?12:38);var state=k.At(a);var place=k.Place(a);
                var own=w.Realm.Armies.Where(f=>f.Continues&&f.Formation.Side==side&&f.Address==a).Select(f=>f.Formation.FormationId.Replace("realm06-","")+" ("+f.Formation.LivingMembers.Count()+")");
                var known=k.LastKnown.Where(f=>f.Address==a).Select(f=>(state==KnowledgeLevel.CurrentlyObserved?"":"Last seen R"+f.refresh+": ")+f.count+" enemy · "+f.composition);
                b.text=(s.ProductionTopology?"":a+" ")+p.Duel.GraphFor(a.World).Node(a.Node).Name+"\n"+string.Join("\n",own.Concat(known));b.tooltip=state+" · "+(place==null?"dynamic state unknown":(place.owner>=0?"owner "+(Side)place.owner+" · ":"")+(place.food>=0?"Food "+place.food+" · ":"")+place.portalState);
                if(junction){b.text="◇";b.tooltip="Crossroads · "+a+" · "+b.tooltip;}
                b.style.backgroundColor=Selected==a?new Color(.55f,.42f,.13f):state==KnowledgeLevel.CurrentlyObserved?new Color(.35f,.49f,.43f):new Color(.22f,.26f,.30f);
            }
            var selected=w.Realm.Selected(side);var portal=selected==null?null:s.PreviewTraverse(side,selected.Formation.FormationId,selected.Address);
            bool atPortal=selected!=null&&selected.Continues&&SeamlessWorlds.Portals.Any(l=>l.Contains(selected.Address));
            traverse.style.display=atPortal?DisplayStyle.Flex:DisplayStyle.None;traverse.SetEnabled(portal?.Legal==true);
            traverse.text=confirmPortal==null?"Traverse Portal · preview 20 Tempo attempt":"Confirm Traverse Portal · spend 20 Tempo";
            var journey=selected==null?null:s.Journey(selected.Formation.FormationId);
            cancelDestination.SetEnabled(journey!=null&&w.CanAct(side));
            var continuation=s.PreviewContinueTravel(side);continueTravel.SetEnabled(continuation.IsLegal);continueTravel.tooltip=continuation.Reason??"Spend current Tempo on the saved route.";
            var plan=Selected.HasValue?s.PreviewJourney(side,Selected.Value):null;setDestination.SetEnabled(plan?.IsLegal==true);
            string Describe(WorldAddress a)=>w.GraphFor(a.World).Node(a.Node).Name+" ("+a+")";
            journeyInfo.text=journey==null?"Select a known destination. Teal: reachable now; gold: later own activations.":"Destination: "+Describe(journey.Destination)+" · "+(journey.paused!=""?"PAUSED: "+journey.paused:"queued; use Continue Travel. End Turn does not move")+"\nRoad progress: "+(journey.next-1)+"/"+(journey.route.Length-1)+" segments · remaining "+w.GraphFor((WorldId)journey.world).PathCost(journey.route.Skip(Math.Max(0,journey.next-1)).ToArray(),selected.RealmProvisions==0)+" Tempo"+" · Cancel Destination / select another POI to change.";
            if(journey!=null)journeyInfo.text+="\nContinue Travel: budget "+selected.Tempo+" Tempo · cost "+continuation.Cost+" · expected stop "+Describe(new WorldAddress(selected.WorldId,continuation.Path.LastOrDefault()==0?selected.Node:continuation.Path.Last()))+" · "+(continuation.Reason??"Teal route; discoveries/contact may stop earlier.");
            if(plan?.IsLegal==true&&(journey==null||Selected.Value!=journey.Destination))journeyInfo.text+="\nSelected: "+Describe(Selected.Value)+" · total "+plan.Cost+" Tempo · "+s.ReachableLegs(selected,plan.Path)+" legs reachable now.";
            if(confirmPortal==null&&observation==null){var f=w.Realm.Selected(side);var q=f==null?null:s.PreviewTraverse(side,f.Formation.FormationId,f.Address);info.text="Pan: drag · zoom: wheel · roads cross only at marked junctions. "+(q==null?"":q.Legal?"Portal: "+q.Destination+" · "+q.RouteState+" · attempt20 even if blocked.":atPortal?q.Reason:"Select a POI or crossroads to plan travel.");}
            events.Clear();foreach(var id in k.Worlds){var captured=id;int count=k.History.Count(e=>e.world==(int)id&&e.sequence>s.ReadThrough(side,id));Add(events,SeamlessWorlds.Name(id)+" · "+count+" unread · focus","world-focus-"+id,()=>Focus(k.KnownNodes.First(a=>a.World==captured)));}
            foreach(var e in k.History.Reverse().Take(8)){int sequence=e.sequence;Add(events,"Show observation R"+e.refresh+" · "+SeamlessWorlds.Name((WorldId)e.world)+" · "+e.kind+" · "+e.description,"observation-"+sequence,()=>ShowObservation(sequence));}
            Add(events,observationPaused?"Resume observation":"Pause observation","observation-pause",()=>{observationPaused=!observationPaused;info.text="HISTORICAL overlay "+(observationPaused?"paused":"playing");Refresh();});
            Add(events,"Fast-forward observed fragment","observation-fast",()=>{if(observation!=null)observationStep=observation.nodes.Length-1;Canvas.MarkDirtyRepaint();});
            Add(events,"Skip / clear historical overlay","observation-skip",()=>{observation=null;s.MarkRead(side,k.History.LastOrDefault()?.sequence??0);Refresh();});Canvas.MarkDirtyRepaint();
        }
        private void Draw(MeshGenerationContext c)
        {
            if(!viewer.HasValue||p.Duel.HandoffPending||p.Duel.PendingContact!=null)return;var k=p.Duel.Seamless.Knowledge(viewer.Value);var pen=c.painter2D;
            foreach(var id in k.Worlds.Where(id=>id!=WorldId.StoneValley||ValleyVisible)){
                // A is two neighboring terrain patches with the same material and no seam gap.
                var points=k.KnownNodes.Where(a=>a.World==id).Select(Position).ToArray();float minX=points.Min(v=>v.x)-80,maxX=points.Max(v=>v.x)+80,minY=points.Min(v=>v.y)-90,maxY=points.Max(v=>v.y)+90;
                pen.fillColor=id==WorldId.Frontier?new Color(.11f,.19f,.15f):new Color(.18f,.17f,.22f);
                int chunks=id==WorldId.Frontier?2:1;for(int chunk=0;chunk<chunks;chunk++){float x=minX+(maxX-minX)*chunk/chunks,x2=minX+(maxX-minX)*(chunk+1)/chunks;pen.BeginPath();pen.MoveTo(new Vector2(x,minY));pen.LineTo(new Vector2(x2,minY));pen.LineTo(new Vector2(x2,maxY));pen.LineTo(new Vector2(x,maxY));pen.ClosePath();pen.Fill();}
                pen.lineWidth=3;pen.strokeColor=new Color(.48f,.52f,.47f);foreach(var edge in p.Duel.GraphFor(id).Edges){var a=new WorldAddress(id,edge.A);var b=new WorldAddress(id,edge.B);if(k.At(a)==KnowledgeLevel.Unexplored||k.At(b)==KnowledgeLevel.Unexplored)continue;pen.BeginPath();pen.MoveTo(Position(a));pen.LineTo(Position(b));pen.Stroke();}
            }
            var f=p.Duel.Realm.Selected(viewer.Value);var s=p.Duel.Seamless;var journey=f==null?null:s.Journey(f.Formation.FormationId);
            var plan=Selected.HasValue?s.PreviewJourney(viewer.Value,Selected.Value):null;
            bool saved=journey!=null&&(!Selected.HasValue||Selected.Value==journey.Destination);var route=saved?journey.route:plan?.IsLegal==true?plan.Path:null;int start=saved?journey.next:1;var routeWorld=saved?(WorldId)journey.world:Selected?.World??WorldId.Frontier;
            if(f!=null&&route!=null&&(routeWorld!=WorldId.StoneValley||ValleyVisible)){int reachable=saved?Math.Max(0,s.PreviewContinueTravel(viewer.Value).Path.Length-1):s.ReachableLegs(f,route,start);for(int i=start;i<route.Length;i++){var a=new WorldAddress(routeWorld,route[i-1]);var b=new WorldAddress(routeWorld,route[i]);if(k.At(a)==KnowledgeLevel.Unexplored||k.At(b)==KnowledgeLevel.Unexplored)continue;pen.lineWidth=7;pen.strokeColor=i<start+reachable?new Color(.15f,1f,.85f):new Color(1f,.72f,.12f);pen.BeginPath();pen.MoveTo(Position(a));pen.LineTo(Position(b));pen.Stroke();}}
            if(saved&&f!=null){var forecast=s.PreviewContinueTravel(viewer.Value);if(forecast.Path.Length>1){pen.lineWidth=4;pen.strokeColor=Color.cyan;pen.BeginPath();pen.Arc(Position(new WorldAddress(f.WorldId,forecast.Path.Last())),22,0,360);pen.Stroke();}}
            pen.lineWidth=2;pen.strokeColor=new Color(.55f,.3f,.8f);foreach(var link in SeamlessWorlds.Portals.Where(l=>k.KnowsLink(l.Id)&&buttons.ContainsKey(l.A)&&buttons.ContainsKey(l.B))){pen.BeginPath();pen.MoveTo(Position(link.A));pen.LineTo(Position(link.B));pen.Stroke();}
            if(observation!=null&&((WorldId)observation.world!=WorldId.StoneValley||ValleyVisible)){pen.lineWidth=6;pen.strokeColor=Color.yellow;foreach(int n in observation.nodes.Take(observationStep+1)){var pos=Position(new WorldAddress((WorldId)observation.world,n));pen.BeginPath();pen.Arc(pos,48,0,360);pen.Stroke();}for(int i=1;i<=observationStep;i++){pen.BeginPath();pen.MoveTo(Position(new WorldAddress((WorldId)observation.world,observation.nodes[i-1])));pen.LineTo(Position(new WorldAddress((WorldId)observation.world,observation.nodes[i])));pen.Stroke();}}
        }
        private static Button Add(VisualElement parent,string text,string name,Action action){var b=new Button(action){text=text,name=name};b.style.minHeight=28;b.style.whiteSpace=WhiteSpace.Normal;parent.Add(b);return b;}
    }
}
