// GridGenerator.cs - MonoBehaviour to generate grid of IndoorTiles
using UnityEngine;
using ScheduleOne.Tiles;
using Grid = ScheduleOne.Tiles.Grid;
using System.Collections.Generic;
using static UnityEngine.UI.Image;

namespace ChloesManorMod
{
    public class GridGenerator : MonoBehaviour
    {
        public Grid targetGrid;
        public IndoorTile tilePrefab;
        public int startX = 10;
        public int startY = 10;
        public int countX = 10;
        public int countY = 10;
        public float tileSpacing = 0.5f;
        // Editor-assigned LayerMask for obstacles to skip tile placement
        public LayerMask obstacleLayerMask;

        private void Start()
        {
            GenerateGrid();
            if (obstacleLayerMask == default)
                obstacleLayerMask = LayerMask.GetMask("GridBlock"); // Default to "Default" layer if not set
        }

        public void GenerateGrid()
        {
            Debug.Log($"Generating grid {this.gameObject.name}");
            if (targetGrid == null)
            {
                targetGrid = GetComponentInParent<Grid>();
                if (targetGrid == null)
                {
                    Debug.LogError("GridGenerator: No Grid reference found on GameObject.");
                    return;
                }
            }
            if (tilePrefab == null)
            {
                Debug.LogError("GridGenerator: No Tile Prefab assigned.");
                return;
            }

            Debug.Log("Ensuring tile collections exist");

            // Ensure Tile Collections are initialized (don't clear existing ones)
            if (targetGrid.Tiles == null)
                targetGrid.Tiles = new();

            if (targetGrid.CoordinateTilePairs == null)
                targetGrid.CoordinateTilePairs = new();

            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    Vector3 localPosition = new Vector3(x * tileSpacing, 0f, y * tileSpacing);
                    GameObject tileGO;
#if UNITY_EDITOR
                    tileGO = UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefab.gameObject) as GameObject;
#else
                    tileGO = Instantiate(tilePrefab.gameObject);
#endif
                    tileGO.transform.SetParent(transform);
                    tileGO.transform.localPosition = localPosition;

                    // after spawning, cast upward from each corner to detect obstacles
                    float half = tileSpacing * 0.5f;
                    Vector3 worldPos = tileGO.transform.position;
                    bool blocked = false;

                    if (Physics.CheckBox(worldPos, Vector3.one * (tileSpacing * 0.5f), transform.rotation, obstacleLayerMask))
                        blocked = true;

                    if (blocked)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(tileGO);
#else
                        Destroy(tileGO);
#endif
                        continue;
                    }

                    var indoorTile = tileGO.GetComponent<IndoorTile>();
                    indoorTile.x = startX + x;
                    indoorTile.y = startY + y;
                    indoorTile.OwnerGrid = targetGrid;
                    targetGrid.Tiles.Add(indoorTile);

                    Coordinate newcoord = new();
                    newcoord.x = indoorTile.x;
                    newcoord.y = indoorTile.y;

                    targetGrid.CoordinateTilePairs.Add(new()
                    {
                        coord = newcoord,
                        tile = indoorTile
                    });

                    indoorTile.gameObject.name = $"Tile [{indoorTile.x}, {indoorTile.y}]";
                }
            }

            // Process coordinate pairs
            //targetGrid.ProcessCoordinateDataPairs();
        }

        [ContextMenu("Clear and fire all generators for grid")]
        private void ClearAndRegenerateAllGrids()
        {
            GridGenerator[] generators = targetGrid.GetComponentsInChildren<GridGenerator>();


            Debug.Log("=== > Clearing grids...");

            foreach (var generator in generators)
            {
                List<Transform> children = new();
                foreach (Transform child in generator.transform)
                    children.Add(child);

                Debug.Log($"Grid child count: {children.Count}");

#if UNITY_EDITOR
                foreach (Transform child in children)
                    if (child?.gameObject != null)
                        DestroyImmediate(child.gameObject);
#endif
                Debug.Log($"Destroyed children - new count {children.Count}");

                generator.targetGrid.Tiles.Clear();
                generator.targetGrid.CoordinateTilePairs.Clear();

                Debug.Log("Cleared Tiles and CoordinateTilePairs");
            }


            Debug.Log("=== > Generating grids...");
            foreach (var generator in generators)
                generator.GenerateGrid();
        }

        /*
        //TEMPORARY
        [ContextMenu("Activate Model Child")]
        private void ActivateModelChild()
        {
            Transform ParentObj = transform.Find("Model");
            ParentObj.gameObject.SetActive(true);
            Transform model = ParentObj.Find("gridunit");

            if (ParentObj == null || model == null)
            {
                Debug.LogError("Stuff not found.");
                return;
            }

            model.GetComponent<MeshFilter>().mesh = modelMesh;
            model.GetComponent<MeshRenderer>().material = modelMaterial;
        }

        public Mesh modelMesh;
        // Editor-assigned material for the grid unit's Model child (assign in inspector)
        public Material modelMaterial;*/


    }
}
