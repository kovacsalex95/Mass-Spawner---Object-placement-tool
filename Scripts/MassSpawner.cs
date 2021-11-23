
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

        public Texture2D terrainHeight = null;
        public Texture2D terrainSlope = null;
        public Texture2D terrain3D = null;

        public Vector2 terrainOffset = new Vector2(500, 500);
        public Vector2 terrainSize = new Vector2(1000, 1000);
        public float terrainTop = 500f;
        public float terrainBottom = 0;

        public LayerMask includeMask;
        public LayerMask excludeMask;

        public LayerMask surveyMask
        {
            get
            {
                LayerMask result = includeMask;

                bool[] excludeLayers = Util.LayerMaskHasLayers(excludeMask);
                for (int i = 0; i < excludeLayers.Length; i++)
                {
                    if (excludeLayers[i] && !Util.LayerMaskContainsLayer(result, i))
                        result = Util.AddLayerToLayerMask(result, i);
                }

                return result;
            }
        }

        public ObjectLayer[] objectLayers = null;
        public int oldSelectedObjectLayerIndex = -1;
        public int selectedObjectLayerIndex = -1;

        public ColorGroup[] colorGroups = null;
        public int selectedColorGroupIndex = -1;

        public PreviewMode preview = PreviewMode.Heightmap;
        public bool showPlacement = true;

        int generatedPreviewTexturesResolution = -1;
        RenderTexture heightmapPreviewTexture = null;
        RenderTexture layerPreviewTexture = null;
        ComputeShader previewComputeShader = null;
        ComputeShader placementComputeShader = null;

        public int selectedTab = -1;
        public int selectedSettingsTab = -1;

        public static int TAB_LAYERS = 1;
        public static int TAB_COLORS = 2;
        public static int TAB_WORLD_HEIGHTMAP = 0;

        public static int STAB_PLACEMENT = 0;
        public static int STAB_TRANSFORM = 1;
        public static int STAB_COLLISION = 2;
        public static int STAB_PREFABS = 3;

        public float previewScale = 1f;
        public float previewOffsetX = 0.5f;
        public float previewOffsetY = 0.5f;


        [System.NonSerialized]
        public bool mapsChecked = false;


        // HEIGHTMAP
        public void GenerateHeightmap()
        {
            this.terrainHeight = new Texture2D((int)heightmapResolution, (int)heightmapResolution, TextureFormat.RGBAFloat, false);

            float terrainHeight = terrainTop - terrainBottom;
            if (terrainHeight > 0)
            {
                RaycastHit hit;
                Color nullColor = new Color(0, 0, 0, 0);

                for (int x = 0; x < (int)heightmapResolution; x++)
                {
                    for (int y = 0; y < (int)heightmapResolution; y++)
                    {
                        Vector3 pos = HeightmapToWorld(x, y);
                        Color c = nullColor;
                        if (Physics.Raycast(pos, Vector3.down, out hit, terrainHeight + 1, surveyMask))
                        {
                            bool isExcluded = Util.LayerMaskContainsLayer(excludeMask, hit.collider.gameObject.layer);
                            if (!isExcluded)
                            {
                                float distanceRatio = 1f - Mathf.Clamp01(hit.distance / terrainHeight);
                                c = Util.ColorLerp(Color.black, Color.white, distanceRatio);
                            }
                            else
                                c = nullColor;
                        }
                        this.terrainHeight.SetPixel(x, y, c);
                    }
                }
                this.terrainHeight.Apply();
            }

            GenerateTerrainSlope();
            SaveMapsToTextureArray();

            GenerateObjectPoints();
        }

        public Vector3 HeightmapToWorld(int xx, int yy)
        {
            Vector3 centerTop = transform.position + new Vector3(terrainOffset.x, 0, terrainOffset.y) + Vector3.up * terrainTop;
            Vector3 centerCorner = centerTop + new Vector3(-terrainSize.x / 2, 0, -terrainSize.y / 2);

            float xRatio = (float)xx / (float)heightmapResolution;
            float yRatio = (float)yy / (float)heightmapResolution;

            float absX = terrainSize.x * xRatio;
            float absY = terrainSize.y * yRatio;

            return centerCorner + new Vector3(absX, 0, absY);
        }



        // SLOPEMAP
        public void GenerateTerrainSlope()
        {
            float heightRatioX = (terrainTop - terrainBottom) / terrainSize.x;
            float heightRatioY = (terrainTop - terrainBottom) / terrainSize.y;

            float SIN45 = 0.707106781f;

            float biggestSlope = 0f;

            terrainSlope = new Texture2D((int)heightmapResolution, (int)heightmapResolution, TextureFormat.RGBAFloat, false);
            terrain3D = new Texture2D((int)heightmapResolution, (int)heightmapResolution, TextureFormat.RGBAFloat, false);
            for (int x = 0; x < (int)heightmapResolution; x++)
            {
                for (int y = 0; y < (int)heightmapResolution; y++)
                {
                    Color targetPixel = new Color(0, 0, 0, 0);
                    Color target3D = new Color(0, 0, 0, 0);
                    Color terrainPixel = terrainHeight.GetPixel(x, y);

                    if (terrainPixel.a > 0)
                    {
                        float height = terrainPixel.r;

                        // Steepness
                        float xDiff = (x <= (int)heightmapResolution - 1) ? terrainPixel.r - terrainHeight.GetPixel(x + 1, y).r : 0;
                        float xAngle = Mathf.Abs(xDiff) / heightRatioX;
                        float xSlope = Mathf.Atan(xAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float yDiff = (y <= (int)heightmapResolution - 1) ? terrainPixel.r - terrainHeight.GetPixel(x, y + 1).r : 0;
                        float yAngle = Mathf.Abs(yDiff) / heightRatioY;
                        float ySlope = Mathf.Atan(yAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float dDiff = 0;
                        if (x <= (int)heightmapResolution - 1 && y <= (int)heightmapResolution - 1)
                        {
                            float fullD = terrainPixel.r - terrainHeight.GetPixel(x + 1, y + 1).r;
                            float origD = terrainPixel.r - terrainHeight.GetPixel(x, y).r;
                            dDiff = Mathf.Lerp(origD, fullD, SIN45);
                        }

                        float dAngle = Mathf.Abs(dDiff) / ((heightRatioX + heightRatioY) / 2);
                        float dSlope = Mathf.Atan(dAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float distance = Mathf.Max(Mathf.Clamp01(xSlope), Mathf.Clamp01(ySlope), Mathf.Clamp01(dSlope));
                        distance -= 0.5f;
                        distance = Mathf.Abs(distance) * 2f;

                        distance *= (float)heightmapResolution / (float)Resolutions._512x512;
                        distance = Mathf.Clamp01(distance);

                        if (distance > biggestSlope)
                            biggestSlope = distance;

                        targetPixel = new Color(distance, distance, distance, 1);


                        // 3D view
                        float arct2 = Mathf.Clamp01(Mathf.Atan2(yDiff, xDiff) * Mathf.Rad2Deg / 360f + 0.5f);
                        float light = Mathf.Abs(arct2 - 0.5f) * 2f;

                        target3D = new Color(light, light, light, 1);
                    }

                    terrainSlope.SetPixel(x, y, targetPixel);
                    terrain3D.SetPixel(x, y, target3D);
                }
            }
            terrainSlope.Apply();
            terrain3D.Apply();

            RemapTerrainSlope(biggestSlope);
        }

        void RemapTerrainSlope(float biggestSlope)
        {
            if (biggestSlope <= 0)
                return;

            for (int x = 0; x < (int)heightmapResolution; x++)
            {
                for (int y = 0; y < (int)heightmapResolution; y++)
                {
                    float currentValue = terrainSlope.GetPixel(x, y).r;
                    float remapValue = currentValue / biggestSlope;
                    terrainSlope.SetPixel(x, y, new Color(remapValue, remapValue, remapValue, 1));
                }
            }

            terrainSlope.Apply();
        }



        // MAPS SAVING
        void SaveMapsToTextureArray()
        {
            Texture2DArray mapsArray = new Texture2DArray((int)heightmapResolution, (int)heightmapResolution, 3, TextureFormat.RGBAFloat, false);

            mapsArray.SetPixels(terrainHeight.GetPixels(), 0);
            mapsArray.SetPixels(terrainSlope.GetPixels(), 1);
            mapsArray.SetPixels(terrain3D.GetPixels(), 2);

            mapsArray.Apply();

            AssetDatabase.CreateAsset(mapsArray, Util.AssetFolder + "/generatedMaps.asset");
        }

        public void TryToLoadSavedMaps()
        {
            mapsChecked = true;

            string path = Util.AssetFolder + "/generatedMaps.asset";

            if (!Util.AssetExists(path))
                return;

            Texture2DArray mapsArray = (Texture2DArray)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2DArray));

            terrainHeight = null;
            terrainSlope = null;
            terrain3D = null;

            terrainHeight = new Texture2D(mapsArray.width, mapsArray.height, TextureFormat.RGBAFloat, false);
            terrainHeight.SetPixels(mapsArray.GetPixels(0));
            terrainHeight.Apply();

            terrainSlope = new Texture2D(mapsArray.width, mapsArray.height, TextureFormat.RGBAFloat, false);
            terrainSlope.SetPixels(mapsArray.GetPixels(1));
            terrainSlope.Apply();

            terrain3D = new Texture2D(mapsArray.width, mapsArray.height, TextureFormat.RGBAFloat, false);
            terrain3D.SetPixels(mapsArray.GetPixels(2));
            terrain3D.Apply();

            SceneView.RepaintAll();
        }



        // OBJECTS
        public void GenerateObjectPoints()
        {
            foreach (ObjectLayer l in objectLayers)
            {
                GenerateObjectPoints(l);
            }
        }

        public void GenerateObjectPoints(ObjectLayer layer)
        {
            if (terrainHeight == null)
                return;

            if (placementComputeShader == null)
                CheckPreviewTextures();

            int placePointsCount = Util.PlacementPointCount((int)heightmapResolution, layer.everyN);
            PlacementPoint[] placePoints = new PlacementPoint[placePointsCount];


            layer.objectPlaces = new RenderTexture((int)heightmapResolution, (int)heightmapResolution, 0, RenderTextureFormat.ARGB32);
            layer.objectPlaces.enableRandomWrite = true;
            layer.objectPlaces.filterMode = FilterMode.Point;
            
            layer.objectPlaces.Create();


            int kernelIndex = placementComputeShader.FindKernel("CSClear");
            placementComputeShader.SetTexture(kernelIndex, "PlacementMap", layer.objectPlaces);
            placementComputeShader.Dispatch(kernelIndex, (int)heightmapResolution / 8, (int)heightmapResolution / 8, 1);


            ComputeBuffer placementPointsBuffer = new ComputeBuffer(placePointsCount, PlacementPoint.stride);
            placementPointsBuffer.SetData(placePoints);


            kernelIndex = placementComputeShader.FindKernel("CSMain");
            placementComputeShader.SetTexture(kernelIndex, "PlacementMap", layer.objectPlaces);
            placementComputeShader.SetFloat("TextureSize", (float)heightmapResolution);
            placementComputeShader.SetFloat("AngleOffset", layer.angleOffset);
            placementComputeShader.SetFloat("EveryN", layer.everyN);
            placementComputeShader.SetFloat("Organicity", layer.organicity);
            placementComputeShader.SetBuffer(kernelIndex, "Points", placementPointsBuffer);
            placementComputeShader.Dispatch(kernelIndex, (int)heightmapResolution / 8, (int)heightmapResolution / 8, 1);

            placementPointsBuffer.GetData(placePoints);

            placementPointsBuffer.Dispose();

            layer.objectPoints = placePoints;
        }

        public void ResetPlacementTextures()
        {
            foreach (ObjectLayer layer in objectLayers)
                ResetLayerPlacementTextures(layer);
        }

        public void ResetLayerPlacementTextures(ObjectLayer layer)
        {
            layer.objectPlaces = null;
            GenerateObjectPoints(layer);
        }


        public void ClearObjects()
        {
            int childs = transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        public void PlaceObjects()
        {
            int placedCount = 0;
            ClearObjects();

            if (objectLayers.Length == 0)
                return;

            for (int i = 0; i < objectLayers.Length; i++)
            {
                previewObjectLayer = objectLayers[selectedObjectLayerIndex];
                if (previewObjectLayer.objectPoints == null || previewObjectLayer.objectPoints.Length == 0)
                    GenerateObjectPoints(previewObjectLayer);
            }

            if (terrainHeight != null)
            {
                foreach (ObjectLayer layer in objectLayers)
                {
                    foreach(PlacementPoint point in layer.objectPoints)
                    {
                        if (!point.valid)
                            continue; 

                        int x = point.x;
                        int y = point.y;

                        Color heightColor = terrainHeight.GetPixel(x, y);

                        if (heightColor.a == 0)
                            continue;

                        float height = heightColor.r;
                        if (height < layer.from || (height >= layer.to && !(height == 1 && layer.to == 1)))
                            continue;

                        Color slopeColor = terrainSlope.GetPixel(x, y);
                        float slope = slopeColor.r;

                        if (layer.minSlope > slope || layer.maxSlope < slope)
                            continue;

                        PlacePrefab(layer, x, y, height, placedCount);

                        placedCount++;
                    }
                }
            }
        }

        void PlacePrefab(ObjectLayer layer, int x, int y, float height, int placedCount)
        {
            PlaceObject[] prefabs = layer.okPrefabs;

            if (prefabs.Length > 0)
            {
                int prefabIndex = prefabs.Length == 1 ? 0 : Mathf.RoundToInt(Mathf.Clamp(Random.Range(0, prefabs.Length), 0, prefabs.Length - 1));

                PlaceObject obj = prefabs[prefabIndex];

                RandomVector3 Offset = obj._position._override ? obj._position : layer._position;
                RandomVector3 Scale = obj._scale._override ? obj._scale : layer._scale;
                RandomVector3 Rotation = obj._rotation._override ? obj._rotation : layer._rotation;

                float heightRatio = 1f - height;
                Vector3 posTop = HeightmapToWorld(x, y);
                Vector3 pos = posTop + Vector3.down * heightRatio * (terrainTop - terrainBottom) + Offset.value;

                bool place = false;
                RaycastHit[] collisions;

                foreach (CollisionRule cRule in layer.collisionRules)
                {
                    collisions = Physics.SphereCastAll(pos, cRule.radius, Vector3.down, cRule.radius * 2, cRule.layerMask);
                    foreach (RaycastHit h in collisions)
                    {
                        if (cRule.mode == CollisionMode.DeleteOther)
                            GameObject.Destroy(h.collider.gameObject);
                    }

                    if (cRule.mode == CollisionMode.DeleteOther || collisions.Length == 0)
                        place = true;
                    else
                        place = false;

                    if (cRule.stop && collisions.Length > 0)
                        break;
                }

                if (place || layer.collisionRules.Length == 0)
                {
                    GameObject newObject = null;

                    if (layer.placing == PlaceType.Instantiate)
                        newObject = GameObject.Instantiate(obj.prefab, pos, Quaternion.identity);
                    else if (layer.placing == PlaceType.Prefabs)
                    {
                        newObject = PrefabUtility.InstantiatePrefab(obj.prefab) as GameObject;
                        newObject.transform.position = pos;
                        newObject.transform.rotation = Quaternion.identity;
                    }

                    if (newObject == null)
                        return;

                    newObject.name = string.Format("{0} ({1})", obj.prefab.name, placedCount);
                    newObject.transform.eulerAngles = Rotation.value;
                    newObject.transform.localScale = Scale.value;

                    newObject.transform.parent = transform;

                    // coloring
                    if (layer.placing == PlaceType.Instantiate && obj.colors != null)
                    {
                        MeshRenderer[] renderers = newObject.GetComponentsInChildren<MeshRenderer>();

                        try
                        {
                            foreach (MaterialColoring coloring in obj.colors)
                            {
                                if (ColorGroupExists(coloring.colorGroup))
                                {
                                    string materialName = coloring.material.name;
                                    string shaderName = coloring.material.shader.name;
                                    Color color = colorGroups[coloring.colorGroup].value;

                                    for (int i = 0; i < renderers.Length; i++)
                                    {
                                        List<Material> newMaterials = new List<Material>();
                                        foreach (Material material in renderers[i].sharedMaterials)
                                        {
                                            if (material != null && material.name == materialName && material.shader.name == shaderName)
                                            {
                                                Material newMaterial = new Material(material);
                                                newMaterial.SetColor(coloring.propertyName != "" ? coloring.propertyName : "_BaseColor", color);
                                                newMaterial.name = string.Format("{0} {1} (clone)", material.name, placedCount);
                                                newMaterials.Add(newMaterial);
                                            }
                                            else
                                                newMaterials.Add(material);
                                        }
                                        renderers[i].sharedMaterials = newMaterials.ToArray();
                                    }
                                }
                            }
                        }
                        catch
                        {
                            Debug.Log("Material coloring error #" + placedCount.ToString());
                        }
                    }
                }
            }
        }

        bool ColorGroupExists(int index)
        {
            return colorGroups == null || colorGroups.Length == 0 ? false : (index >= 0 && index < colorGroups.Length);
        }



        // PREVIEW
        void CheckPreviewTextures()
        {
            if (generatedPreviewTexturesResolution == (int)heightmapResolution && heightmapPreviewTexture != null && layerPreviewTexture != null && previewComputeShader != null && placementComputeShader != null)
                return;

            generatedPreviewTexturesResolution = (int)heightmapResolution;

            heightmapPreviewTexture = new RenderTexture(generatedPreviewTexturesResolution, generatedPreviewTexturesResolution, 0, RenderTextureFormat.ARGB32);
            heightmapPreviewTexture.enableRandomWrite = true;
            heightmapPreviewTexture.filterMode = FilterMode.Trilinear;
            heightmapPreviewTexture.wrapMode = TextureWrapMode.Clamp;
            heightmapPreviewTexture.Create();

            layerPreviewTexture = new RenderTexture(generatedPreviewTexturesResolution, generatedPreviewTexturesResolution, 0, RenderTextureFormat.ARGB32);
            layerPreviewTexture.enableRandomWrite = true;
            layerPreviewTexture.filterMode = FilterMode.Trilinear;
            layerPreviewTexture.wrapMode = TextureWrapMode.Clamp;
            layerPreviewTexture.Create();

            previewComputeShader = (ComputeShader)Resources.Load("Shaders/EditorPreview");

            placementComputeShader = (ComputeShader)Resources.Load("Shaders/PlacementGenerator");
        }

        public RenderTexture GenerateHeightmapPreviewTexture(Texture2D heightmap)
        {
            CheckPreviewTextures();

            int kernelIndex = previewComputeShader.FindKernel("CSHeightmap");

            previewComputeShader.SetTexture(kernelIndex, "HeightmapResult", heightmapPreviewTexture);
            previewComputeShader.SetTexture(kernelIndex, "Heightmap", heightmap);

            previewComputeShader.Dispatch(kernelIndex, heightmapPreviewTexture.width / 8, heightmapPreviewTexture.height / 8, 1);

            return heightmapPreviewTexture;
        }

        public RenderTexture GenerateLayerPreviewTexture()
        {
            CheckPreviewTextures();

            int kernelIndex = previewComputeShader.FindKernel("CSLayer");

            previewComputeShader.SetTexture(kernelIndex, "LayerResult", layerPreviewTexture);
            previewComputeShader.SetTexture(kernelIndex, "Heightmap", terrainHeight);
            previewComputeShader.SetTexture(kernelIndex, "SlopeMap", terrainSlope);
            previewComputeShader.SetTexture(kernelIndex, "PlacementMap", objectLayers[selectedObjectLayerIndex].objectPlaces);
            previewComputeShader.SetVector("HeightRange", new Vector4(objectLayers[selectedObjectLayerIndex].from, objectLayers[selectedObjectLayerIndex].to, 0, 0));
            previewComputeShader.SetVector("SlopeRange", new Vector4(objectLayers[selectedObjectLayerIndex].minSlope, objectLayers[selectedObjectLayerIndex].maxSlope, 0, 0));
            previewComputeShader.SetBool("ShowPlacement", showPlacement);

            previewComputeShader.Dispatch(kernelIndex, heightmapPreviewTexture.width / 8, heightmapPreviewTexture.height / 8, 1);

            return layerPreviewTexture;
        }


#if UNITY_EDITOR
        Color previewHeightColor;
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
        private void OnDrawGizmos()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                // World bounds
                if (selectedTab == TAB_WORLD_HEIGHTMAP)
                {
                    Vector3 worldCenter = transform.position + Vector3.up * ((terrainBottom + terrainTop) / 2f) + new Vector3(terrainOffset.x, 0, terrainOffset.y);
                    Vector3 worldSize = new Vector3(terrainSize.x, terrainTop - terrainBottom, terrainSize.y);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(worldCenter, worldSize);
                }

                // Placement preview
                if (selectedTab != TAB_LAYERS)
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

                    RandomVector3 Offset = previewObj
                        ._position
                        ._override ?
                        previewObj
                        ._position :
                        previewObjectLayer
                        ._position;

                    float heightRatio = 1f - previewHeight;
                    Vector3 posTop = HeightmapToWorld(prevX, prevY);
                    Vector3 pos = posTop + Vector3.down * heightRatio * (terrainTop - terrainBottom) + Offset.value;

                    Gizmos.DrawCube(pos + Vector3.up * 1.5f, Vector3.one * 4);
                }
            }
        }
#endif
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