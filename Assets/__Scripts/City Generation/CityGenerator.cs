using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public class CityGenerator : MonoBehaviour
{
    [Foldout("Prefabs")]
    public List<GameObject> buildingPrefabs;
    public GameObject roadDottedLine;
    public GameObject roadSolidLine;
    public GameObject roadIntersection;

    [Foldout("Generation Constraints")]
    public int minRoadLength;
    public int maxRoadLength;
    public float swapLineTypeChance;

    [Foldout("Building Generation")]
    public float sidewalkGap = 2f;
    public float buildingSpawnChance = 75f;
    public bool spawnBuildingsOnBothSides = true;

    [Tooltip("Extra space between buildings along the road.")]
    public float buildingGapAlongRoad = 2f;

    [Tooltip("Use 90 or -90 if buildings face sideways.")]
    public float buildingYRotationOffset = 90f;

    void Start()
    {
        GenerateRoad();
    }

    void Update()
    {

    }

    void GenerateRoad()
    {
        int roadLength = UnityEngine.Random.Range(minRoadLength, maxRoadLength + 1);
        Debug.Log($"RoadLength {roadLength}");

        GameObject roadParent = new GameObject("Road Parent");
        GameObject buildingParent = new GameObject("Building Parent");

        GameObject roadPrefab = UnityEngine.Random.Range(0, 2) == 0
            ? roadDottedLine
            : roadSolidLine;

        Vector3 spawnCoord = Vector3.zero;
        Quaternion roadRot = Quaternion.identity;

        float roadChunkLength = GetPrefabLength(roadPrefab);

        float rightSideLastBuildingEnd = -99999f;
        float leftSideLastBuildingEnd = -99999f;

        for (int i = 0; i < roadLength; i++)
        {
            GameObject newChunk = Instantiate(
                roadPrefab,
                spawnCoord,
                roadRot,
                roadParent.transform
            );

            float distanceAlongRoad = i * roadChunkLength;

            TrySpawnBuildingsNextToRoadChunk(
                newChunk,
                buildingParent.transform,
                distanceAlongRoad,
                ref rightSideLastBuildingEnd,
                ref leftSideLastBuildingEnd
            );

            spawnCoord += newChunk.transform.forward * roadChunkLength;

            if (i == roadLength - 1)
            {
                spawnCoord += newChunk.transform.forward * 15f;
                break;
            }

            if (UnityEngine.Random.value <= swapLineTypeChance / 100f)
            {
                roadPrefab = roadPrefab == roadDottedLine
                    ? roadSolidLine
                    : roadDottedLine;

                roadChunkLength = GetPrefabLength(roadPrefab);
            }
        }

        Instantiate(roadIntersection, spawnCoord, roadRot, roadParent.transform);
    }

    void TrySpawnBuildingsNextToRoadChunk(
        GameObject roadChunk,
        Transform buildingParent,
        float distanceAlongRoad,
        ref float rightSideLastBuildingEnd,
        ref float leftSideLastBuildingEnd
    )
    {
        if (buildingPrefabs == null || buildingPrefabs.Count == 0)
        {
            Debug.LogWarning("No building prefabs assigned.");
            return;
        }

        if (UnityEngine.Random.value <= buildingSpawnChance / 100f)
        {
            TrySpawnBuilding(
                roadChunk,
                roadChunk.transform.right,
                buildingParent,
                distanceAlongRoad,
                ref rightSideLastBuildingEnd
            );
        }

        if (spawnBuildingsOnBothSides && UnityEngine.Random.value <= buildingSpawnChance / 100f)
        {
            TrySpawnBuilding(
                roadChunk,
                -roadChunk.transform.right,
                buildingParent,
                distanceAlongRoad,
                ref leftSideLastBuildingEnd
            );
        }
    }

    void TrySpawnBuilding(
        GameObject roadChunk,
        Vector3 sideDirection,
        Transform buildingParent,
        float distanceAlongRoad,
        ref float sideLastBuildingEnd
    )
    {
        GameObject buildingPrefab = buildingPrefabs[
            UnityEngine.Random.Range(0, buildingPrefabs.Count)
        ];

        float roadHalfWidth = GetObjectWidth(roadChunk) / 2f;

        float buildingWidth = GetObjectWidth(buildingPrefab);
        float buildingDepth = GetObjectDepth(buildingPrefab);

        // Since we are rotating the building 90 degrees, width/depth may need to swap.
        bool swapsWidthAndDepth = Mathf.Abs(Mathf.RoundToInt(buildingYRotationOffset)) % 180 == 90;

        float buildingHalfWidthAlongRoad = swapsWidthAndDepth
            ? buildingDepth / 2f
            : buildingWidth / 2f;

        float buildingHalfDepthAwayFromRoad = swapsWidthAndDepth
            ? buildingWidth / 2f
            : buildingDepth / 2f;

        float buildingStartAlongRoad = distanceAlongRoad - buildingHalfWidthAlongRoad;
        float buildingEndAlongRoad = distanceAlongRoad + buildingHalfWidthAlongRoad;

        if (buildingStartAlongRoad < sideLastBuildingEnd + buildingGapAlongRoad)
        {
            return;
        }

        float distanceFromRoadCenter =
            roadHalfWidth +
            sidewalkGap +
            buildingHalfDepthAwayFromRoad;

        Vector3 buildingSpawnPos =
            roadChunk.transform.position +
            sideDirection.normalized * distanceFromRoadCenter;

        Quaternion buildingRot =
            Quaternion.LookRotation(-sideDirection, Vector3.up) *
            Quaternion.Euler(0f, buildingYRotationOffset, 0f);

        GameObject newBuilding = Instantiate(
            buildingPrefab,
            buildingSpawnPos,
            buildingRot,
            buildingParent
        );

        AlignBuildingBottomToRoad(newBuilding, roadChunk);

        sideLastBuildingEnd = buildingEndAlongRoad;
    }

    void AlignBuildingBottomToRoad(GameObject building, GameObject roadChunk)
    {
        Renderer buildingRenderer = building.GetComponentInChildren<Renderer>();
        Renderer roadRenderer = roadChunk.GetComponentInChildren<Renderer>();

        if (buildingRenderer == null)
        {
            Debug.LogError($"No renderer attached to building: {building}");
            return;
        }

        if (roadRenderer == null)
        {
            Debug.LogError($"No renderer attached to road chunk: {roadChunk}");
            return;
        }

        float buildingBottomY = buildingRenderer.bounds.min.y;
        float roadSurfaceY = roadRenderer.bounds.max.y;

        float yOffset = roadSurfaceY - buildingBottomY;

        building.transform.position += Vector3.up * yOffset;
    }

    float GetPrefabLength(GameObject prefab)
    {
        Renderer renderer = prefab.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogError($"No renderer attached to prefab: {prefab}");
            return 1f;
        }

        return renderer.bounds.size.z;
    }

    float GetObjectWidth(GameObject obj)
    {
        Renderer renderer = obj.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogError($"No renderer attached to object: {obj}");
            return 1f;
        }

        return renderer.bounds.size.x;
    }

    float GetObjectDepth(GameObject obj)
    {
        Renderer renderer = obj.GetComponentInChildren<Renderer>();

        if (renderer == null)
        {
            Debug.LogError($"No renderer attached to object: {obj}");
            return 1f;
        }

        return renderer.bounds.size.z;
    }
}