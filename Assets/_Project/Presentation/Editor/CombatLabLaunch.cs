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
            EditorApplication.delayCall+=()=>{var p=Object.FindAnyObjectByType<BattlePresenter>();if(p==null)return;if(pending=="05A")p.StartCity();else if(pending=="05B")p.StartCity(true);else p.StartCombatLab((CombatLabMatch)System.Enum.Parse(typeof(CombatLabMatch),pending));};
        }
        private static void Launch(string name)
        {
            if(EditorApplication.isPlaying){Debug.Log("Stop this isolated Play session before starting a new fixture.");return;}
            SessionState.SetString(Key,name);GrayboxSceneSetup.PlayScene();
        }
        [MenuItem("Gate C/Combat Lab/Fire vs Ice (near contact)")] private static void Fire()=>Launch(nameof(CombatLabMatch.FireVsIce));
        [MenuItem("Gate C/Combat Lab/Support vs Fire (near contact)")] private static void Support()=>Launch(nameof(CombatLabMatch.SupportVsFire));
        [MenuItem("Gate C/Combat Lab/Mobile Blades (near contact)")] private static void Blades()=>Launch(nameof(CombatLabMatch.MobileBlades));
        [MenuItem("Gate C/City Foundations 05A")] private static void City()=>Launch("05A");
        [MenuItem("Gate C/City and Combat 05B (Fire vs Ice)")] private static void Combined()=>Launch("05B");
    }
}
