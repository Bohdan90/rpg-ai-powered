#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
namespace RPG.VisualTrial.Tests
{
 public class HwTrialPlaybackTests
 {
  [UnityTest]public IEnumerator ActualHumanoidPlaybackTransitionsAndSocketsStayBound()
  {
   var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/VisualTrials/HW_TI/Generated/HW_TI.prefab"));var a=go.GetComponent<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;var hand=a.GetBoneTransform(HumanBodyBones.RightHand);var socket=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="RightHandWeaponSocket");var local=socket.localPosition;var marker=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="GripPoint");var shield=go.GetComponentsInChildren<Transform>().Single(t=>t.name=="LeftForearmShieldSocket");
   try{a.Play("Idle",0,0);yield return null;Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Idle"),Is.True);Vector3 first=hand.position;
    a.CrossFadeInFixedTime("Walk",.1f);yield return new WaitForSeconds(.6f);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Walk"),Is.True);Assert.That(Vector3.Distance(first,hand.position),Is.GreaterThan(.01f));a.CrossFadeInFixedTime("Idle",.1f);yield return new WaitForSeconds(.2f);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Idle"),Is.True);
    foreach(var state in new[]{"Attack","Hit"}){a.CrossFadeInFixedTime(state,.1f);yield return new WaitForSeconds(.4f);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName(state),Is.True);Assert.That(socket.parent,Is.EqualTo(hand));Assert.That(socket.localPosition,Is.EqualTo(local));Assert.That(Vector3.Distance(socket.position,marker.position),Is.LessThan(.0001f));Assert.That(shield.parent,Is.EqualTo(a.GetBoneTransform(HumanBodyBones.LeftLowerArm)));yield return new WaitForSeconds(2);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Idle"),Is.True);}
    a.CrossFadeInFixedTime("Death",.1f);yield return new WaitForSeconds(3.2f);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Death"),Is.True);Assert.That(a.GetBoneTransform(HumanBodyBones.Head).position.y,Is.LessThan(.8f));Assert.That(socket.localPosition,Is.EqualTo(local));Assert.That(Vector3.Distance(socket.position,marker.position),Is.LessThan(.0001f));Assert.That(shield.parent,Is.EqualTo(a.GetBoneTransform(HumanBodyBones.LeftLowerArm)));
   }finally{Object.Destroy(go);}
  }
 }
}
#endif
