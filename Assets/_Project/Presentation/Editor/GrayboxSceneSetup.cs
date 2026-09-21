using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace RPG.Presentation.Editor
{
    public static class GrayboxSceneSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/TacticalGraybox.unity";
        public const string PanelPath = "Assets/_Project/UI/GrayboxPanelSettings.asset";

        [MenuItem("Gate C/Create or Open Tactical Graybox")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1440, 900);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = .5f;
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/_Project/UI/GrayboxTheme.tss");
                AssetDatabase.CreateAsset(panel, PanelPath);
            }
            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Tactical Graybox").AddComponent<BattlePresenter>();
            }
            else EditorSceneManager.OpenScene(ScenePath);
            var bootstrap = Object.FindAnyObjectByType<BattlePresenter>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("panelSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            serialized.FindProperty("unlitShader").objectReferenceValue = Shader.Find("Universal Render Pipeline/Unlit");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(bootstrap.gameObject.scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Gate C/Play Tactical Graybox")]
        public static void PlayScene()
        {
            CreateScene();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == ScenePath) EditorApplication.EnterPlaymode();
        }

        // Optional local integration build. Uses only this scene; does not change project build defaults.
        public static void BuildValidationPlayer()
        {
            CreateScene();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "GateCGraybox.app"),
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Graybox validation player build failed.");
        }
    }
}
