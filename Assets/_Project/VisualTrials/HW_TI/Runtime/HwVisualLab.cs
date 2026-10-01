using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
namespace RPG.VisualTrial
{
 public sealed class HwVisualLab:MonoBehaviour
 {
  public GameObject Character;public Material Corrected,ProviderGloss;public bool AutoProof;
  readonly List<GameObject> units=new List<GameObject>();Camera view;int count=1,lod=0;string action="Idle",cameraMode="Close";bool raw;
  public IReadOnlyList<GameObject> Units=>units;
  public void Start(){var cameraGo=new GameObject("Trial Camera");view=cameraGo.AddComponent<Camera>();view.backgroundColor=new Color(.12f,.14f,.16f);view.clearFlags=CameraClearFlags.SolidColor;view.nearClipPlane=.05f;view.farClipPlane=100;view.rect=new Rect(0,0,.75f,1);
   var clear=new GameObject("Full viewport clear").AddComponent<Camera>();clear.depth=-10;clear.cullingMask=0;clear.clearFlags=CameraClearFlags.SolidColor;clear.backgroundColor=new Color(.08f,.09f,.10f);
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.48f,.48f);var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(45,-35,0);light.shadows=LightShadows.Soft;
   var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.transform.rotation=Quaternion.Euler(65,140,0);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="23 x 17 field footprint";floor.transform.position=new Vector3(0,-.06f,0);floor.transform.localScale=new Vector3(23,.1f,17);var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=new Color(.19f,.22f,.20f);floor.GetComponent<Renderer>().material=m;
   var lineMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));lineMat.color=new Color(.3f,.33f,.3f);for(int x=-11;x<=11;x++)Line(new Vector3(x,0,-8.5f),new Vector3(x,0,8.5f),lineMat);for(int z=-8;z<=8;z++)Line(new Vector3(-11.5f,0,z),new Vector3(11.5f,0,z),lineMat);
   SetCount(1);SetCamera("Close");if(AutoProof)StartCoroutine(Proof());
  }
  void Line(Vector3 a,Vector3 b,Material mat){var l=new GameObject("field grid").AddComponent<LineRenderer>();l.positionCount=2;l.SetPositions(new[]{a,b});l.startWidth=l.endWidth=.012f;l.sharedMaterial=mat;}
  public void SetCount(int n){foreach(var u in units)Destroy(u);units.Clear();count=n;for(int i=0;i<n;i++){var u=Instantiate(Character);u.name="Trial warrior "+(i+1);u.transform.position=n==1?Vector3.zero:new Vector3((i%6-2.5f)*1.6f,0,(i/6-1)*2);units.Add(u);}SetLod(lod);SetMaterial(raw);Act("Idle");}
  public void SetLod(int n){lod=n;foreach(var u in units)u.GetComponent<LODGroup>().ForceLOD(n);}
  public void SetMaterial(bool provider){raw=provider;foreach(var u in units)foreach(var s in u.GetComponentsInChildren<SkinnedMeshRenderer>())s.sharedMaterial=provider?ProviderGloss:Corrected;}
  public void Act(string name){action=name;foreach(var u in units){var a=u.GetComponent<Animator>();a.speed=1;a.CrossFadeInFixedTime(name,.10f);}}
  public void SetCamera(string mode){cameraMode=mode;view.orthographic=true;if(mode=="Close"){view.orthographicSize=1.2f;view.transform.position=new Vector3(2.2f,1.45f,4.5f);view.transform.LookAt(new Vector3(0,.9f,0));}
   else{view.transform.position=new Vector3(0,20,0);view.transform.rotation=Quaternion.Euler(90,0,0);view.orthographicSize=Mathf.Max(18f/1.55f,25.2f/(2*view.aspect))*(mode=="Far"?1.35f:1);}}
  void OnGUI(){var rect=new Rect(Screen.width*.76f,10,Screen.width*.235f,Screen.height-20);GUILayout.BeginArea(rect,GUI.skin.box);GUILayout.Label("HW_TI MESHY TRIAL 01\nDeveloper Visual Asset Lab\nNOT production art / no combat timing");GUILayout.Label("One body + shared Humanoid Avatar\nTemporary sword / shield, no fingers");GUILayout.Label("View: "+cameraMode+" | "+count+" actors | LOD"+lod+"\n"+action+" | "+(raw?"RAW GLOSS COMPARISON":"RESTORED REMESH PBR"));
   foreach(var s in new[]{"Idle","Walk","Attack","Hit","Death"})if(GUILayout.Button(s,GUILayout.Height(30)))Act(s);
   GUILayout.Label("Camera");foreach(var s in new[]{"Close","Tactical","Far"})if(GUILayout.Button(s))SetCamera(s);
   GUILayout.BeginHorizontal();foreach(int n in new[]{1,9,18})if(GUILayout.Button(n+" units"))SetCount(n);GUILayout.EndHorizontal();GUILayout.BeginHorizontal();for(int i=0;i<3;i++)if(GUILayout.Button("LOD"+i))SetLod(i);GUILayout.EndHorizontal();
   if(GUILayout.Button(raw?"Corrected PBR":"Compare provider gloss"))SetMaterial(!raw);GUILayout.Label("102k / 50k / 25k candidates\nTactical: exact existing overhead 23×17 fit\nIn-place locomotion; gameplay unchanged");GUILayout.EndArea();}
  [Serializable]public class Measurement{public int characters,lod,triangles,vertices,storedSkinnedRenderers,activeSkinnedRenderers,materials,drawCalls;public float meanFrameMs,p95FrameMs;public long allocatedBytes;}
  [Serializable]public class Evidence{public string persistentPath=Application.persistentDataPath;public string device=SystemInfo.graphicsDeviceName;public string cpu=SystemInfo.processorType;public int ramMiB=SystemInfo.systemMemorySize;public string environment="Unity Editor real Play Mode; wall frame time includes editor/vsync; not GPU benchmark";public List<Measurement> measurements=new List<Measurement>();public bool completed;}
  IEnumerator Shot(string name){yield return null;
#if UNITY_EDITOR
   while(UnityEditor.ShaderUtil.anythingCompiling)yield return null;
#endif
   yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("/private/tmp/hw-ti-engine/results/"+name+".png");yield return null;}
  public IEnumerator Proof(){Directory.CreateDirectory("/private/tmp/hw-ti-engine/results");yield return new WaitForSeconds(1);SetMaterial(true);yield return Shot("01-raw-gloss-close");SetMaterial(false);yield return Shot("02-corrected-close");
   foreach(var name in new[]{"Idle","Walk","Attack","Hit","Death"}){Act(name);yield return new WaitForSeconds(name=="Death"?2.8f:.6f);SetCamera("Close");yield return Shot("close-"+name);SetCamera("Tactical");yield return Shot("tactical-"+name);}
   Act("Idle");foreach(var mode in new[]{"Close","Tactical","Far"}){SetCamera(mode);for(int i=0;i<3;i++){SetLod(i);yield return null;yield return Shot(mode+"-LOD"+i);}}
   var evidence=new Evidence();SetCamera("Tactical");foreach(int n in new[]{1,9,18})for(int l=0;l<3;l++){SetCount(n);SetLod(l);Act("Walk");for(int f=0;f<40;f++)yield return null;var times=new List<float>();for(int f=0;f<120;f++){yield return null;times.Add(Time.unscaledDeltaTime*1000);}times.Sort();var sm=units[0].GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(s=>s.sharedMesh.triangles.Length).ToArray();var m=new Measurement{characters=n,lod=l,triangles=sm[l].sharedMesh.triangles.Length/3*n,vertices=sm[l].sharedMesh.vertexCount*n,storedSkinnedRenderers=n*3,activeSkinnedRenderers=n,materials=units[0].GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Count(),meanFrameMs=times.Average(),p95FrameMs=times[(int)(times.Count*.95f)],allocatedBytes=Profiler.GetTotalAllocatedMemoryLong()};
#if UNITY_EDITOR
    m.drawCalls=UnityEditor.UnityStats.drawCalls;
#endif
    evidence.measurements.Add(m);yield return Shot("field-"+n+"-LOD"+l);File.WriteAllText("/private/tmp/hw-ti-engine/results/performance.json",JsonUtility.ToJson(evidence,true));}
   evidence.completed=true;File.WriteAllText("/private/tmp/hw-ti-engine/results/performance.json",JsonUtility.ToJson(evidence,true));Debug.Log("HW_TI_PROOF_COMPLETE");
  }
 }
}
