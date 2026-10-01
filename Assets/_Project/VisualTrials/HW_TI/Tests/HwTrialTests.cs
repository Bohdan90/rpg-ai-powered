using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace RPG.VisualTrial.Tests
{
 public class HwTrialTests
 {
  const string R="Assets/_Project/VisualTrials/HW_TI";
  [Test]public void CanonicalPrefabHasHealthyHumanoidAndLodsOnOneSkeleton(){var p=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Generated/HW_TI.prefab");Assert.That(p,Is.Not.Null);var a=p.GetComponent<Animator>();Assert.That(a.avatar.isValid&&a.avatar.isHuman,Is.True);Assert.That(a.runtimeAnimatorController,Is.Not.Null);var sm=p.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.That(sm.Length,Is.EqualTo(3));Assert.That(sm.Select(s=>s.rootBone).Distinct().Count(),Is.EqualTo(1));Assert.That(sm.Select(s=>s.sharedMesh.triangles.Length/3).OrderByDescending(n=>n),Is.EqualTo(new[]{102053,50000,25000}));Assert.That(p.GetComponentsInChildren<Transform>().Any(t=>t.name=="TEMP_SwordSocket"),Is.True);Assert.That(p.GetComponentsInChildren<Transform>().Any(t=>t.name=="TEMP_ShieldSocket"),Is.True);var sword=p.GetComponentsInChildren<Transform>().Single(t=>t.name=="TEMP_SwordSocket");Assert.That(sword.lossyScale.x,Is.EqualTo(1).Within(.001));Assert.That(sword.lossyScale.y,Is.EqualTo(1).Within(.001));Assert.That(p.GetComponentsInChildren<Transform>().SelectMany(t=>t.GetComponents<Component>()).Any(c=>c==null),Is.False);}
  [TestCase("Idle")][TestCase("Walk")][TestCase("Attack")][TestCase("Hit")][TestCase("Death")]
  public void ClipsAreHumanoidAndSkeletonOnly(string name){var path=R+"/Animations/"+name+".fbx";var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>c.name==name);Assert.That(clip.humanMotion,Is.True);Assert.That(clip.length,Is.GreaterThan(.5f));Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<SkinnedMeshRenderer>(),Is.Empty);var m=(ModelImporter)AssetImporter.GetAtPath(path);Assert.That(m.avatarSetup,Is.EqualTo(ModelImporterAvatarSetup.CopyFromOther));Assert.That(m.sourceAvatar,Is.Not.Null);}
  [Test]public void RestoredPbrHasRealMapsAndCorrectColorSpaces(){var m=AssetDatabase.LoadAssetAtPath<Material>(R+"/Generated/CorrectedPBR.mat");foreach(var slot in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})Assert.That(m.GetTexture(slot),Is.Not.Null);Assert.That(m.IsKeywordEnabled("_NORMALMAP"),Is.True);Assert.That(m.IsKeywordEnabled("_METALLICSPECGLOSSMAP"),Is.True);Assert.That(((TextureImporter)AssetImporter.GetAtPath(R+"/Textures/MetallicSmoothness.png")).sRGBTexture,Is.False);Assert.That(((TextureImporter)AssetImporter.GetAtPath(R+"/Textures/Normal.png")).textureType,Is.EqualTo(TextureImporterType.NormalMap));}
  [Test]public void ControllerHasRequiredClipsAndSceneExists(){var p=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Generated/HW_TI.prefab");Assert.That(p.GetComponent<Animator>().runtimeAnimatorController.animationClips.Select(c=>c.name).Distinct().OrderBy(n=>n),Is.EqualTo(new[]{"Attack","Death","Hit","Idle","Walk"}));Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(R+"/Generated/HW_TI_VisualLab.unity"),Is.Not.Null);}
 }
}
