using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using AnchorDefense;

[InitializeOnLoad] public static class DisintegrationPreview
{
    static int frames;
    static DisintegrationPreview() { EditorApplication.update += Tick; }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/AnchorDefense/Scenes/Gameplay.unity");
        SessionState.SetBool("DisintegrationCapture", true);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool("DisintegrationCapture", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        foreach (var companion in Object.FindObjectsOfType<AICompanionService>()) companion.enabled = false;
        foreach (var spawner in Object.FindObjectsOfType<EndlessEnemySpawner>()) spawner.enabled = false;
        if (++frames < 30) return;
        SessionState.SetBool("DisintegrationCapture", false);
        try { Render(); EditorApplication.Exit(0); }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    static void Render()
    {
        var camera = Camera.main;
        camera.aspect = 16f / 9f;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>()) canvas.gameObject.SetActive(false);
        var shader = Shader.Find("AnchorDefense/AICommandSignal");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new System.Exception("Signal shader failed compilation.");
        var config = AssetDatabase.LoadAssetAtPath<AICommandConfig>("Assets/AnchorDefense/Configs/AI/AICommandConfig.asset");
        var strike = AssetDatabase.LoadAssetAtPath<ActiveSkillDefinition>("Assets/AnchorDefense/Configs/AI/Skills/AnchorStrike.asset");
        var grid = Object.FindObjectOfType<CubeZoneGridController>();
        var volumes = Object.FindObjectsOfType<CubeZoneVolume>();
        CubeZoneVolume selected = null;
        float best = float.MaxValue;
        foreach (var v in volumes)
        {
            var vp = camera.WorldToViewportPoint(v.transform.position);
            if (vp.z <= 0) continue;
            float d = Vector2.Distance(new Vector2(vp.x, vp.y), new Vector2(0.72f, 0.5f));
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                var corner = camera.WorldToViewportPoint(v.transform.position + new Vector3(x,y,z) * grid.Config.CubeSize * 0.55f);
                if (corner.x < 0.03f || corner.x > 0.97f || corner.y < 0.03f || corner.y > 0.97f) d += 2;
            }
            if (d < best) { selected = v; best = d; }
        }
        // Preview camera only: a closer angle makes all four animation phases readable.
        selected = grid.GetCubeById(7);
        Vector3 focus = Vector3.Lerp(Vector3.zero, selected.transform.position, 0.7f);
        camera.transform.position = focus + new Vector3(0, 10.5f, -22) * 0.95f;
        camera.transform.LookAt(focus);
        var marker = Object.Instantiate(config.FieldMarkerPrefab).GetComponent<AICommandFieldMarker>();
        marker.transform.position = selected != null ? selected.transform.position : new Vector3(8, 0, 0);
        marker.Initialize(new[] { strike }, grid.Config.CubeSize, 0, camera, 6);
        var folder = Path.GetFullPath("ValidationArtifacts/DisintegrationVfx");
        Directory.CreateDirectory(folder);
        Debug.Log("VFX preview zone: " + selected.name + " position " + marker.transform.position);
        for (int frame = 0; frame < 98; frame++)
        {
            float age = frame / 15f;
            typeof(AICommandFieldMarker).GetField("elapsed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(marker, age);
            typeof(AICommandFieldMarker).GetMethod("RefreshPresentation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(marker, null);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(folder, "frame-" + frame.ToString("000") + ".png"), tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
        if (ShaderUtil.ShaderHasError(shader)) throw new System.Exception("Signal shader failed compilation during rendering.");
        Debug.Log("DISINTEGRATION_CAPTURE_OK");
    }
}
