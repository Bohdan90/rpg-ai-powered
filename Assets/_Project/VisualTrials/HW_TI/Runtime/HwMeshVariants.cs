using System;
using UnityEngine;
namespace RPG.VisualTrial
{
 public sealed class HwMeshVariants:MonoBehaviour
 {
  [Serializable] public sealed class Candidate { public string Label; public Mesh Mesh; public Transform[] Bones; public Transform Root; }
  public Candidate[] Candidates; public SkinnedMeshRenderer Body; public int Selected;
  public void Select(int index){Selected=index;var c=Candidates[index];Body.sharedMesh=c.Mesh;Body.bones=c.Bones;Body.rootBone=c.Root;Body.localBounds=c.Mesh.bounds;}
 }
}
