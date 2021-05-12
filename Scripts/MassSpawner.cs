
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Rendering;
using UnityEditor;

namespace lxkvcs
{
    [ExecuteInEditMode()]
    public class MassSpawner : MonoBehaviour
    {
        [SerializeField]
        public Resolutions splatResolution = Resolutions._1024x1024;

        public Texture2D terrainSplat = null;
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

                bool[] excludeLayers = MassSpawner.HasLayers(excludeMask);
                for (int i = 0; i < excludeLayers.Length; i++)
                {
                    if (excludeLayers[i] && !MassSpawner.LayerIncluded(result, i))
                        result = MassSpawner.AddLayer(result, i);
                }

                return result;
            }
        }

        public ObjectLayer[] objectLayers = null;
        public int oldSelectedObjectLayerIndex = -1;
        public int selectedObjectLayerIndex = -1;

        public ColorGroup[] colorGroups = null;
        public int selectedColorGroupIndex = -1;

        public string[] colorGroupLabels
        {
            get
            {
                List<string> labels = new List<string>();
                if (colorGroups != null && colorGroups.Length > 0)
                {
                    foreach (ColorGroup g in colorGroups)
                        labels.Add(g.name);
                }
                else
                {
                    labels.Add("-");
                }
                return labels.ToArray();
            }
        }
        public bool colorGroupExists(int index)
        {
            return colorGroups == null || colorGroups.Length == 0 ? false : (index >= 0 && index < colorGroups.Length);
        }

        public PreviewMode preview = PreviewMode.Heightmap;
        public bool showPlacement = true;

        public void GenerateHeightmap()
        {
            terrainSplat = new Texture2D((int)splatResolution, (int)splatResolution, TextureFormat.RGBAFloat, false);

            float terrainHeight = terrainTop - terrainBottom;
            if (terrainHeight > 0)
            {
                RaycastHit hit;
                Color nullColor = new Color(0, 0, 0, 0);

                for (int x = 0; x < (int)splatResolution; x++)
                {
                    for (int y = 0; y < (int)splatResolution; y++)
                    {
                        Vector3 pos = heightmapToWorld(x, y);
                        Color c = nullColor;
                        if (Physics.Raycast(pos, Vector3.down, out hit, terrainHeight + 1, surveyMask))
                        {
                            bool isExcluded = LayerIncluded(excludeMask, hit.collider.gameObject.layer);
                            if (!isExcluded)
                            {
                                float distanceRatio = 1f - Mathf.Clamp01(hit.distance / terrainHeight);
                                c = LerpColor(Color.black, Color.white, distanceRatio);
                            }
                            else
                                c = nullColor;
                        }
                        terrainSplat.SetPixel(x, y, c);
                    }
                }
                terrainSplat.Apply();
            }

            GenerateTerrainSlope();
            GenerateObjectPoints();
        }

        public void GenerateTerrainSlope()
        {
            float heightRatioX = (terrainTop - terrainBottom) / terrainSize.x;
            float heightRatioY = (terrainTop - terrainBottom) / terrainSize.y;

            float SIN45 = 0.707106781f;

            float biggestSlope = 0f;

            terrainSlope = new Texture2D((int)splatResolution, (int)splatResolution, TextureFormat.RGBAFloat, false);
            terrain3D = new Texture2D((int)splatResolution, (int)splatResolution, TextureFormat.RGBAFloat, false);
            for (int x = 0; x < (int)splatResolution; x++)
            {
                for (int y = 0; y < (int)splatResolution; y++)
                {
                    Color targetPixel = new Color(0, 0, 0, 0);
                    Color target3D = new Color(0, 0, 0, 0);
                    Color terrainPixel = terrainSplat.GetPixel(x, y);

                    if (terrainPixel.a > 0)
                    {
                        float height = terrainPixel.r;

                        // Steepness
                        float xDiff = (x <= (int)splatResolution - 1) ? terrainPixel.r - terrainSplat.GetPixel(x + 1, y).r : 0;
                        float xAngle = Mathf.Abs(xDiff) / heightRatioX;
                        float xSlope = Mathf.Atan(xAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float yDiff = (y <= (int)splatResolution - 1) ? terrainPixel.r - terrainSplat.GetPixel(x, y + 1).r : 0;
                        float yAngle = Mathf.Abs(yDiff) / heightRatioY;
                        float ySlope = Mathf.Atan(yAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float dDiff = 0;
                        if (x <= (int)splatResolution - 1 && y <= (int)splatResolution - 1)
                        {
                            float fullD = terrainPixel.r - terrainSplat.GetPixel(x + 1, y + 1).r;
                            float origD = terrainPixel.r - terrainSplat.GetPixel(x, y).r;
                            dDiff = Mathf.Lerp(origD, fullD, SIN45);
                        }

                        float dAngle = Mathf.Abs(dDiff) / ((heightRatioX + heightRatioY) / 2);
                        float dSlope = Mathf.Atan(dAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float distance = Mathf.Max(Mathf.Clamp01(xSlope), Mathf.Clamp01(ySlope), Mathf.Clamp01(dSlope));
                        distance -= 0.5f;
                        distance = Mathf.Abs(distance) * 2f;

                        distance *= (float)splatResolution / (float)Resolutions._512x512;
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

            for (int x = 0; x < (int)splatResolution; x++)
            {
                for (int y = 0; y < (int)splatResolution; y++)
                {
                    float currentValue = terrainSlope.GetPixel(x, y).r;
                    float remapValue = currentValue / biggestSlope;
                    terrainSlope.SetPixel(x, y, new Color(remapValue, remapValue, remapValue, 1));
                }
            }

            terrainSlope.Apply();
        }

        public void GenerateObjectPoints()
        {
            foreach (ObjectLayer l in objectLayers)
            {
                GenerateObjectPoints(l);
            }
        }

        public void GenerateObjectPoints(ObjectLayer layer)
        {
            if (terrainSplat != null)
            {
                Texture2D measureTexture = terrainSplat;
                List<Vector2> placePoints = new List<Vector2>();

                layer.objectPlaces = new Texture2D((int)splatResolution, (int)splatResolution, TextureFormat.ARGB32, false);
                Color[] resetColorArray = layer.objectPlaces.GetPixels();
                for (int i = 0; i < resetColorArray.Length; i++)
                {
                    resetColorArray[i] = new Color(0, 0, 0, 0);
                }
                layer.objectPlaces.SetPixels(resetColorArray);

                for (int x = 0; x < (int)splatResolution; x += layer.everyN)//x++)
                {
                    for (int y = 0; y < (int)splatResolution; y += layer.everyN)//y++)
                    {

                        float angle = Random.Range(0f, 360f);
                        float xplus = Mathf.Sin(Mathf.Deg2Rad * angle) * layer.angleOffset;
                        float yplus = Mathf.Cos(Mathf.Deg2Rad * angle) * layer.angleOffset;

                        int xx = Mathf.RoundToInt((float)x + xplus);
                        int yy = Mathf.RoundToInt((float)y + yplus);

                        if (new Rect(0, 0, (float)splatResolution, (float)splatResolution).Contains(new Vector2(xx, yy)))
                        {
                            Color terrainPixel = measureTexture.GetPixel(xx, yy);
                            if (terrainPixel.a > 0)
                            {
                                bool isCross = layer.angleOffset > 0 || layer.everyN > 2;
                                if (isCross)
                                    DrawCross(layer.objectPlaces, xx, yy, 3, Color.red);
                                else
                                    layer.objectPlaces.SetPixel(xx, yy, Color.red);
                                placePoints.Add(new Vector2(xx, yy));
                            }
                        }
                    }
                }
                layer.objectPlaces.Apply();
                layer.objectPoints = placePoints.ToArray();
            }
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
            int coloredCount = 0;
            ClearObjects();

            if (terrainSplat != null)
            {
                for (int x = 0; x < (int)splatResolution; x++)
                {
                    for (int y = 0; y < (int)splatResolution; y++)
                    {
                        Color heightColor = terrainSplat.GetPixel(x, y);
                        if (heightColor.a > 0)
                        {
                            float height = heightColor.r;

                            Color slopeColor = terrainSlope.GetPixel(x, y);
                            float slope = slopeColor.r;

                            ObjectLayer layer = null;

                            foreach (ObjectLayer objectLayer in objectLayers)
                            {
                                Color placeColor = objectLayer.objectPlaces.GetPixel(x, y);
                                if (placeColor == Color.red)
                                {
                                    bool inHeightRange = height >= objectLayer.from && (height < objectLayer.to || (height == 1 && objectLayer.to == 1));
                                    if (inHeightRange)
                                    {
                                        bool isInSlopeRange = objectLayer.minSlope <= slope && objectLayer.maxSlope >= slope;
                                        if (isInSlopeRange)
                                        {
                                            layer = objectLayer;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (layer != null)
                            {
                                if (layer.placing == PlaceType.Prefabs)
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
                                        Vector3 posTop = heightmapToWorld(x, y);
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
                                            GameObject newObject = GameObject.Instantiate(obj.prefab, pos, Quaternion.identity);
                                            newObject.name = string.Format("{0} ({1})", obj.prefab.name, placedCount);
                                            newObject.transform.eulerAngles = Rotation.value;
                                            newObject.transform.localScale = Scale.value;

                                            newObject.transform.parent = transform;

                                            // coloring
                                            if (obj.colors != null)
                                            {
                                                MeshRenderer[] renderers = newObject.GetComponentsInChildren<MeshRenderer>();

                                                try
                                                {
                                                    foreach (MaterialColoring coloring in obj.colors)
                                                    {
                                                        if (colorGroupExists(coloring.colorGroup))
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
                                                                        coloredCount++;
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

                                            placedCount++;
                                        }
                                    }
                                }
                                else
                                {
                                    // TODO
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddColorGroup()
        {
            ColorGroup group = new ColorGroup();
            AddColorGroup(group);
        }
        public void AddColorGroup(ColorGroup group)
        {
            ColorGroup[] oldGroups = colorGroups;
            colorGroups = new ColorGroup[oldGroups.Length + 1];
            for (int i = 0; i < oldGroups.Length; i++)
            {
                colorGroups[i] = oldGroups[i];
            }
            colorGroups[oldGroups.Length] = group;
            selectedColorGroupIndex = colorGroups.Length - 1;
            colorGroups[selectedColorGroupIndex].opened = true;
        }
        public void RemoveColorGroup(int index)
        {
            List<ColorGroup> newList = new List<ColorGroup>();
            for (int i = 0; i < colorGroups.Length; i++)
            {
                if (i != index)
                    newList.Add(colorGroups[i]);
            }
            colorGroups = newList.ToArray();
            if (colorGroups.Length > 0)
            {
                selectedColorGroupIndex = colorGroups.Length - 1;
                colorGroups[selectedColorGroupIndex].opened = true;
            }
            else
                selectedColorGroupIndex = -1;
        }
        public void DuplicateColorGroup(int index)
        {
            if (selectedColorGroupIndex != -1)
                colorGroups[selectedColorGroupIndex].opened = false;

            ColorGroup newGroup = new ColorGroup(colorGroups[index]);
            newGroup.name += " (clone)";
            newGroup.opened = true;
            AddColorGroup(newGroup);

            selectedColorGroupIndex = colorGroups.Length - 1;
        }

        public void AddObjectLayer()
        {
            ObjectLayer layer = new ObjectLayer();

            layer._scale.x.min = 1;
            layer._scale.y.min = 1;
            layer._scale.z.min = 1;

            AddObjectLayer(layer);
        }
        public void AddObjectLayer(ObjectLayer layer)
        {
            ObjectLayer[] oldLayers = objectLayers;
            objectLayers = new ObjectLayer[oldLayers.Length + 1];

            for (int i = 0; i < oldLayers.Length; i++)
            {
                objectLayers[i] = oldLayers[i];
            }
            objectLayers[oldLayers.Length] = layer;
            selectedObjectLayerIndex = objectLayers.Length - 1;
            objectLayers[selectedObjectLayerIndex].opened = true;
        }
        public void RemoveObjectLayer(int index)
        {
            List<ObjectLayer> newList = new List<ObjectLayer>();
            for (int i = 0; i < objectLayers.Length; i++)
            {
                if (i != index)
                    newList.Add(objectLayers[i]);
            }
            objectLayers = newList.ToArray();
            if (objectLayers.Length > 0)
            {
                selectedObjectLayerIndex = objectLayers.Length - 1;
                objectLayers[selectedObjectLayerIndex].opened = true;
            }
            else
                selectedObjectLayerIndex = -1;
        }

        public void MoveObjectLayerUp(int index)
        {
            int toIndex = Mathf.Max(0, index - 1);
            MoveObjectLayer(index, toIndex);
        }

        public void MoveObjectLayerDown(int index)
        {
            int toIndex = Mathf.Min(objectLayers.Length - 1, index + 1);
            MoveObjectLayer(index, toIndex);
        }

        void MoveObjectLayer(int fromIndex, int toIndex)
        {
            if (fromIndex != toIndex)
            {
                ObjectLayer fromLayer = objectLayers[fromIndex];
                ObjectLayer toLayer = objectLayers[toIndex];

                ObjectLayer[] oldLayers = objectLayers;
                objectLayers = new ObjectLayer[oldLayers.Length];
                for (int i = 0; i < oldLayers.Length; i++)
                {
                    if (i == fromIndex || i == toIndex)
                    {
                        if (i == fromIndex)
                        {
                            objectLayers[i] = toLayer;
                        }
                        else
                        {
                            objectLayers[i] = fromLayer;
                        }
                    }
                    else
                    {
                        objectLayers[i] = oldLayers[i];
                    }
                }

                if (selectedObjectLayerIndex == fromIndex)
                {
                    selectedObjectLayerIndex = toIndex;
                    objectLayers[selectedObjectLayerIndex].opened = true;
                }
            }
        }
        public void DuplicateObjectLayer(int index)
        {
            if (selectedObjectLayerIndex != -1)
                objectLayers[selectedObjectLayerIndex].opened = false;

            ObjectLayer newLayer = new ObjectLayer(objectLayers[index]);
            newLayer.name += " (clone)";
            newLayer.opened = true;
            AddObjectLayer(newLayer);

            selectedObjectLayerIndex = objectLayers.Length - 1;
        }

        public Vector3 heightmapToWorld(int xx, int yy)
        {
            Vector3 centerTop = transform.position + new Vector3(terrainOffset.x, 0, terrainOffset.y) + Vector3.up * terrainTop;
            Vector3 centerCorner = centerTop + new Vector3(-terrainSize.x / 2, 0, -terrainSize.y / 2);

            float xRatio = (float)xx / (float)splatResolution;
            float yRatio = (float)yy / (float)splatResolution;

            float absX = terrainSize.x * xRatio;
            float absY = terrainSize.y * yRatio;

            return centerCorner + new Vector3(absX, 0, absY);
        }

        public static bool isSRP => GraphicsSettings.renderPipelineAsset != null;

        int generatedPreviewTexturesResolution = -1;
        RenderTexture heightmapPreviewTexture = null;
        RenderTexture layerPreviewTexture = null;
        ComputeShader previewComputeShader = null;

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
            previewComputeShader.SetTexture(kernelIndex, "Heightmap", terrainSplat);
            previewComputeShader.SetTexture(kernelIndex, "SlopeMap", terrainSlope);
            previewComputeShader.SetTexture(kernelIndex, "PlacementMap", objectLayers[selectedObjectLayerIndex].objectPlaces);
            previewComputeShader.SetVector("HeightRange", new Vector4(objectLayers[selectedObjectLayerIndex].from, objectLayers[selectedObjectLayerIndex].to, 0, 0));
            previewComputeShader.SetVector("SlopeRange", new Vector4(objectLayers[selectedObjectLayerIndex].minSlope, objectLayers[selectedObjectLayerIndex].maxSlope, 0, 0));
            previewComputeShader.SetBool("ShowPlacement", showPlacement);

            previewComputeShader.Dispatch(kernelIndex, heightmapPreviewTexture.width / 8, heightmapPreviewTexture.height / 8, 1);

            return layerPreviewTexture;
        }
        void CheckPreviewTextures()
        {
            if (generatedPreviewTexturesResolution == (int)splatResolution && heightmapPreviewTexture != null && layerPreviewTexture != null && previewComputeShader != null)
                return;

            generatedPreviewTexturesResolution = (int)splatResolution;

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
        }


        public static bool LayerIncluded(LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static LayerMask AddLayer(LayerMask mask, int layer)
        {
            LayerMask newMask = mask;
            newMask |= (1 << layer);
            return newMask;
        }
        public static bool[] HasLayers(LayerMask layerMask)
        {
            var hasLayers = new bool[32];

            for (int i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    hasLayers[i] = true;
                }
            }

            return hasLayers;
        }

        public static void DrawCross(Texture2D target, int xx, int yy, int length, Color color)
        {
            for (int i = 0; i < length * 2 + 1; i++)
            {
                int xHor = xx - length + i;
                int yHor = yy;
                int xVer = xx;
                int yVer = yy - length + i;

                float horDistance = Mathf.Pow(1f - Mathf.Clamp01(Mathf.Abs((float)xx - (float)xHor) / (float)length), 2);
                float verDistance = Mathf.Pow(1f - Mathf.Clamp01(Mathf.Abs((float)yy - (float)yVer) / (float)length), 2);

                Color currentHorColor = target.GetPixel(xHor, yHor);
                Color horColor = new Color(color.r, color.g, color.b, Mathf.Max(horDistance, currentHorColor.a));
                Color currentVerColor = target.GetPixel(xVer, yVer);
                Color verColor = new Color(color.r, color.g, color.b, Mathf.Max(verDistance, currentVerColor.a));

                target.SetPixel(xHor, yHor, horColor);
                target.SetPixel(xVer, yVer, verColor);
            }
        }

        public static Color LerpColor(Color a, Color b, float r)
        {
            float _r = Mathf.Clamp01(r);
            return new Color(Mathf.Lerp(a.r, b.r, _r), Mathf.Lerp(a.g, b.g, _r), Mathf.Lerp(a.b, b.b, _r), Mathf.Lerp(a.a, b.a, _r));
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
        private void OnDrawGizmosSelected()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                // World bounds
                Vector3 worldCenter = transform.position + Vector3.up * ((terrainBottom + terrainTop) / 2f) + new Vector3(terrainOffset.x, 0, terrainOffset.y);
                Vector3 worldSize = new Vector3(terrainSize.x, terrainTop - terrainBottom, terrainSize.y);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(worldCenter, worldSize);

                // Placement preview
                if (objectLayers == null || objectLayers.Length == 0 || !showPlacement || selectedObjectLayerIndex == -1 || terrainSplat == null || transform.childCount != 0)
                    return;

                // Preview cubes
                previewObjectLayer = objectLayers[selectedObjectLayerIndex];
                if (previewObjectLayer.objectPoints == null || previewObjectLayer.objectPoints.Length == 0)
                {
                    GenerateObjectPoints(previewObjectLayer);
                    return;
                }

                Gizmos.color = Color.red;
                foreach (Vector2 v in previewObjectLayer.objectPoints)
                {
                    prevX = (int)v.x;
                    prevY = (int)v.y;

                    previewHeightColor = terrainSplat.GetPixel(prevX, prevY);
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
                    Vector3 posTop = heightmapToWorld(prevX, prevY);
                    Vector3 pos = posTop + Vector3.down * heightRatio * (terrainTop - terrainBottom) + Offset.value;

                    Gizmos.DrawCube(pos + Vector3.up * 1.5f, Vector3.one * 3);
                }
            }
        }
#endif
    }


    [System.Serializable]
    public class RandomBetween
    {
        public float min = 0f;
        public float max = 1f;
        public bool random = false;

        public float value => random && min != max ? Random.Range(min, max) : min;
        public float avarage => random && min != max ? (min + max) / 2 : min;
    }

    [System.Serializable]
    public class RandomVector3
    {
        public bool _override = false;
        public RandomBetween x;
        public RandomVectorModeX xMode = RandomVectorModeX.Fixed;
        public RandomBetween y;
        public RandomVectorModeY yMode = RandomVectorModeY.Fixed;
        public RandomBetween z;
        public RandomVectorModeZ zMode = RandomVectorModeZ.Fixed;

        public float min = 0;
        public float max = 1;
        public bool clamp = false;
        public bool whole = false;

        public RandomVector3()
        {
            x = new RandomBetween();
            y = new RandomBetween();
            z = new RandomBetween();
        }

        public Vector3 avarage
        {
            get
            {
                x.random = xMode == RandomVectorModeX.Random;
                y.random = yMode == RandomVectorModeY.Random;
                z.random = zMode == RandomVectorModeZ.Random;

                float xx = x.avarage;
                float yy = y.avarage;
                float zz = z.avarage;

                bool yCopy = false; bool zCopy = false;
                if (zMode == RandomVectorModeZ.CopyX)
                {
                    zz = xx;
                    zCopy = true;
                }
                else if (zMode == RandomVectorModeZ.CopyY)
                {
                    zz = yy;
                    zCopy = true;
                }

                if (yMode == RandomVectorModeY.CopyX)
                {
                    yy = xx;
                    yCopy = true;
                }
                else if (yMode == RandomVectorModeY.CopyZ)
                {
                    yy = zz;
                    yCopy = true;
                }

                if (yCopy && zCopy && (xMode == RandomVectorModeX.CopyY || xMode == RandomVectorModeX.CopyZ))
                {
                    xMode = RandomVectorModeX.Fixed;
                }
                if (xMode == RandomVectorModeX.CopyY)
                {
                    xx = yy;
                }
                else if (yMode == RandomVectorModeY.CopyZ)
                {
                    xx = zz;
                }

                Vector3 result = new Vector3(xx, yy, zz);

                if (clamp)
                    result = new Vector3(Mathf.Clamp(result.x, min, max), Mathf.Clamp(result.y, min, max), Mathf.Clamp(result.z, min, max));

                if (whole)
                    result = new Vector3(Mathf.Floor(result.x), Mathf.Floor(result.y), Mathf.Floor(result.z));

                return result;
            }
        }

        public Vector3 value
        {
            get
            {
                x.random = xMode == RandomVectorModeX.Random;
                y.random = yMode == RandomVectorModeY.Random;
                z.random = zMode == RandomVectorModeZ.Random;

                float xx = x.value;
                float yy = y.value;
                float zz = z.value;

                bool yCopy = false; bool zCopy = false;
                if (zMode == RandomVectorModeZ.CopyX)
                {
                    zz = xx;
                    zCopy = true;
                }
                else if (zMode == RandomVectorModeZ.CopyY)
                {
                    zz = yy;
                    zCopy = true;
                }

                if (yMode == RandomVectorModeY.CopyX)
                {
                    yy = xx;
                    yCopy = true;
                }
                else if (yMode == RandomVectorModeY.CopyZ)
                {
                    yy = zz;
                    yCopy = true;
                }

                if (yCopy && zCopy && (xMode == RandomVectorModeX.CopyY || xMode == RandomVectorModeX.CopyZ))
                {
                    xMode = RandomVectorModeX.Fixed;
                }
                if (xMode == RandomVectorModeX.CopyY)
                {
                    xx = yy;
                }
                else if (yMode == RandomVectorModeY.CopyZ)
                {
                    xx = zz;
                }

                Vector3 result = new Vector3(xx, yy, zz);

                if (clamp)
                    result = new Vector3(Mathf.Clamp(result.x, min, max), Mathf.Clamp(result.y, min, max), Mathf.Clamp(result.z, min, max));

                if (whole)
                    result = new Vector3(Mathf.Floor(result.x), Mathf.Floor(result.y), Mathf.Floor(result.z));

                return result;
            }
        }
    }

    [System.Serializable]
    public enum RandomVectorModeX
    {
        Fixed = 0,
        Random = 1,
        CopyY = 3,
        CopyZ = 4
    }
    [System.Serializable]
    public enum RandomVectorModeY
    {
        Fixed = 0,
        Random = 1,
        CopyX = 2,
        CopyZ = 4
    }
    [System.Serializable]
    public enum RandomVectorModeZ
    {
        Fixed = 0,
        Random = 1,
        CopyX = 2,
        CopyY = 3
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

    [System.Serializable]
    public class ColorGroup
    {
        public string name = string.Empty;
        public ColorGroupMode mode = ColorGroupMode.RGB;

        public RandomVector3 rgb;
        public RandomVector3 hsv;
        public Gradient gradient;
        public Color color1 = Color.white;
        public Color color2 = Color.white;

        public bool opened = false;

        public ColorGroup()
        {
            rgb = new RandomVector3();
            rgb.x.min = 255;
            rgb.x.max = 255;
            rgb.y.min = 255;
            rgb.y.max = 255;
            rgb.z.min = 255;
            rgb.z.max = 255;
            rgb.min = 0;
            rgb.max = 255;
            rgb.clamp = true;
            rgb.whole = true;

            hsv = new RandomVector3();
            hsv.z.min = 100;
            hsv.z.max = 100;
            hsv.min = 0;
            hsv.max = 100;
            hsv.clamp = true;
            hsv.whole = true;

            gradient = new Gradient();
        }
        public ColorGroup(ColorGroup source)
        {
            name = source.name;
            mode = source.mode;
            rgb = source.rgb;
            hsv = source.hsv;
            gradient = source.gradient;
            color1 = source.color1;
            color2 = source.color2;
        }

        public Color value
        {
            get
            {
                if (mode == ColorGroupMode.RGB)
                {
                    Vector3 rgbValue = rgb.value;
                    return new Color(rgbValue.x / 255f, rgbValue.y / 255f, rgbValue.z / 255f);
                }
                else if (mode == ColorGroupMode.HSV)
                {
                    Vector3 hsvValue = hsv.value;
                    return Color.HSVToRGB(hsvValue.x / 100f, hsvValue.y / 100f, hsvValue.z / 100f);
                }
                else if (mode == ColorGroupMode.Gradient)
                {
                    float v = Random.Range(0f, 1f);
                    return gradient.Evaluate(v);
                }

                return ColorLerp(color1, color2, Random.Range(0f, 1f));
            }
        }

        public Color avarage
        {
            get
            {
                if (mode == ColorGroupMode.RGB)
                {
                    Vector3 rgbValue = rgb.avarage;
                    return new Color(rgbValue.x / 255f, rgbValue.y / 255f, rgbValue.z / 255f);
                }
                else if (mode == ColorGroupMode.HSV)
                {
                    Vector3 hsvValue = hsv.avarage;
                    return Color.HSVToRGB(hsvValue.x / 100f, hsvValue.y / 100f, hsvValue.z / 100f);
                }

                float v = 0.5f;
                return ColorLerp(color1, color2, v);
            }
        }

        public static Color ColorLerp(Color a, Color b, float t)
        {
            float _t = Mathf.Clamp01(t);
            return new Color(Mathf.Lerp(a.r, b.r, _t), Mathf.Lerp(a.g, b.g, _t), Mathf.Lerp(a.b, b.b, _t));
        }
    }

    [System.Serializable]
    public enum ColorGroupMode
    {
        RGB,
        HSV,
        Gradient,
        ColorLerp
    }

    [System.Serializable]
    public enum PlaceType
    {
        Prefabs,
        TerrainTrees
    }

    [System.Serializable]
    public class PlaceObject
    {
        public GameObject prefab;
        public RandomVector3 _position;
        public RandomVector3 _rotation;
        public RandomVector3 _scale;
        public bool transform = false;
        public MaterialColoring[] colors = null;
        public bool coloring = false;

        public void AddColoring()
        {
            MaterialColoring coloring = new MaterialColoring(MassSpawner.isSRP);
            AddColoring(coloring);
        }
        public void AddColoring(MaterialColoring coloring)
        {
            MaterialColoring[] oldColoring = colors;
            colors = new MaterialColoring[oldColoring.Length + 1];
            for (int i = 0; i < oldColoring.Length; i++)
            {
                colors[i] = oldColoring[i];
            }
            colors[oldColoring.Length] = coloring;
        }
        public void RemoveColoring(int index)
        {
            List<MaterialColoring> newList = new List<MaterialColoring>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (i != index)
                    newList.Add(colors[i]);
            }
            colors = newList.ToArray();
        }
    }

    [System.Serializable]
    public class MaterialColoring
    {
        public Material material = null;
        public string propertyName = "_BaseColor";
        public int colorGroup = -1;

        public MaterialColoring(bool isSrp)
        {
            propertyName = isSrp ? "_BaseColor" : "_Color";
        }
    }

    [System.Serializable]
    public enum CollisionMode
    {
        DontSpawn,
        DeleteOther
    }

    [System.Serializable]
    public class CollisionRule
    {
        public CollisionMode mode = CollisionMode.DontSpawn;
        public LayerMask layerMask;
        public float radius = 1f;
        public bool stop = false;
    }

    [System.Serializable]
    public class ObjectLayer
    {
        public bool opened = true;

        public string name = string.Empty;

        public float from = 0f;
        public float to = 1f;

        public float minSlope = 0f;
        public float maxSlope = 1f;

        public float smoothing = 0f;

        public PlaceObject[] prefabs = null;
        public int[] treeObjects = null;

        public PlaceType placing = PlaceType.Prefabs;

        public bool _transform = false;
        public RandomVector3 _position;
        public RandomVector3 _rotation;
        public RandomVector3 _scale;

        public Texture2D objectPlaces = null;
        public Vector2[] objectPoints = null;
        public int everyN = 10;
        public float angleOffset = 5f;

        public bool _collision = false;
        public CollisionRule[] collisionRules = null;

        public ObjectLayer()
        {
            _position = new RandomVector3();
            _rotation = new RandomVector3();
            _rotation.min = 0;
            _rotation.max = 360;
            _rotation.clamp = true;
            _scale = new RandomVector3();
        }

        public ObjectLayer(ObjectLayer target)
        {
            name = target.name;
            from = target.from;
            to = target.to;
            minSlope = target.minSlope;
            maxSlope = target.maxSlope;
            smoothing = target.smoothing;
            prefabs = target.prefabs;
            treeObjects = target.treeObjects;
            placing = target.placing;
            _position = target._position;
            _rotation = target._rotation;
            _rotation.min = 0;
            _rotation.max = 360;
            _rotation.clamp = true;
            _scale = target._scale;
            objectPlaces = target.objectPlaces;
            everyN = target.everyN;
            angleOffset = target.angleOffset;
            collisionRules = target.collisionRules;
        }

        public PlaceObject[] okPrefabs
        {
            get
            {
                List<PlaceObject> result = new List<PlaceObject>();
                foreach (PlaceObject obj in prefabs)
                {
                    if (obj.prefab != null)
                        result.Add(obj);
                }
                return result.ToArray();
            }
        }
        public int okPrefabsCount
        {
            get
            {
                int result = 0;
                foreach (PlaceObject obj in prefabs)
                {
                    if (obj.prefab != null)
                        result++;
                }
                return result;
            }
        }

        public int[] okTreeObjects
        {
            get
            {
                List<int> result = new List<int>();
                foreach (int obj in treeObjects)
                {
                    if (obj != -1)
                        result.Add(obj);
                }
                return result.ToArray();
            }
        }

        public void CheckArrays()
        {
            if (prefabs == null)
                prefabs = new PlaceObject[0];
            if (treeObjects == null)
                treeObjects = new int[0];
            if (collisionRules == null)
                collisionRules = new CollisionRule[0];
        }
        public void AddRule()
        {
            AddRule(new CollisionRule());
        }
        public void AddRule(CollisionRule rule)
        {
            CollisionRule[] oldRules = collisionRules;
            collisionRules = new CollisionRule[oldRules.Length + 1];
            for (int i = 0; i < oldRules.Length; i++)
            {
                collisionRules[i] = oldRules[i];
            }
            collisionRules[oldRules.Length] = rule;
        }
        public void RemoveRule(int index)
        {
            List<CollisionRule> newList = new List<CollisionRule>();
            for (int i = 0; i < collisionRules.Length; i++)
            {
                if (i != index)
                    newList.Add(collisionRules[i]);
            }
            collisionRules = newList.ToArray();
        }
        public void MoveRuleUp(int index)
        {
            int toIndex = Mathf.Max(0, index - 1);

            MoveRule(index, toIndex);
        }
        public void MoveRuleDown(int index)
        {
            int toIndex = Mathf.Min(collisionRules.Length - 1, index + 1);

            MoveRule(index, toIndex);
        }
        public void MoveRule(int fromIndex, int toIndex)
        {
            if (fromIndex != toIndex)
            {
                CollisionRule fromRule = collisionRules[fromIndex];
                CollisionRule toRule = collisionRules[toIndex];

                CollisionRule[] oldRules = collisionRules;
                collisionRules = new CollisionRule[oldRules.Length];
                for (int i = 0; i < oldRules.Length; i++)
                {
                    if (i == fromIndex || i == toIndex)
                    {
                        if (i == fromIndex)
                        {
                            collisionRules[i] = toRule;
                        }
                        else
                        {
                            collisionRules[i] = fromRule;
                        }
                    }
                    else
                    {
                        collisionRules[i] = oldRules[i];
                    }
                }
            }
        }
        public void AddPrefab()
        {
            AddPrefab(null);
        }
        public void AddPrefab(GameObject prefab)
        {
            PlaceObject[] oldPrefabs = prefabs;
            prefabs = new PlaceObject[oldPrefabs.Length + 1];
            for (int i = 0; i < oldPrefabs.Length; i++)
            {
                prefabs[i] = oldPrefabs[i];
            }
            PlaceObject p = new PlaceObject();
            p.prefab = prefab;
            prefabs[oldPrefabs.Length] = p;
        }
        public void RemovePrefab(int index)
        {
            List<PlaceObject> newList = new List<PlaceObject>();
            for (int i = 0; i < prefabs.Length; i++)
            {
                if (i != index)
                    newList.Add(prefabs[i]);
            }
            prefabs = newList.ToArray();
        }
        public void AddTreeObject()
        {
            AddTreeObject(-1);
        }
        public void AddTreeObject(int treeObject)
        {
            int[] oldTrees = treeObjects;
            treeObjects = new int[oldTrees.Length + 1];
            for (int i = 0; i < oldTrees.Length; i++)
            {
                treeObjects[i] = oldTrees[i];
            }
            treeObjects[oldTrees.Length] = treeObject;
        }
        public void RemoveTreeObject(int index)
        {
            List<int> newList = new List<int>();
            for (int i = 0; i < treeObjects.Length; i++)
            {
                if (i != index)
                    newList.Add(treeObjects[i]);
            }
            treeObjects = newList.ToArray();
        }
    }
}