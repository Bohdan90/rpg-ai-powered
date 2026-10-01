using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
namespace RPG.VisualTrial.Editor
{
 public static class HwTrialBuild
 {
  public const string Root="Assets/_Project/VisualTrials/HW_TI";
  static string[] names={"Idle","Walk","Attack","Hit","Death"};
  public static void Build()
  {
   AssetDatabase.Refresh();
   var model=Root+"/Models/HW_TI.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(model);
   importer.preserveHierarchy=true;importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.optimizeGameObjects=false;
   var h=importer.humanDescription;
   string[,] map={{"Hips","Hips"},{"Spine","Spine02"},{"Chest","Spine01"},{"UpperChest","Spine"},{"Neck","neck"},{"Head","Head"},{"LeftUpperLeg","LeftUpLeg"},{"LeftLowerLeg","LeftLeg"},{"LeftFoot","LeftFoot"},{"LeftToes","LeftToeBase"},{"RightUpperLeg","RightUpLeg"},{"RightLowerLeg","RightLeg"},{"RightFoot","RightFoot"},{"RightToes","RightToeBase"},{"LeftShoulder","LeftShoulder"},{"LeftUpperArm","LeftArm"},{"LeftLowerArm","LeftForeArm"},{"LeftHand","LeftHand"},{"RightShoulder","RightShoulder"},{"RightUpperArm","RightArm"},{"RightLowerArm","RightForeArm"},{"RightHand","RightHand"}};
   h.human=Enumerable.Range(0,map.GetLength(0)).Select(i=>new HumanBone{humanName=map[i,0],boneName=map[i,1],limit=new HumanLimit{useDefaultValues=true}}).ToArray();importer.humanDescription=h;importer.SaveAndReimport();
   var avatar=AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().Single();if(!avatar.isValid||!avatar.isHuman)throw new Exception("Humanoid mapping invalid: stop before any Generic fallback");
   foreach(var name in names){var path=Root+"/Animations/"+name+".fbx";var m=(ModelImporter)AssetImporter.GetAtPath(path);m.preserveHierarchy=true;m.animationType=ModelImporterAnimationType.Human;m.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;m.sourceAvatar=avatar;m.importAnimation=true;m.materialImportMode=ModelImporterMaterialImportMode.None;m.animationCompression=ModelImporterAnimationCompression.Off;
    var clips=m.defaultClipAnimations;foreach(var c in clips){c.name=name;c.loopTime=name=="Idle"||name=="Walk";c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;}m.clipAnimations=clips;m.SaveAndReimport();}
   Texture("BaseColor",true,false);Texture("Normal",false,true);Texture("MetallicSmoothness",false,false);
   Directory.CreateDirectory(Root+"/Generated");AssetDatabase.Refresh();
   var corrected=MaterialAsset("CorrectedPBR");corrected.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/BaseColor.png"));corrected.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Normal.png"));corrected.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/MetallicSmoothness.png"));corrected.SetFloat("_Smoothness",1);corrected.SetFloat("_Metallic",1);corrected.EnableKeyword("_NORMALMAP");corrected.EnableKeyword("_METALLICSPECGLOSSMAP");
   var raw=MaterialAsset("ProviderGlossComparison");raw.SetTexture("_BaseMap",corrected.GetTexture("_BaseMap"));raw.SetFloat("_Smoothness",.59f);raw.SetFloat("_Metallic",1); // provider animation factor: roughness .41, glTF metallic default1, maps lost
   var prop=MaterialAsset("TEMP_WeaponShield");prop.color=new Color(.3f,.24f,.18f);prop.SetFloat("_Metallic",.2f);prop.SetFloat("_Smoothness",.25f);
   var controllerPath=Root+"/Generated/HW_TI.controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
   if(controller.layers.Length==0)controller.AddLayer("Base Layer");var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
   foreach(var name in names){var state=machine.AddState(name);state.motion=AssetDatabase.LoadAllAssetsAtPath(Root+"/Animations/"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));if(name=="Idle")machine.defaultState=state;}
   foreach(var name in new[]{"Attack","Hit"}){var state=machine.states.Single(s=>s.state.name==name).state;var t=state.AddTransition(machine.defaultState);t.hasExitTime=true;t.exitTime=.95f;t.duration=.12f;}
   var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));go.name="HW_TI_Trial";var animator=go.GetComponent<Animator>();animator.avatar=avatar;animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var renderers=go.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.triangles.Length).ToArray();foreach(var s in renderers){s.sharedMaterial=corrected;s.updateWhenOffscreen=true;}
   var lod=go.GetComponent<LODGroup>()??go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.35f,new Renderer[]{renderers[0]}),new LOD(.12f,new Renderer[]{renderers[1]}),new LOD(.01f,new Renderer[]{renderers[2]})});lod.RecalculateBounds();
   Attach(go,"RightHand","TEMP_SwordSocket",prop,true);Attach(go,"LeftHand","TEMP_ShieldSocket",prop,false);
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Generated/HW_TI.prefab");UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var lab=new GameObject("HW_TI Visual Asset Lab — developer proof").AddComponent<HwVisualLab>();lab.Character=prefab;lab.Corrected=corrected;lab.ProviderGloss=raw;EditorSceneManager.SaveScene(scene,Root+"/Generated/HW_TI_VisualLab.unity");
   var report=new {avatarValid=avatar.isValid,humanoid=avatar.isHuman,bones=map.GetLength(0),clips=names,renderers=renderers.Length};Directory.CreateDirectory("/private/tmp/hw-ti-engine/results");File.WriteAllText("/private/tmp/hw-ti-engine/results/import.json",JsonUtility.ToJson(new ImportEvidence{valid=avatar.isValid,human=avatar.isHuman,lods=3}));Debug.Log("HW_TI_BUILD_PASS "+report); 
  }
  [Serializable]class ImportEvidence{public bool valid,human;public int lods;}
  static void Texture(string name,bool srgb,bool normal){var t=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/"+name+".png");t.sRGBTexture=srgb;t.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;t.maxTextureSize=4096;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();}
  static Material MaterialAsset(string name){var p=Root+"/Generated/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}return m;}
  static void Attach(GameObject root,string boneName,string socketName,Material mat,bool sword){var bone=root.GetComponentsInChildren<Transform>().Single(t=>t.name==boneName);var socket=new GameObject(socketName).transform;socket.SetParent(bone,false);socket.position=bone.position;socket.rotation=Quaternion.identity;socket.localScale=new Vector3(1/bone.lossyScale.x,1/bone.lossyScale.y,1/bone.lossyScale.z);
   var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name=sword?"TEMP sword — grip articulation not authored":"TEMP shield — socket proof only";p.transform.SetParent(socket,false);p.transform.localScale=sword?new Vector3(.045f,.018f,.8f):new Vector3(.42f,.55f,.045f);p.transform.localPosition=sword?new Vector3(0,0,.29f):new Vector3(0,0,.04f);p.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(p.GetComponent<Collider>());
  }
  public static void LaunchProof(){ShaderUtil.allowAsyncCompilation=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Show();EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();EditorSceneManager.OpenScene(Root+"/Generated/HW_TI_VisualLab.unity");UnityEngine.Object.FindAnyObjectByType<HwVisualLab>().AutoProof=true;EditorApplication.isPlaying=true;}
  [MenuItem("Gate C/Visual Asset Lab/HW_TI Meshy Trial 01")]
  public static void Launch(){if(EditorApplication.isPlaying){Debug.LogWarning("Finish current Play session before opening Visual Asset Lab.");return;}if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(Root+"/Generated/HW_TI_VisualLab.unity");EditorApplication.isPlaying=true;}
 }
}
