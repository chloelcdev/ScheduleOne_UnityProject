using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[RequireComponent(typeof(BoxCollider))]
public class TerrainHoleCutter : MonoBehaviour
{
    [Header("Terrain Settings")]
    [Tooltip("The terrain to cut holes in. If not set, will try to find the terrain at this object's position.")]
    public Terrain targetTerrain;

    [Tooltip("The size of the hole in terrain units (1 unit = 1 meter)")]
    public Vector2 holeSize = new Vector2(2f, 2f);

    [Tooltip("Smoothness of the hole edges (0 = sharp, 1 = very smooth)")]
    [Range(0f, 1f)]
    public float edgeSmoothness = 0.5f;

    [Header("Preview Settings")]
    [Tooltip("Enable real-time preview in the editor")]
    public bool enablePreview = false;

    private BoxCollider triggerCollider;
    private TerrainData terrainData;
    private bool[,] originalHoles;
    private bool isPreviewing = false;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        if (targetTerrain == null)
            targetTerrain = Terrain.activeTerrain;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("NPC"))
            CutHole();
    }

    private void OnValidate()
    {
        if (enablePreview && !isPreviewing)
            StartPreview();
        else if (!enablePreview && isPreviewing)
            StopPreview();
    }

    private void OnDestroy()
    {
        StopPreview();
    }

    private void StartPreview()
    {
        if (targetTerrain == null)
        {
            Debug.LogWarning("No terrain assigned to TerrainHoleCutter!");
            return;
        }

        terrainData = targetTerrain.terrainData;
        originalHoles = terrainData.GetHoles(0, 0, terrainData.holesResolution, terrainData.holesResolution);
        isPreviewing = true;
        CutHole();
    }

    private void StopPreview()
    {
        if (isPreviewing && originalHoles != null && terrainData != null)
        {
            terrainData.SetHolesDelayLOD(0, 0, originalHoles);
            //terrainData.SyncTexture();
            isPreviewing = false;
        }
    }

    private Bounds GetTerrainBounds()
    {
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPos = targetTerrain.transform.position;
        return new Bounds(
            terrainPos + terrainSize * 0.5f,
            terrainSize
        );
    }

    private bool GetTerrainIntersection(out Vector2Int startPos, out Vector2Int size)
    {
        startPos = Vector2Int.zero;
        size = Vector2Int.zero;

        if (targetTerrain == null || terrainData == null)
            return false;

        Bounds terrainBounds = GetTerrainBounds();
        Bounds objectBounds = triggerCollider.bounds;

        // Check if the object intersects with the terrain at all
        if (!terrainBounds.Intersects(objectBounds))
            return false;

        // Convert world positions to terrain hole coordinates
        Vector3 terrainPos = targetTerrain.transform.position;
        int holesResolution = terrainData.holesResolution;
        Vector3 terrainSize = terrainData.size;

        // Calculate the intersection area in world space
        Vector3 min = Vector3.Max(terrainBounds.min, objectBounds.min);
        Vector3 max = Vector3.Min(terrainBounds.max, objectBounds.max);

        // Convert to hole coordinates
        startPos = new Vector2Int(
            Mathf.RoundToInt((min.x - terrainPos.x) / terrainSize.x * holesResolution),
            Mathf.RoundToInt((min.z - terrainPos.z) / terrainSize.z * holesResolution)
        );

        size = new Vector2Int(
            Mathf.RoundToInt((max.x - min.x) / terrainSize.x * holesResolution),
            Mathf.RoundToInt((max.z - min.z) / terrainSize.z * holesResolution)
        );

        // Clamp to valid ranges
        startPos = new Vector2Int(
            Mathf.Clamp(startPos.x, 0, holesResolution - 1),
            Mathf.Clamp(startPos.y, 0, holesResolution - 1)
        );

        size = new Vector2Int(
            Mathf.Clamp(size.x, 1, holesResolution - startPos.x),
            Mathf.Clamp(size.y, 1, holesResolution - startPos.y)
        );

        return true;
    }

    public void CutHole()
    {
        if (targetTerrain == null)
        {
            Debug.LogWarning("No terrain assigned to TerrainHoleCutter!");
            return;
        }

        terrainData = targetTerrain.terrainData;

        if (!GetTerrainIntersection(out Vector2Int startPos, out Vector2Int size))
        {
            Debug.LogWarning("No intersection with terrain found!");
            return;
        }

        // Get current holes data
        bool[,] holes = isPreviewing ? originalHoles : terrainData.GetHoles(0, 0, terrainData.holesResolution, terrainData.holesResolution);

        // Create a smooth hole
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                int actualX = startPos.x + x;
                int actualY = startPos.y + y;

                // Calculate distance from center of hole
                float distanceX = (x - size.x * 0.5f) / (size.x * 0.5f);
                float distanceY = (y - size.y * 0.5f) / (size.y * 0.5f);
                float distance = Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);

                // Apply smoothness
                float smoothFactor = Mathf.Clamp01(1f - (distance * (1f - edgeSmoothness)));

                // Set hole based on smoothness
                holes[actualY, actualX] = smoothFactor < 0.5f;
            }
        }

        // Apply the modified holes
        terrainData.SetHolesDelayLOD(0, 0, holes);
        //if (!isPreviewing)
            //terrainData.SyncTexture(textureName)
    }

    private void OnDrawGizmos()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        // Draw the trigger area
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, triggerCollider.size);

        // Draw the intersection area if we have a terrain
        if (targetTerrain != null && terrainData != null)
        {
            if (GetTerrainIntersection(out Vector2Int startPos, out Vector2Int size))
            {
                Vector3 terrainPos = targetTerrain.transform.position;
                Vector3 terrainSize = terrainData.size;
                int holesResolution = terrainData.holesResolution;

                Vector3 worldStart = new Vector3(
                    terrainPos.x + (startPos.x / (float)holesResolution) * terrainSize.x,
                    terrainPos.y,
                    terrainPos.z + (startPos.y / (float)holesResolution) * terrainSize.z
                );

                Vector3 worldSize = new Vector3(
                    (size.x / (float)holesResolution) * terrainSize.x,
                    0.1f,
                    (size.y / (float)holesResolution) * terrainSize.z
                );

                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawCube(worldStart + worldSize * 0.5f, worldSize);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainHoleCutter))]
public class TerrainHoleCutterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainHoleCutter cutter = (TerrainHoleCutter)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Cut Hole Now"))
        {
            cutter.CutHole();
        }
    }
}
#endif