using RPG.Core;
using UnityEditor;
using UnityEngine;
namespace RPG.Presentation.Editor
{
    internal static class CombatLabLaunch
    {
        private const string Key="Convergence5051.PendingLaunch";
        [InitializeOnLoadMethod] private static void Register(){EditorApplication.playModeStateChanged-=Entered;EditorApplication.playModeStateChanged+=Entered;}
        private static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            string pending=SessionState.GetString(Key,"");SessionState.EraseString(Key);if(pending.Length==0)return;
            EditorApplication.delayCall+=()=>{var p=Object.FindAnyObjectByType<BattlePresenter>();if(p==null)return;if(pending=="07Inspect")p.LoadDuel(System.IO.Path.Combine(Application.streamingAssetsPath,"SeamlessWorlds07","authored-portal-arrival.json"));else if(pending=="07W")p.StartSeamless();else if(pending=="07Opaque")p.StartSeamlessOpaqueProbe();else if(pending=="07WI")p.StartSeamless(west:CombatPreset.Ice,east:CombatPreset.Fire);else if(pending=="07EI")p.StartSeamless(Side.East,west:CombatPreset.Ice,east:CombatPreset.Fire);else if(pending=="07E")p.StartSeamless(Side.East);else if(pending=="07T")p.StartSeamless(temporary:true);else if(pending=="06Inspect")p.LoadDuel(System.IO.Path.Combine(Application.streamingAssetsPath,"RealmOperations06","controlled-joint-aftermath.json"));else if(pending=="06WI")p.StartRealm(Side.West,CombatPreset.Ice,CombatPreset.Fire);else if(pending=="06EI")p.StartRealm(Side.East,CombatPreset.Ice,CombatPreset.Fire);else if(pending=="06W")p.StartRealm();else if(pending=="06E")p.StartRealm(Side.East);else if(pending=="05A")p.StartCity();else if(pending=="05B")p.StartCity(true);else p.StartCombatLab((CombatLabMatch)System.Enum.Parse(typeof(CombatLabMatch),pending));};
        }
        private static void Launch(string name)
        {
            if(EditorApplication.isPlaying){Debug.Log("Stop this isolated Play session before starting a new fixture.");return;}
            SessionState.SetString(Key,name);GrayboxSceneSetup.PlayScene();
        }
        [MenuItem("Gate C/Seamless Worlds 07/Inspection - AUTHORED portal arrival")] private static void WorldsInspection()=>Launch("07Inspect");
        [MenuItem("Gate C/Seamless Worlds 07/Opaque edge - CONTROLLED")] private static void WorldsOpaque()=>Launch("07Opaque");
        [MenuItem("Gate C/Seamless Worlds 07/West starts - Ice vs Fire")] private static void WorldsWestIce()=>Launch("07WI");
        [MenuItem("Gate C/Seamless Worlds 07/East starts - Ice vs Fire")] private static void WorldsEastIce()=>Launch("07EI");
        [MenuItem("Gate C/Seamless Worlds 07/West starts")] private static void WorldsWest()=>Launch("07W");
        [MenuItem("Gate C/Seamless Worlds 07/East starts")] private static void WorldsEast()=>Launch("07E");
        [MenuItem("Gate C/Seamless Worlds 07/Temporary Route - CONTROLLED")] private static void WorldsTemporary()=>Launch("07T");
        [MenuItem("Gate C/Combat Lab/Fire vs Ice (near contact)")] private static void Fire()=>Launch(nameof(CombatLabMatch.FireVsIce));
        [MenuItem("Gate C/Combat Lab/Support vs Fire (near contact)")] private static void Support()=>Launch(nameof(CombatLabMatch.SupportVsFire));
        [MenuItem("Gate C/Combat Lab/Mobile Blades (near contact)")] private static void Blades()=>Launch(nameof(CombatLabMatch.MobileBlades));
        [MenuItem("Gate C/Realm Operations 06/Inspection (CONTROLLED joint aftermath)")] private static void RealmInspect()=>Launch("06Inspect");
        [MenuItem("Gate C/Realm Operations 06/West starts (Ice vs Fire)")] private static void RealmWestIce()=>Launch("06WI");
        [MenuItem("Gate C/Realm Operations 06/East starts (Ice vs Fire)")] private static void RealmEastIce()=>Launch("06EI");
        [MenuItem("Gate C/Realm Operations 06/West starts (Fire vs Ice)")] private static void RealmWest()=>Launch("06W");
        [MenuItem("Gate C/Realm Operations 06/East starts (Fire vs Ice)")] private static void RealmEast()=>Launch("06E");
        [MenuItem("Gate C/City Foundations 05A")] private static void City()=>Launch("05A");
        [MenuItem("Gate C/City and Combat 05B (Fire vs Ice)")] private static void Combined()=>Launch("05B");
    }
}
