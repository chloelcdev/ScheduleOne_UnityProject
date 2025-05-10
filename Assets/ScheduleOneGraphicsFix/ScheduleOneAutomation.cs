// Assets/Editor/ScheduleOneAutomation.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Experimental.Rendering; // For GraphicsFormat

public static class ScheduleOneAutomation
{
    // Shader replacement map: Key is part of OLD shader name, Value is NEW Shader object
    // !!! --- THIS MAP IS THE PRIMARY THING YOU WILL BE EDITING --- !!!
    // !!! --- Use the [ShaderInspect] logs to determine the correct KEYS for your OLD shaders --- !!!
    // !!! --- Ensure Shader.Find() paths for NEW shaders are correct (use internal shader name) --- !!!
    static readonly Dictionary<string, Shader> shaderMap = new Dictionary<string, Shader>()
    {
        // --- Current/Example Entries (EXPAND THIS SIGNIFICANTLY) ---
        { "Universal Render Pipeline_Lit",              Shader.Find("Universal Render Pipeline/Lit") },
        { "Standard",                                   Shader.Find("Universal Render Pipeline/Lit") },
        { "Legacy Shaders/Diffuse",                     Shader.Find("Universal Render Pipeline/Simple Lit") },
        { "SimpleLit",                                  Shader.Find("Universal Render Pipeline/Simple Lit") }, // Good if "SimpleLit" is in the old name
        { "GlassShader",                                Shader.Find("Universal Render Pipeline/Lit") }, // This matched "Shader Graphs/GlassShader"
        { "Stylized Water 2_Standard (Tessellation)",   Shader.Find("Universal Render Pipeline/Lit") },
        { "Stylized Water 2/Standard",                  Shader.Find("Universal Render Pipeline/Lit") }, // For non-tessellated variant if it exists by this name
        
        // For your custom shaders - ensure Shader.Find uses the INTERNAL name from the .shader file
        // Example: if WorldspaceUV_New.shader contains 'Shader "MyShaders/WorldspaceUV_New"'
        // then use Shader.Find("MyShaders/WorldspaceUV_New")
        { "WorldspaceUV_New",                           Shader.Find("Assets/Shader/Fix/WorldspaceUV_New") }, // Placeholder: FIX THIS Shader.Find path
        { "RoofShader",                                 Shader.Find("Assets/Shader/Fix/WorldspaceUV_New") }, // Placeholder: FIX THIS Shader.Find path

        // Add new entries based on your [ShaderInspect] logs, for example:
        // { "Funly/Sky Studio/Skybox/3D Standard", Shader.Find("Skybox/Panoramic") },
        // { "Shader Graphs/6D Lighting Shader URP", Shader.Find("Universal Render Pipeline/Particles/Unlit") }, // Or Lit
        // { "M_BlendMaster", Shader.Find("Universal Render Pipeline/Lit") },
        // { "GUI/Text Shader", Shader.Find("TextMeshPro/Mobile/Distance Field") }, // Or another URP text shader
        { "Particles/Standard Unlit", Shader.Find("Universal Render Pipeline/Particles/Unlit") }
        // etc.
    };

    // Paths to fix terrain (relative to Assets folder)
    const string GrassDetailTexturePath = "Assets/Texture2D/GrassField.png";
    const string GrassTexturePath = "Assets/Texture2D/Terrain grass.png";
    static readonly string[] TreePrefabPaths = {
        "Assets/GameObject/Fir L - Variant 1.prefab",
        "Assets/GameObject/Fir L - Variant 2.prefab"
    };

    // Static constructor to verify shader map on editor load/script compilation
    static ScheduleOneAutomation()
    {
        Debug.Log("[ScheduleOneAutomation] Initializing: Verifying replacement shaders in shaderMap...");
        var tempMap = new Dictionary<string, Shader>(shaderMap);

        foreach (var kvp in tempMap)
        {
            if (kvp.Value == null)
            {
                string attemptedShaderFindPath = GetShaderFindByPathForKey_Helper(kvp.Key);
                Debug.LogError($"[ScheduleOneAutomation] CRITICAL VERIFICATION FAILED: Replacement shader for OLD shader key '{kvp.Key}' could NOT be found. Attempted Shader.Find for '{attemptedShaderFindPath}'. This rule will be skipped or cause errors if the value remains null.");
            }
            else
            {
                Debug.Log($"[ScheduleOneAutomation] Verified: OLD shader key '{kvp.Key}' will be replaced by NEW shader '{kvp.Value.name}'.");
            }
        }
        Debug.Log("[ScheduleOneAutomation] Shader map verification complete.");
    }

