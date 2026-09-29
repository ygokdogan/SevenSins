using Player.Movement;
using UnityEditor;
using UnityEngine;

public static class DashParticleBaker
{
    [MenuItem("Tools/Western Samurai/Validate Dash Particles")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Exit Play Mode before validation.");
        var effect = UnityEngine.Object.FindFirstObjectByType<DashVisualEffects>();
        var serialized = new SerializedObject(effect);
        var system = (ParticleSystem)serialized.FindProperty("speedLines").objectReferenceValue;
        var camera = (Camera)serialized.FindProperty("viewCamera").objectReferenceValue;
        var buffer = new ParticleSystem.Particle[128];
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var previousPosition = system.transform.localPosition;
        var randomState = Random.state;
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            Vector3[] directions = { camera.transform.forward, camera.transform.right, -camera.transform.forward };
            string[] names = { "forward", "right", "backward" };
            for (int test = 0; test < directions.Length; test++)
            {
                system.Clear();
                Random.InitState(4321);
                Invoke(effect, "BeginDash", directions[test]);
                Invoke(effect, "LateUpdate");
                system.Simulate(.06f, false, false);
                int count = system.GetParticles(buffer);
                if (count == 0) throw new System.Exception("Dash emitted no particles.");
                for (int i = 0; i < count; i++)
                {
                    if (test == 1 && buffer[i].velocity.x >= 0)
                        throw new System.Exception("Right dash must flow left.");
                    float dot = Vector3.Dot(buffer[i].position, buffer[i].velocity);
                    if ((test == 0 && dot <= 0) || (test == 2 && dot >= 0))
                        throw new System.Exception("Incorrect radial dash flow.");
                }
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes("Temp/Dash-" + names[test] + ".png", texture.EncodeToPNG());
                Invoke(effect, "EndDash");
                system.Simulate(1, false, false);
                if (system.particleCount != 0) throw new System.Exception("Dash particles did not expire.");
            }
            Debug.Log("Dash particles validated: forward, right, backward, lifetime cleanup.");
        }
        finally
        {
            Invoke(effect, "EndDash");
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            system.transform.localPosition = previousPosition;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    private static void Invoke(DashVisualEffects effect, string name, params object[] arguments) =>
        typeof(DashVisualEffects).GetMethod(name, System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(effect, arguments);

    public static void Bake(DashVisualEffects effect)
    {
        Camera camera = effect.GetComponentInChildren<Camera>(true);
        if (camera == null) throw new System.InvalidOperationException("Dash requires a player camera.");
        var serialized = new SerializedObject(effect);
        var system = serialized.FindProperty("speedLines").objectReferenceValue as ParticleSystem;
        // Migrate the original defaults once; preserve subsequent Inspector tuning.
        if (serialized.FindProperty("width").vector2Value == new Vector2(.006f, .014f))
            serialized.FindProperty("width").vector2Value = new Vector2(.012f, .028f);
        if (serialized.FindProperty("length").vector2Value == new Vector2(.22f, .55f))
            serialized.FindProperty("length").vector2Value = new Vector2(.352f, .88f);
        if (system == null)
        {
            var go = new GameObject("Dash Speed Lines", typeof(ParticleSystem));
            go.transform.SetParent(camera.transform, false);
            system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0;
            main.startSize3D = true;
            main.maxParticles = 128;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .12f),
                    new GradientAlphaKey(.8f, .55f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.maxParticleSize = 1;
            const string path = "Assets/Generated/RuntimeSetup/DashSpeedLines.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("WesternSamurai/DashSpeedLines");
                if (shader == null) throw new System.InvalidOperationException("Dash speedline shader missing.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            renderer.sharedMaterial = material;
        }
        serialized.FindProperty("viewCamera").objectReferenceValue = camera;
        serialized.FindProperty("speedLines").objectReferenceValue = system;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effect);
    }
}
