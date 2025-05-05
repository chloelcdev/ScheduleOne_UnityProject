// GridGenerator.cs - MonoBehaviour to generate grid of IndoorTiles
using UnityEngine;
using UnityEditor;
using ScheduleOne.Map;
using ScheduleOne.Tiles;
using Grid = ScheduleOne.Tiles.Grid;
using System.Collections.Generic;
using ScheduleOne.Tiles;

namespace ChloesManorMod
{
    public class GridGenerator : MonoBehaviour
    {
        public Grid targetGrid;
        public IndoorTile tilePrefab;
        public int startX = 0;
        public int startY = 0;
        public int countX = 10;
        public int countY = 10;
        public float tileSpacing = 0.5f;
        // Editor-assigned LayerMask for obstacles to skip tile placement
        public LayerMask obstacleLayerMask;

        private void Start()
        {
            GenerateGrid();
        }

        public void GenerateGrid()
        {
            if (targetGrid == null)
            {
                targetGrid = GetComponent<Grid>();
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

            // Ensure Tiles list is initialized (don't clear existing tiles)
            if (targetGrid.Tiles == null)
                targetGrid.Tiles = new();

            for (int x = 0; x < countX; x++)
            {
                for (int y = 0; y < countY; y++)
                {
                    Vector3 localPosition = new Vector3(x * tileSpacing, 0f, y * tileSpacing);
                    GameObject tileGO;
                    tileGO = PrefabUtility.InstantiatePrefab(tilePrefab.gameObject) as GameObject;
                    tileGO.transform.SetParent(transform);
                    tileGO.transform.localPosition = localPosition;

                    // after spawning, cast upward from each corner to detect obstacles
                    float half = tileSpacing * 0.5f;
                    Vector3 worldPos = tileGO.transform.position;
                    bool blocked = false;
                    Vector3[] cornerOffsets = {
                        new Vector3( half, 0f,  half),
                        new Vector3(-half, 0f,  half),
                        new Vector3( half, 0f, -half),
                        new Vector3(-half, 0f, -half)
                    };
                    foreach (var offset in cornerOffsets)
                    {
                        Vector3 origin = worldPos + offset;
                        if (Physics.OverlapBox(origin, Vector3.one * 0.1f, Quaternion.identity, obstacleLayerMask).Length > 0)
                        {
                            blocked = true;
                            break;
                        }
                        
                    }
                    if (blocked)
                    {
                        DestroyImmediate(tileGO);
                        continue;
                    }

                    var indoorTile = tileGO.GetComponent<IndoorTile>();
                    indoorTile.x = startX + x;
                    indoorTile.y = startY + y;
                    indoorTile.OwnerGrid = targetGrid;
                    targetGrid.Tiles.Add(indoorTile);

                    indoorTile.gameObject.name = $"Tile [{indoorTile.x},{indoorTile.y}]";
                }
            }

            // Process coordinate pairs
            //targetGrid.ProcessCoordinateDataPairs();
        }

        [ContextMenu("Clear and fire all generators for grid")]
        private void ClearAndRegenerateAllGrids()
        {
            GridGenerator[] generators = targetGrid.GetComponentsInChildren<GridGenerator>();


            foreach (var generator in generators)
            {
                List<Transform> children = new();
                foreach (Transform child in generator.transform)
                    children.Add(child);

                foreach (Transform tile in children)
                    if (tile?.gameObject != null)
                        DestroyImmediate(tile.gameObject);

                generator.targetGrid.Tiles.Clear();
                generator.targetGrid.CoordinateTilePairs.Clear();
            }

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
