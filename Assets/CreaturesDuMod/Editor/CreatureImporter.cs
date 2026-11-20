// Construit le prefab d'une créature du mod Explosive Fishing à partir de <Nom>.creature.json (exporté par exporter_unity.py) :
// une pièce = un GameObject posé sur son pivot (on peut l'animer en la faisant tourner), maillages, matériau URP aux pixels nets,
// clips d'animation tirés des poses du mod et un Animator Controller prêt à l'emploi.
// Le prefab se construit tout seul s'il manque ; menu Tools > Créatures du mod > Reconstruire pour le refaire.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class CreatureImporter
{
    [System.Serializable] class Node { public string name; public int parent; public float[] pos, rot, scale, vertices, normals, uvs; public int[] triangles; }
    [System.Serializable] class Clip { public string name; public bool loop; public float[] times, frames; }
    [System.Serializable] class Data { public string name, texture, emission; public Node[] nodes; public Clip[] clips; }

    static CreatureImporter()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (var json in FindJsons())
                if (!File.Exists(PrefabPath(json)) || File.GetLastWriteTimeUtc(json) > File.GetLastWriteTimeUtc(PrefabPath(json))) Build(json);
        };
    }

    [MenuItem("Tools/Créatures du mod/Reconstruire")]
    static void RebuildAll()
    {
        foreach (var json in FindJsons()) Build(json);
    }

    static IEnumerable<string> FindJsons()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:TextAsset"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith(".creature.json")) yield return p;
        }
    }

    static string PrefabPath(string json) => json.Replace(".creature.json", ".prefab");

    static void Build(string jsonPath)
    {
        var dir = Path.GetDirectoryName(jsonPath).Replace('\\', '/');
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(jsonPath));
        var n = data.name;

        // Textures : pixels nets, sans compression ni mipmaps, comme dans le jeu
        var tex = ImportPixelTexture($"{dir}/{data.texture}");
        var emi = string.IsNullOrEmpty(data.emission) ? null : ImportPixelTexture($"{dir}/{data.emission}");

        foreach (var old in new[] { $"{dir}/{n}_Maillages.asset", $"{dir}/{n}.mat", $"{dir}/{n}.controller" })
            AssetDatabase.DeleteAsset(old);
        AssetDatabase.DeleteAsset($"{dir}/Animations");

        var mat = MakeMaterial(tex, emi);
        mat.name = n;
        AssetDatabase.CreateAsset(mat, $"{dir}/{n}.mat");

        // Hiérarchie des pièces
        var top = new GameObject(n);
        var gos = new GameObject[data.nodes.Length];
        var paths = new string[data.nodes.Length];
        var meshes = new List<Mesh>();
        for (int i = 0; i < data.nodes.Length; i++)
        {
            var nd = data.nodes[i];
            var go = new GameObject(nd.name);
            var parent = nd.parent < 0 ? top.transform : gos[nd.parent].transform;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = V3(nd.pos, 0);
            go.transform.localRotation = new Quaternion(nd.rot[0], nd.rot[1], nd.rot[2], nd.rot[3]);
            go.transform.localScale = V3(nd.scale, 0);
            gos[i] = go;
            paths[i] = nd.parent < 0 ? nd.name : paths[nd.parent] + "/" + nd.name;
            if (nd.triangles == null || nd.triangles.Length == 0) continue;
            var mesh = new Mesh { name = $"{n}_{paths[i].Replace('/', '.')}" };
            int vc = nd.vertices.Length / 3;
            var v = new Vector3[vc]; var no = new Vector3[vc]; var uv = new Vector2[vc];
            for (int k = 0; k < vc; k++) { v[k] = V3(nd.vertices, k * 3); no[k] = V3(nd.normals, k * 3); uv[k] = new Vector2(nd.uvs[k * 2], nd.uvs[k * 2 + 1]); }
            mesh.vertices = v; mesh.normals = no; mesh.uv = uv; mesh.triangles = nd.triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            meshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }
        var meshPath = $"{dir}/{n}_Maillages.asset";
        AssetDatabase.CreateAsset(meshes[0], meshPath);
        for (int i = 1; i < meshes.Count; i++) AssetDatabase.AddObjectToAsset(meshes[i], meshPath);

        // Clips et Animator Controller
        AnimatorController ctrl = null;
        if (data.clips != null && data.clips.Length > 0)
        {
            AssetDatabase.CreateFolder(dir, "Animations");
            ctrl = AnimatorController.CreateAnimatorControllerAtPath($"{dir}/{n}.controller");
            var sm = ctrl.layers[0].stateMachine;
            AnimatorState idle = null;
            var states = new Dictionary<string, (AnimatorState st, bool loop)>();
            foreach (var c in data.clips)
            {
                var clip = MakeClip(c, data.nodes.Length, paths);
                AssetDatabase.CreateAsset(clip, $"{dir}/Animations/{n}_{c.name}.anim");
                var st = sm.AddState(c.name);
                st.motion = clip;
                states[c.name] = (st, c.loop);
                if (idle == null) { idle = st; sm.defaultState = st; }
            }
            foreach (var kv in states)
            {
                if (kv.Value.st == idle) continue;
                if (kv.Value.loop)
                {
                    // Boucles (marche, charge) : un booléen du même nom
                    ctrl.AddParameter(kv.Key, AnimatorControllerParameterType.Bool);
                    var go = idle.AddTransition(kv.Value.st); go.hasExitTime = false; go.duration = 0.2f;
                    go.AddCondition(AnimatorConditionMode.If, 0, kv.Key);
                    var back = kv.Value.st.AddTransition(idle); back.hasExitTime = false; back.duration = 0.2f;
                    back.AddCondition(AnimatorConditionMode.IfNot, 0, kv.Key);
                }
                else
                {
                    // Gestes (frappe, rugissement...) : un déclencheur du même nom, retour au repos à la fin du geste
                    ctrl.AddParameter(kv.Key, AnimatorControllerParameterType.Trigger);
                    var go = sm.AddAnyStateTransition(kv.Value.st); go.hasExitTime = false; go.duration = 0.1f; go.canTransitionToSelf = false;
                    go.AddCondition(AnimatorConditionMode.If, 0, kv.Key);
                    var back = kv.Value.st.AddTransition(idle); back.hasExitTime = true; back.exitTime = 1f; back.duration = 0.15f;
                }
            }
            var anim = top.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
        }

        PrefabUtility.SaveAsPrefabAsset(top, PrefabPath(jsonPath));
        Object.DestroyImmediate(top);
        foreach (var o in new Object[] { mat, AssetDatabase.LoadAssetAtPath<Object>(meshPath), ctrl }) if (o != null) AssetDatabase.SaveAssetIfDirty(o);
        Debug.Log($"[Créatures du mod] {n} : prefab {PrefabPath(jsonPath)}, {data.nodes.Length} pièces, {meshes.Count} maillages, {data.clips?.Length ?? 0} clips, shader {mat.shader.name}");
        try { Preview(PrefabPath(jsonPath), n); } catch (System.Exception e) { Debug.LogWarning($"[Créatures du mod] aperçu impossible : {e.Message}"); }
    }

    static Texture2D ImportPixelTexture(string path)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material MakeMaterial(Texture2D tex, Texture2D emi)
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(urp != null ? urp : Shader.Find("Standard"));
        mat.mainTexture = tex;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        // Découpe nette des pixels transparents, faces visibles des deux côtés (pans de poils sans épaisseur)
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 1f);
        mat.SetFloat("_Cutoff", 0.5f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)RenderQueue.AlphaTest;
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.05f);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
        if (emi != null)
        {
            mat.SetTexture("_EmissionMap", emi);
            mat.SetColor("_EmissionColor", Color.white * 1.5f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return mat;
    }

    static AnimationClip MakeClip(Clip c, int count, string[] paths)
    {
        var clip = new AnimationClip { name = c.name, frameRate = 30 };
        int keys = c.times.Length;
        string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" };
        for (int i = 0; i < count; i++)
        {
            bool moves = false;
            for (int ch = 0; ch < 7 && !moves; ch++)
                for (int k = 1; k < keys; k++)
                    if (Mathf.Abs(c.frames[(k * count + i) * 7 + ch] - c.frames[i * 7 + ch]) > 1e-5f) { moves = true; break; }
            if (!moves) continue;
            for (int ch = 0; ch < 7; ch++)
            {
                var curve = new AnimationCurve();
                for (int k = 0; k < keys; k++) curve.AddKey(c.times[k], c.frames[(k * count + i) * 7 + ch]);
                for (int k = 0; k < keys; k++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(paths[i], typeof(Transform), props[ch]), curve);
            }
        }
        clip.EnsureQuaternionContinuity();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = c.loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    static Vector3 V3(float[] a, int i) => new Vector3(a[i], a[i + 1], a[i + 2]);

    // Rendus de contrôle dans Temp/<Nom>_apercu_<pose>.png (trois-quarts avant), sans toucher à la scène ouverte
    static void Preview(string prefabPath, string n)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var shots = new List<(string name, AnimationClip clip, float t)> { ("repos", null, 0f) };
        foreach (var (pose, t) in new[] { ("Rugissement", 1.0f), ("Frappe", 0.9f) })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Path.GetDirectoryName(prefabPath)}/Animations/{n}_{pose}.anim");
            if (clip != null) shots.Add((pose.ToLowerInvariant(), clip, t));
        }
        foreach (var shot in shots)
        {
            var pru = new PreviewRenderUtility();
            try
            {
                var go = pru.InstantiatePrefabInScene(prefab);
                if (shot.clip != null) shot.clip.SampleAnimation(go, shot.t);
                var b = new Bounds(go.transform.position, Vector3.zero);
                foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                var cam = pru.camera;
                cam.fieldOfView = 30;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 200f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.22f, 0.24f, 0.30f);
                var dirv = Quaternion.Euler(15, 215, 0) * Vector3.forward;
                cam.transform.position = b.center - dirv * b.extents.magnitude * 4.2f;
                cam.transform.LookAt(b.center);
                pru.lights[0].intensity = 1.3f;
                pru.lights[0].transform.rotation = Quaternion.Euler(45, 150, 0);
                pru.lights[1].intensity = 0.6f;
                pru.lights[1].transform.rotation = Quaternion.Euler(20, -60, 0);
                pru.ambientColor = new Color(0.45f, 0.47f, 0.52f);
                pru.BeginStaticPreview(new Rect(0, 0, 900, 600));
                pru.Render(true, false);
                var img = pru.EndStaticPreview();
                var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", $"{n}_apercu_{shot.name}.png"));
                File.WriteAllBytes(outPath, img.EncodeToPNG());
                Object.DestroyImmediate(img);
                Debug.Log($"[Créatures du mod] aperçu : {outPath}");
            }
            finally { pru.Cleanup(); }
        }
    }
}
