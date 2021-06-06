
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace lxkvcs
{
    [ExecuteInEditMode()]
    public class MassSpawner : MonoBehaviour
    {
        [SerializeField]
        public Resolutions heightmapResolution = Resolutions._1024x1024;

        public Vector2 terrainOffset = new Vector2(500, 500);
        public Vector2 terrainSize = new Vector2(1000, 1000);
        public float terrainTop = 500f;
        public float terrainBottom = 0;

        public LayerMask includeMask;
        public LayerMask excludeMask;

        public ObjectLayer[] objectLayers = null;
        public ColorGroup[] colorGroups = null;


        /*Color previewHeightColor;
        float previewHeight;
        Color previewSlopeColor;
        float previewSlope;
        ObjectLayer previewObjectLayer;
        bool previewInHeightRange;
        bool previewInSlopeRange;
        PlaceObject[] previewPrefabs;
        PlaceObject previewObj;
        int prevX;
        int prevY;
        private void OnDrawGizmosSelected()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                // World bounds
                if (openedModal == TAB_WORLD_HEIGHTMAP)
                {
                    Vector3 worldSize = new Vector3(terrainSize.x, terrainTop - terrainBottom, terrainSize.y);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(worldCenter, worldSize);
                }

                // Placement preview
                if (openedModal != TAB_LAYERS)
                    return;

                if (objectLayers == null || objectLayers.Length == 0 || !showPlacement || selectedObjectLayerIndex == -1 || terrainHeight == null || transform.childCount != 0)
                    return;

                previewObjectLayer = objectLayers[selectedObjectLayerIndex];
                if (previewObjectLayer.objectPoints == null || previewObjectLayer.objectPoints.Length == 0)
                {
                    GenerateObjectPoints(previewObjectLayer);
                    return;
                }

                Gizmos.color = Color.red;
                foreach (PlacementPoint v in previewObjectLayer.objectPoints)
                {
                    if (!v.valid)
                        continue;

                    prevX = (int)v._point.x;
                    prevY = (int)v._point.y;

                    previewHeightColor = terrainHeight.GetPixel(prevX, prevY);
                    if (previewHeightColor.a == 0)
                        continue;

                    previewHeight = previewHeightColor.r;

                    previewInHeightRange = previewHeight >= previewObjectLayer.from && (previewHeight < previewObjectLayer.to || (previewHeight == 1 && previewObjectLayer.to == 1));
                    if (!previewInHeightRange)
                        continue;

                    previewSlopeColor = terrainSlope.GetPixel(prevX, prevY);
                    previewSlope = previewSlopeColor.r;

                    previewInSlopeRange = previewObjectLayer.minSlope <= previewSlope && previewObjectLayer.maxSlope >= previewSlope;
                    if (!previewInSlopeRange)
                        continue;

                    if (previewObjectLayer.okPrefabsCount == 0)
                        continue;

                    previewPrefabs = previewObjectLayer.okPrefabs;

                    previewObj = previewPrefabs[0];

                    RandomVector3 Offset = previewObj._position._override ? previewObj._position : previewObjectLayer._position;

                    float heightRatio = 1f - previewHeight;
                    Vector3 posTop = HeightmapToWorld(prevX, prevY);
                    Vector3 pos = posTop + Vector3.down * heightRatio * (terrainTop - terrainBottom) + Offset.value;

                    Gizmos.DrawCube(pos + Vector3.up * 1.5f, Vector3.one * 4);
                }
            }
        }*/
    }


    [System.Serializable]
    public enum EditorMode
    {
        Placement = 0,
        Mask = 1
    }


    [System.Serializable]
    public enum OpenedView
    {
        Placement = 0,
        ColorGroups = 1,
        HeightmapWorld = 2
    }

    [System.Serializable]
    public enum LayerSettingsTab
    {
        Placement = 0,
        Transform = 1,
        Collision = 2,
        Prefabs = 3
    }

    [System.Serializable]
    public enum PreviewMode
    {
        Heightmap = 0,
        Slopemap = 1,
        _3D = 2
    }

    [System.Serializable]
    public enum Resolutions
    {
        _128x128 = 128,
        _256x256 = 256,
        _512x512 = 512,
        _1024x1024 = 1024,
        _2048x2048 = 2048,
        _4096x4096 = 4096
    }
}