    static string GetShaderFindByPathForKey_Helper(string key)
    {
        // This helper is for logging purposes to guess what Shader.Find might have tried.
        // It needs to be manually updated if you change how you determine the Shader.Find paths in the map.
        if (shaderMap.TryGetValue(key, out Shader shader) && shader != null) return $"'{shader.name}' (already found, this is unexpected in error log)"; // Should not happen if value is null

        // Guess common patterns based on your current map structure (this part is indicative)
        if (key.Contains("Universal Render Pipeline") || key.Contains("SimpleLit") || key.Contains("Lit")) return key; // Assuming key might BE the find path
        if (key.Contains("GlassShader")) return "Universal Render Pipeline/Lit";
        if (key.Contains("Stylized Water")) return "Universal Render Pipeline/Lit";
        if (key.Contains("WorldspaceUV_New")) return "Assets/Shader/Fix/WorldspaceUV_New OR its internal shader name";
        if (key.Contains("RoofShader")) return "Assets/Shader/Fix/WorldspaceUV_New OR its internal shader name";
        if (key.Contains("Standard")) return "Universal Render Pipeline/Lit";
        // Add more specific guesses here if it helps your debugging
        return "UNKNOWN_SHADER_FIND_PATH (Update GetShaderFindByPathForKey_Helper or check Shader.Find call in map)";
    }

    // Main method
    public static void FixShadersInAll()
    {
        string[] searchInFolders = new string[] { "Assets" };
        int updatedMaterialCount = 0;

        Debug.Log("--- [FixAll] Starting Full Project Fix (Targeting 'Assets' Folder) ---");

        // 1) MATERIALS
        string[] matGuids = AssetDatabase.FindAssets("t:Material", searchInFolders);
        Debug.Log($"[FixAll] Found {matGuids.Length} materials in 'Assets' folder to check.");
        foreach (var guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) continue;

            // --- TEMPORARY DEBUG LOG TO INSPECT SHADER NAMES ---
            // This is the line that prints all shader names. Review console output from this.
            Debug.Log($"[ShaderInspect] Material: '{mat.name}' (Path: {path}), CURRENT SHADER NAME: '{mat.shader.name}'");
            // --- END TEMPORARY DEBUG LOG ---

            foreach (var kv in shaderMap)
            {
                if (mat.shader.name.Contains(kv.Key))
                {
                    if (kv.Value == null) { /* Error already logged by static constructor */ continue; }
                    if (mat.shader == kv.Value) continue;

                    Debug.Log($"[FixAll] Updating shader for material '{mat.name}' (Path: {path}). Old: '{mat.shader.name}', New: '{kv.Value.name}'");
                    mat.shader = kv.Value;

                    if (kv.Key.Contains("GlassShader") || kv.Key.Contains("Stylized Water"))
                    {
                        if (mat.HasProperty("_SurfaceType")) mat.SetFloat("_SurfaceType", 1.0f);
                        if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0.0f);
                        Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                        c.a = 0.5f;
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                        Debug.Log($"[FixAll] Adjusted material '{mat.name}' for transparency.");
                    }
                    EditorUtility.SetDirty(mat);
                    updatedMaterialCount++;
                    break;
                }
            }
        }
        Debug.Log($"[FixAll] Material shader swap complete. Updated {updatedMaterialCount} materials in 'Assets'.");

        // 2) SCENES
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", searchInFolders);
        Debug.Log($"[FixAll] Found {sceneGuids.Length} scenes in 'Assets' folder to process.");
        string originalActiveScenePath = EditorSceneManager.GetActiveScene().path;

        foreach (var sg in sceneGuids)
        {
            var sp = AssetDatabase.GUIDToAssetPath(sg);
            Debug.Log($"[FixAll] Opening scene: {sp}");
            Scene s = EditorSceneManager.OpenScene(sp, OpenSceneMode.Single);
            if (!s.IsValid() || !s.isLoaded) { Debug.LogError($"[FixAll] Failed to open: {sp}"); continue; }

            SwapShadersInScene(s);
            FixTerrainInScene(s);

            Debug.Log($"[FixAll] Marking scene '{s.name}' dirty and saving.");
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
        }
        if (!string.IsNullOrEmpty(originalActiveScenePath) && File.Exists(originalActiveScenePath))
        {
            try { EditorSceneManager.OpenScene(originalActiveScenePath, OpenSceneMode.Single); }
            catch (System.Exception e) { Debug.LogWarning($"[FixAll] Could not reopen original scene '{originalActiveScenePath}': {e.Message}"); }
        }
        Debug.Log("[FixAll] Scene processing complete.");

        // 3) PREFABS
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchInFolders);
        Debug.Log($"[FixAll] Found {prefabGuids.Length} prefabs in 'Assets' folder to process.");
        int prefabsModifiedCount = 0;
        foreach (var pg in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(pg);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            bool prefabNeedsSaving = false;
            foreach (var r in renderers)
            {
                Material[] sharedMats = r.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material mat = sharedMats[i];
                    if (mat == null || mat.shader == null) continue;

                    foreach (var kv in shaderMap)
                    {
                        if (mat.shader.name.Contains(kv.Key))
                        {
                            if (kv.Value == null) continue;
                            if (mat.shader == kv.Value) continue;

                            mat.shader = kv.Value;
                            if (kv.Key.Contains("GlassShader") || kv.Key.Contains("Stylized Water"))
                            {
                                if (mat.HasProperty("_SurfaceType")) mat.SetFloat("_SurfaceType", 1.0f);
                                if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0.0f);
                                Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                                c.a = 0.5f;
                                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                            }
                            EditorUtility.SetDirty(mat);
                            EditorUtility.SetDirty(r);
                            prefabNeedsSaving = true;
                            break;
                        }
                    }
                }
            }
            if (prefabNeedsSaving) { EditorUtility.SetDirty(prefab); prefabsModifiedCount++; }
        }
        Debug.Log($"[FixAll] Prefab material shader processing complete. Dirtied {prefabsModifiedCount} prefabs.");

        // 4) RENDERTEXTURE
        FixWeatherRenderTextureFormat();

        AssetDatabase.SaveAssets();
        Debug.Log("✱✱✱ [FixAll] ALL PROCESSING COMPLETE! (Assets folder only) ✱✱✱");
    }

    static void SwapShadersInScene(Scene scene)
    {
        // Debug.Log($"[SwapShadersInScene] Processing scene: {scene.name}"); 
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                ForEachMaterial(r.sharedMaterials, mat =>
                {
                    if (mat.shader == null) return;
                    foreach (var kv in shaderMap)
                    {
                        if (mat.shader.name.Contains(kv.Key))
                        {
                            if (kv.Value == null) continue;
                            if (mat.shader == kv.Value) continue;
                            mat.shader = kv.Value;
                            if (kv.Key.Contains("GlassShader") || kv.Key.Contains("Stylized Water"))
                            {
                                if (mat.HasProperty("_SurfaceType")) mat.SetFloat("_SurfaceType", 1.0f);
                                if (mat.HasProperty("_BlendMode")) mat.SetFloat("_BlendMode", 0.0f);
                                Color c = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                                c.a = 0.5f;
                                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                            }
                            EditorUtility.SetDirty(mat);
                            break;
                        }
                    }
                });
            }
        }
    }

    static void ForEachMaterial(Material[] mats, System.Action<Material> action)
    {
        if (mats == null) return;
        foreach (var m in mats) { if (m != null) action(m); }
    }

    static void FixTerrainInScene(Scene scene)
    {
        // Debug.Log($"[FixTerrainInScene] Attempting to fix terrain in scene: {scene.name}");
        GameObject terrainGO = GameObject.Find("Map/Container/Main Terrain");
        if (terrainGO == null) { return; }
        Terrain terrain = terrainGO.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null) { return; }

        TerrainData td = terrain.terrainData;
        bool terrainDataWasChanged = false;

        Texture2D grassDetailTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassDetailTexturePath);
        if (grassDetailTexture != null)
        {
            DetailPrototype[] currentDetailPrototypes = td.detailPrototypes;
            bool detailsWereUpdated = false;
            for (int i = 0; i < currentDetailPrototypes.Length; i++)
            {
                if (currentDetailPrototypes[i].prototypeTexture == null)
                {
                    currentDetailPrototypes[i].prototypeTexture = grassDetailTexture;
                    detailsWereUpdated = true;
                }
            }
            if (detailsWereUpdated) { td.detailPrototypes = currentDetailPrototypes; terrainDataWasChanged = true; }
        }
        else { Debug.LogWarning($"[FixTerrainInScene] Grass detail texture not found at: {GrassDetailTexturePath}"); }

        TreePrototype[] currentTreePrototypes = td.treePrototypes;
        bool treesWereUpdated = false;
        List<TreePrototype> newTreeProtoList = new List<TreePrototype>(currentTreePrototypes);

        for (int i = 0; i < TreePrefabPaths.Length; i++)
        {
            GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TreePrefabPaths[i]);
            if (treePrefab == null) { Debug.LogWarning($"[FixTerrainInScene] Tree prefab not found at: {TreePrefabPaths[i]}"); continue; }
            if (i < newTreeProtoList.Count)
            {
                if (newTreeProtoList[i].prefab != treePrefab) { newTreeProtoList[i].prefab = treePrefab; treesWereUpdated = true; }
            }
            else
            {
                newTreeProtoList.Add(new TreePrototype { prefab = treePrefab }); treesWereUpdated = true;
            }
        }
        if (treesWereUpdated) { td.treePrototypes = newTreeProtoList.ToArray(); terrainDataWasChanged = true; }

        Material terrainMaterial = terrain.materialTemplate;
        if (terrainMaterial != null)
        {
            Texture2D baseMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (baseMapTexture != null)
            {
                string[] terrainTexturePropertyNames = { "_BaseMap", "_MainTex" };
                foreach (var propName in terrainTexturePropertyNames)
                {
                    if (terrainMaterial.HasProperty(propName))
                    {
                        if (terrainMaterial.GetTexture(propName) != baseMapTexture)
                        {
                            terrainMaterial.SetTexture(propName, baseMapTexture);
                            EditorUtility.SetDirty(terrainMaterial);
                        }
                        break;
                    }
                }
            }
            else { Debug.LogWarning($"[FixTerrainInScene] Terrain base map texture not found at: {GrassTexturePath}"); }
        }
        else { Debug.LogWarning($"[FixTerrainInScene] Terrain '{terrain.name}' does not have a material template assigned."); }

        if (terrainDataWasChanged) { EditorUtility.SetDirty(td); EditorUtility.SetDirty(terrain); }
    }

    public static void FixWeatherRenderTextureFormat()
    {
        string renderTexturePath = "Assets/RenderTexture/WeatherRenderTexture.renderTexture";
        RenderTexture weatherRT = AssetDatabase.LoadAssetAtPath<RenderTexture>(renderTexturePath);
        if (weatherRT == null) { Debug.LogError($"[FixWeatherRT] Could not find RT: {renderTexturePath}"); return; }

        var targetFormat = GraphicsFormat.D32_SFloat_S8_UInt;
        if (weatherRT.depthStencilFormat != targetFormat)
        {
            Debug.Log($"[FixWeatherRT] Updating DepthStencilFormat for '{renderTexturePath}' from '{weatherRT.depthStencilFormat}' to '{targetFormat}'.");
            if (weatherRT.IsCreated()) { weatherRT.Release(); }
            weatherRT.depthStencilFormat = targetFormat;
            EditorUtility.SetDirty(weatherRT);
        }
        else { Debug.Log($"[FixWeatherRT] DepthStencilFormat for '{renderTexturePath}' is already '{targetFormat}'."); }
    }
}