
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditorInternal;
using System.IO;

/*

    TODO:
    
    Performance:
    -   optimize object place script
    -   use compute shaders

    Features:
    -   Terrain normal as rotation (with ratio)

*/

namespace lxkvcs
{
    [CustomEditor(typeof(MassSpawner))]
    public class MassSpawnerEditor : Editor
    {
        MassSpawner spawner;
        bool heightmapUpdate = false;
        bool heightmapAutoUpdate = false;

        bool placeObjects = false;

        const int TAB_LAYERS = 2;
        const int TAB_COLORS = 3;
        const int TAB_HEIGHTMAP = 1;
        const int TAB_WORLD = 0;
        int selectedTab = -1;

        private void Awake()
        {
            spawner = target as MassSpawner;
            heightmapUpdate = true;
        }

        private static string GetMonoScriptPathFor(Type type)
        {
            var asset = "";

            var guids = AssetDatabase.FindAssets(string.Format("{0} t:script", type.Name));

            if (guids.Length > 1)
            {
                foreach (var guid in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                    var filename = Path.GetFileNameWithoutExtension(assetPath);

                    if (filename == type.Name)
                    {
                        asset = guid;
                        break;
                    }
                }
            }
            else if (guids.Length == 1)
            {
                asset = guids[0];
            }
            else
            {
                Debug.LogErrorFormat("Unable to locate {0}", type.Name);
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(asset);
        }

        public string AssetFolder()
        {
            string scriptPath = GetMonoScriptPathFor(typeof(MassSpawner));
            return scriptPath.Replace("/Scripts/MassSpawner.cs", "");
        }

        public override void OnInspectorGUI()
        {
            if (selectedTab == -1)
                selectedTab = spawner.terrainSplat != null ? TAB_LAYERS : TAB_HEIGHTMAP;

            // Spawner arrays check
            if (spawner.objectLayers == null)
                spawner.objectLayers = new ObjectLayer[0];
            if (spawner.colorGroups == null)
                spawner.colorGroups = new ColorGroup[0];

            string assetsFolder = AssetFolder();

            // Top region variables
            int iconsize = 40;
            string[] names = EnumNames<PreviewMode>();
            Texture2D[] icons = new Texture2D[]
            {
                (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_heightmap.png", typeof(Texture2D)),
                (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_slopemap.png", typeof(Texture2D)),
                (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_3d.png", typeof(Texture2D))
            };
            Texture2D objectIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_object.png", typeof(Texture2D));
            Texture2D upIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_up.png", typeof(Texture2D));
            Texture2D downIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_down.png", typeof(Texture2D));

            string[] labels = new string[]
            {
                "Terrain height",
                "Steepness and slopes",
                "Stylized 3D"
            };

            float inspectorSize = EditorGUIUtility.currentViewWidth - 90;
            Rect previewRect = new Rect(70, 40, inspectorSize, inspectorSize);

            // Preview mode label
            GUI.Label(new Rect(20, 10, 120, 20), labels[(int)spawner.preview]);

            // Update & Delete objects buttons
            GUILayout.BeginHorizontal();
            GUILayout.Space(140);
            if (spawner.objectLayers.Length > 0 && spawner.terrainSplat != null)
            {
                if (GUILayout.Button(spawner.transform.childCount > 0 ? "Update objects" : "Place objects", GUILayout.Height(30)))
                {
                    placeObjects = true;
                }
            }
            if (spawner.transform.childCount > 0)
            {
                if (GUILayout.Button(string.Format("Delete objects ({0})", spawner.transform.childCount), GUILayout.Height(30)))
                {
                    spawner.ClearObjects();
                }
            }
            if ((spawner.objectLayers.Length == 0 || spawner.terrainSplat == null) && spawner.transform.childCount == 0)
                EditorGUILayout.Space(32);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // Preview mode selector
            spawner.preview = (PreviewMode)GUILayout.SelectionGrid((int)spawner.preview, icons, 1, GUILayout.Width(iconsize));

            EditorGUILayout.Space(10);

            // Preview show object placement toggle
            if (spawner.selectedObjectLayerIndex != -1)
                spawner.showPlacement = GUILayout.Toggle(spawner.showPlacement, objectIcon, "Button", GUILayout.Height(iconsize), GUILayout.Width(iconsize));
            else
                EditorGUILayout.Space(iconsize + 4);

            if (spawner.selectedObjectLayerIndex != -1 && spawner.objectLayers[spawner.selectedObjectLayerIndex].objectPlaces == null)
                spawner.GenerateObjectPoints(spawner.objectLayers[spawner.selectedObjectLayerIndex]);

            // Preview drawing
            Texture2D previewTexture = spawner.terrainSplat;
            if (spawner.preview == PreviewMode.Slopemap)
                previewTexture = spawner.terrainSlope;
            else if (spawner.preview == PreviewMode._3D)
                previewTexture = spawner.terrain3D;

            if (previewTexture != null)
                GUI.DrawTexture(previewRect, spawner.GenerateHeightmapPreviewTexture(previewTexture), ScaleMode.ScaleToFit);

            if (spawner.terrainSplat != null && spawner.terrainSlope != null && spawner.selectedObjectLayerIndex != -1)
                GUI.DrawTexture(previewRect, spawner.GenerateLayerPreviewTexture(), ScaleMode.ScaleToFit);

            EditorGUILayout.Space(inspectorSize - (names.Length + 1) * iconsize);

            // Middle toolbar (below the preview)
            selectedTab = GUILayout.Toolbar(selectedTab, new string[] {
                "World settings",
                "Heightmap",
                string.Format("Layers ({0})", spawner.objectLayers.Length),
                string.Format("Color groups ({0})", spawner.colorGroups.Length)
            });

            EditorGUILayout.Space(10);

            // Heightmap generation & settings
            if (selectedTab == TAB_HEIGHTMAP)
            {
                spawner.splatResolution = CheckChange(spawner.splatResolution, (Resolutions)EditorGUILayout.EnumPopup("Resolution", spawner.splatResolution));

                spawner.includeMask = CheckChange(spawner.includeMask, LayerMaskField("Terrain layers", spawner.includeMask));
                spawner.excludeMask = CheckChange(spawner.excludeMask, LayerMaskField("Exclude layers", spawner.excludeMask));

                EditorGUILayout.BeginHorizontal();
                if (!heightmapAutoUpdate)
                {
                    if (GUILayout.Button("Generate heightmap"))
                        spawner.GenerateHeightmap();
                }
                if (GUILayout.Button(string.Format("Auto: {0}", heightmapAutoUpdate ? "ON" : "OFF")))
                {
                    heightmapAutoUpdate = !heightmapAutoUpdate;
                    if (heightmapAutoUpdate)
                        heightmapUpdate = true;
                }
                EditorGUILayout.EndHorizontal();
            }

            // World settings
            if (selectedTab == TAB_WORLD)
            {
                spawner.terrainTop = CheckChange(spawner.terrainTop, EditorGUILayout.FloatField("Top", spawner.terrainTop));
                spawner.terrainBottom = CheckChange(spawner.terrainBottom, EditorGUILayout.FloatField("Bottom", spawner.terrainBottom));
                spawner.terrainOffset = CheckChange(spawner.terrainOffset, EditorGUILayout.Vector2Field("Offset", spawner.terrainOffset));
                spawner.terrainSize = CheckChange(spawner.terrainSize, EditorGUILayout.Vector2Field("Size", spawner.terrainSize));
            }


            int columnWidth = 100;
            // Color groups
            if (selectedTab == TAB_COLORS)
            {
                EditorGUILayout.BeginHorizontal();

                // Color group selector
                EditorGUILayout.BeginVertical();
                int index = 0;
                foreach (ColorGroup colorGroup in spawner.colorGroups)
                {
                    colorGroup.opened = CloseAllColorGroups(ButtonTitle(maxLength(colorGroup.name, 15), colorGroup.opened, string.Format("Color group #{0}", index), columnWidth));
                    index++;
                }
                GUILayout.Space(10);
                if (GUILayout.Button("Add color group", GUILayout.Width(columnWidth)))
                {
                    CloseAllColorGroups();
                    spawner.AddColorGroup();
                }
                EditorGUILayout.EndVertical();

                // Color group settings
                if (spawner.selectedColorGroupIndex != -1)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    bool remove = false;
                    bool duplicate = false;

                    // Duplicate & Remove buttons
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Duplicate group"))
                        duplicate = true;
                    if (GUILayout.Button("Remove group"))
                        remove = true;
                    EditorGUILayout.EndHorizontal();

                    // Name
                    spawner.colorGroups[spawner.selectedColorGroupIndex].name = EditorGUILayout.TextField("Name", spawner.colorGroups[spawner.selectedColorGroupIndex].name);

                    // Mode
                    spawner.colorGroups[spawner.selectedColorGroupIndex].mode = (ColorGroupMode)EditorGUILayout.EnumPopup("Mixing mode", spawner.colorGroups[spawner.selectedColorGroupIndex].mode);

                    EditorGUILayout.Space();
                    if (spawner.colorGroups[spawner.selectedColorGroupIndex].mode == ColorGroupMode.RGB)
                        spawner.colorGroups[spawner.selectedColorGroupIndex].rgb = RandomVectorGUI(spawner.colorGroups[spawner.selectedColorGroupIndex].rgb, "R", "G", "B");
                    else if (spawner.colorGroups[spawner.selectedColorGroupIndex].mode == ColorGroupMode.HSV)
                        spawner.colorGroups[spawner.selectedColorGroupIndex].hsv = RandomVectorGUI(spawner.colorGroups[spawner.selectedColorGroupIndex].hsv, "H", "S", "V");
                    else if (spawner.colorGroups[spawner.selectedColorGroupIndex].mode == ColorGroupMode.Gradient)
                        spawner.colorGroups[spawner.selectedColorGroupIndex].gradient = EditorGUILayout.GradientField(spawner.colorGroups[spawner.selectedColorGroupIndex].gradient);
                    else if (spawner.colorGroups[spawner.selectedColorGroupIndex].mode == ColorGroupMode.ColorLerp)
                    {
                        EditorGUILayout.BeginHorizontal();
                        spawner.colorGroups[spawner.selectedColorGroupIndex].color1 = EditorGUILayout.ColorField(spawner.colorGroups[spawner.selectedColorGroupIndex].color1);
                        spawner.colorGroups[spawner.selectedColorGroupIndex].color2 = EditorGUILayout.ColorField(spawner.colorGroups[spawner.selectedColorGroupIndex].color2);
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.ColorField("Preview color", spawner.colorGroups[spawner.selectedColorGroupIndex].value);

                    GUILayout.EndVertical();

                    if (remove)
                    {
                        CloseAllColorGroups();
                        spawner.RemoveColorGroup(spawner.selectedColorGroupIndex);
                    }
                    if (duplicate)
                    {
                        CloseAllColorGroups();
                        spawner.DuplicateColorGroup(spawner.selectedColorGroupIndex);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a color group from the left side to customize its settings", MessageType.Info);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Object layers
            if (selectedTab == TAB_LAYERS)
            {
                EditorGUILayout.BeginHorizontal();

                // Layer selector
                EditorGUILayout.BeginVertical();
                int index = 0;
                foreach (ObjectLayer objectLayer in spawner.objectLayers)
                {
                    objectLayer.opened = CloseAllLayers(ButtonTitle(maxLength(objectLayer.name, 15), objectLayer.opened, string.Format("Object layer #{0}", index), columnWidth));
                    index++;
                }
                GUILayout.Space(10);
                if (GUILayout.Button("Add layer", GUILayout.Width(columnWidth)))
                {
                    CloseAllLayers();
                    spawner.AddObjectLayer();
                }

                EditorGUILayout.EndVertical();

                // Layer settings
                if (spawner.selectedObjectLayerIndex != -1)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    bool remove = false;
                    bool move_up = false;
                    bool move_down = false;
                    bool duplicate = false;

                    // Move, Duplicate & Remove buttons
                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button(upIcon, GUILayout.Width(24)))
                        move_up = true;
                    if (GUILayout.Button(downIcon, GUILayout.Width(24)))
                        move_down = true;
                    if (GUILayout.Button("Duplicate layer"))
                        duplicate = true;
                    if (GUILayout.Button("Remove layer"))
                        remove = true;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    // Name
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].name = EditorGUILayout.TextField("Name", spawner.objectLayers[spawner.selectedObjectLayerIndex].name);

                    EditorGUILayout.Space();

                    // Terrain height

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Terrain height");
                    GUILayout.Space(10);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].from = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].from, GUILayout.MaxWidth(40)));
                    EditorGUILayout.MinMaxSlider(ref spawner.objectLayers[spawner.selectedObjectLayerIndex].from, ref spawner.objectLayers[spawner.selectedObjectLayerIndex].to, 0, 1);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].to = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].to, GUILayout.MaxWidth(40)));
                    GUILayout.EndHorizontal();

                    // Terrain slope
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Terrain slope");
                    GUILayout.Space(10);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].minSlope = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].minSlope, GUILayout.MaxWidth(40)));
                    EditorGUILayout.MinMaxSlider(ref spawner.objectLayers[spawner.selectedObjectLayerIndex].minSlope, ref spawner.objectLayers[spawner.selectedObjectLayerIndex].maxSlope, 0, 1);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].maxSlope = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].maxSlope, GUILayout.MaxWidth(40)));
                    GUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    // Rarity & angleOffset
                    float everyN = spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN;
                    float angleOffset = spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset;
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN = 30 - EditorGUILayout.IntSlider("Placement density", 30 - spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN, 0, 27);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset = EditorGUILayout.Slider("Placement chatotics", spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset, 0, 30);

                    if (everyN != spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN || angleOffset != spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset)
                        spawner.GenerateObjectPoints(spawner.objectLayers[spawner.selectedObjectLayerIndex]);


                    // CHECKING ARRAYS INITIALIZATION
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].CheckArrays();

                    EditorGUILayout.Space(14);

                    // Collision
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(12);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex]._collision = EditorGUILayout.BeginFoldoutHeaderGroup(spawner.objectLayers[spawner.selectedObjectLayerIndex]._collision, string.Format("Collision rules ({0})", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules.Length));
                    GUILayout.EndHorizontal();
                    if (spawner.objectLayers[spawner.selectedObjectLayerIndex]._collision)
                    {
                        for (int i = 0; i < spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules.Length; i++)
                        {
                            GUILayout.BeginVertical(EditorStyles.textArea);

                            spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].mode = (CollisionMode)EditorGUILayout.EnumPopup("Mode", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].mode);

                            spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask = LayerMaskField("Detection layers", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask);

                            float newRadius = EditorGUILayout.FloatField("Detection radius", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].radius);
                            if (newRadius > 0)
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].radius = newRadius;

                            if (i < spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules.Length - 1)
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].stop = EditorGUILayout.Toggle("Ignore rules below if detected", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].stop);

                            GUILayout.BeginHorizontal();

                            if (GUILayout.Button(upIcon, GUILayout.Width(24)))
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].MoveRuleUp(i);
                            if (GUILayout.Button(downIcon, GUILayout.Width(24)))
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].MoveRuleDown(i);

                            if (GUILayout.Button("Delete rule"))
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].RemoveRule(i);

                            GUILayout.EndHorizontal();

                            GUILayout.EndVertical();

                            EditorGUILayout.Space(6);
                        }
                        if (GUILayout.Button("Add new rule"))
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].AddRule();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    EditorGUILayout.Space();

                    // Transform
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(12);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex]._transform = EditorGUILayout.BeginFoldoutHeaderGroup(spawner.objectLayers[spawner.selectedObjectLayerIndex]._transform, "Transform settings");
                    GUILayout.EndHorizontal();
                    if (spawner.objectLayers[spawner.selectedObjectLayerIndex]._transform)
                    {
                        spawner.objectLayers[spawner.selectedObjectLayerIndex]._position = RandomVectorGUI("Offset", spawner.objectLayers[spawner.selectedObjectLayerIndex]._position);
                        spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation = RandomVectorGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation);
                        spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale = RandomVectorGUI("Scale", spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale);
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    EditorGUILayout.Space(14);

                    GUI.enabled = false;
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].placing = (PlaceType)EditorGUILayout.EnumPopup("Objects", spawner.objectLayers[spawner.selectedObjectLayerIndex].placing);
                    GUI.enabled = true;


                    EditorGUILayout.Space(4);

                    // Prefabs
                    if (spawner.objectLayers[spawner.selectedObjectLayerIndex].placing == PlaceType.Prefabs)
                    {
                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length == 0)
                            EditorGUILayout.HelpBox("You need to add prefabs to this layer in order to use it", MessageType.Info);

                        for (int i = 0; i < spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length; i++)
                        {
                            bool deleteLayer = false;

                            GUILayout.BeginVertical(EditorStyles.textArea);

                            EditorGUILayout.BeginHorizontal();
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].prefab = (GameObject)EditorGUILayout.ObjectField(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].prefab, typeof(GameObject), false);
                            if (GUILayout.Button("Delete"))
                                deleteLayer = true;
                            if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].prefab != null)
                            {
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform = GUILayout.Toggle(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform, "Transform", "Button");
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring = GUILayout.Toggle(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring, "Colors", "Button");
                            }
                            EditorGUILayout.EndHorizontal();

                            if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].prefab != null)
                            {
                                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform || spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring)
                                    EditorGUILayout.Space();

                                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform)
                                {
                                    spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._position = RandomVectorGUI("Offset", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._position, true);
                                    spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation = RandomVectorGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation, true);
                                    spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._scale = RandomVectorGUI("Scale", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._scale, true);
                                }

                                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform && spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring)
                                    EditorGUILayout.Space();

                                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring)
                                {
                                    if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors == null)
                                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors = new MaterialColoring[0];

                                    for (int c = 0; c < spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors.Length; c++)
                                    {
                                        bool c_remove = false;

                                        EditorGUILayout.BeginHorizontal();
                                        if (GUILayout.Button("X", GUILayout.Width(20)))
                                            c_remove = true;
                                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].material = (Material)EditorGUILayout.ObjectField(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].material, typeof(Material), false);

                                        string oldPropertyName = spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].propertyName;
                                        string propertyName = EditorGUILayout.TextField(oldPropertyName);
                                        if (propertyName == "")
                                            propertyName = MassSpawner.isSRP ? "_BaseColor" : "_Color";

                                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].propertyName = propertyName;

                                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup != -1 && !spawner.colorGroupExists(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup))
                                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup = -1;

                                        int selectedGroup = EditorGUILayout.Popup(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup, spawner.colorGroupLabels);
                                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup = spawner.colorGroups.Length > 0 ? selectedGroup : -1;

                                        EditorGUILayout.EndHorizontal();

                                        if (c_remove)
                                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].RemoveColoring(c);
                                    }

                                    if (GUILayout.Button("Add material"))
                                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].AddColoring();
                                }
                            }


                            GUILayout.EndVertical();

                            EditorGUILayout.Space(4);

                            if (deleteLayer)
                                spawner.objectLayers[spawner.selectedObjectLayerIndex].RemovePrefab(i);
                        }

                        if (GUILayout.Button("Add prefab"))
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].AddPrefab();
                    }
                    else
                    {
                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length == 0)
                            EditorGUILayout.HelpBox("You need to add terrain trees to this layer in order to use it", MessageType.Info);

                        if (GUILayout.Button("Add terrain tree"))
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].AddTreeObject();
                    }

                    GUILayout.EndVertical();

                    if (remove)
                    {
                        CloseAllLayers();
                        spawner.RemoveObjectLayer(spawner.selectedObjectLayerIndex);
                    }
                    if (move_up)
                    {
                        CloseAllLayers();
                        spawner.MoveObjectLayerUp(spawner.selectedObjectLayerIndex);
                    }
                    else if (move_down)
                    {
                        CloseAllLayers();
                        spawner.MoveObjectLayerDown(spawner.selectedObjectLayerIndex);
                    }
                    if (duplicate)
                    {
                        CloseAllLayers();
                        spawner.DuplicateObjectLayer(spawner.selectedObjectLayerIndex);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a layer from the left side to customize its settings", MessageType.Info);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Some update logic
            UpdateSelectedObject();
            UpdateSelectedColorGroup();

            if (heightmapUpdate)
            {
                if (heightmapAutoUpdate)
                    spawner.GenerateHeightmap();

                heightmapUpdate = false;
            }
            if (placeObjects)
            {
                if (spawner.terrainSplat == null)
                    spawner.GenerateHeightmap();

                spawner.GenerateObjectPoints();
                spawner.PlaceObjects();
                placeObjects = false;
            }
        }

        // GUI functions
        string[] EnumNames<T>()
        {
            // Enum names to string[]
            string[] result = Enum.GetNames(typeof(T));

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = result[i].Replace("_", "");
            }

            return result;
        }

        bool ButtonTitle(string title, bool source, string _default = "Button", int width = 100, int height = 24)
        {
            // Toggle button
            return GUILayout.Toggle(source, stringDefault(title, _default), "Button", GUILayout.Height(height), GUILayout.Width(width));
        }
        string maxLength(string source, int length = 10)
        {
            // Max length string
            return source.Length <= length ? source : source.Substring(0, Mathf.Max(1, length - 3)) + "...";
        }

        RandomVector3 RandomVectorGUI(RandomVector3 value, bool canOverride = false)
        {
            return RandomVectorGUI(string.Empty, value);
        }
        RandomVector3 RandomVectorGUI(RandomVector3 value, string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z")
        {
            return RandomVectorGUI(string.Empty, value, false, xAxisName, yAxisName, zAxisName);
        }
        RandomVector3 RandomVectorGUI(string name, RandomVector3 value, bool canOverride = false, string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z")
        {
            // Random vector settings (transform settings, coloring etc.)
            RandomVector3 result = value;

            bool hasTitle = name != string.Empty || canOverride;

            if (hasTitle)
                GUILayout.BeginHorizontal();

            if (name != string.Empty)
                GUILayout.Label(name);

            if (canOverride)
            {
                result._override = GUILayout.Toggle(result._override, result._override ? "Custom" : "Inherit", "Button", GUILayout.Width(100));
            }
            else
                result._override = false;

            if (hasTitle)
                GUILayout.EndHorizontal();

            if (!canOverride || result._override)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                // X
                GUILayout.BeginHorizontal();
                GUILayout.Label(xAxisName, GUILayout.Width(20));
                GUILayout.Space(10);
                result.xMode = (RandomVectorModeX)EditorGUILayout.EnumPopup(value.xMode, GUILayout.Width(80));
                GUILayout.BeginHorizontal();
                if (result.xMode != RandomVectorModeX.CopyY && result.xMode != RandomVectorModeX.CopyZ)
                {
                    result.x = RandomFloatInput(result.x, (int)result.xMode);
                    if (value.clamp)
                    {
                        result.x.min = Mathf.Clamp(result.x.min, result.min, result.max);
                        result.x.max = Mathf.Clamp(result.x.max, result.min, result.max);
                    }
                    if (value.whole)
                    {
                        result.x.min = Mathf.Floor(result.x.min);
                        result.x.max = Mathf.Floor(result.x.max);
                    }
                }
                else
                {
                    RandomFloatInput(result.xMode == RandomVectorModeX.CopyY ? result.y : result.z, result.xMode == RandomVectorModeX.CopyY ? (int)result.yMode : (int)result.zMode);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();

                // Y
                GUILayout.BeginHorizontal();
                GUILayout.Label(yAxisName, GUILayout.Width(20));
                GUILayout.Space(10);
                result.yMode = (RandomVectorModeY)EditorGUILayout.EnumPopup(value.yMode, GUILayout.Width(80));
                GUILayout.BeginHorizontal();
                if (result.yMode != RandomVectorModeY.CopyX && result.yMode != RandomVectorModeY.CopyZ)
                {
                    result.y = RandomFloatInput(result.y, (int)result.yMode);
                    if (value.clamp)
                    {
                        result.y.min = Mathf.Clamp(result.y.min, result.min, result.max);
                        result.y.max = Mathf.Clamp(result.y.max, result.min, result.max);
                    }
                    if (value.whole)
                    {
                        result.y.min = Mathf.Floor(result.y.min);
                        result.y.max = Mathf.Floor(result.y.max);
                    }
                }
                else
                {
                    RandomFloatInput(result.yMode == RandomVectorModeY.CopyX ? result.x : result.z, result.yMode == RandomVectorModeY.CopyX ? (int)result.xMode : (int)result.zMode);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();

                // Z
                GUILayout.BeginHorizontal();
                GUILayout.Label(zAxisName, GUILayout.Width(20));
                GUILayout.Space(10);
                result.zMode = (RandomVectorModeZ)EditorGUILayout.EnumPopup(value.zMode, GUILayout.Width(80));
                GUILayout.BeginHorizontal();
                if (result.zMode != RandomVectorModeZ.CopyX && result.zMode != RandomVectorModeZ.CopyY)
                {
                    result.z = RandomFloatInput(result.z, (int)result.zMode);
                    if (value.clamp)
                    {
                        result.z.min = Mathf.Clamp(result.z.min, result.min, result.max);
                        result.z.max = Mathf.Clamp(result.z.max, result.min, result.max);
                    }
                    if (value.whole)
                    {
                        result.z.min = Mathf.Floor(result.z.min);
                        result.z.max = Mathf.Floor(result.z.max);
                    }
                }
                else
                {
                    RandomFloatInput(result.zMode == RandomVectorModeZ.CopyX ? result.x : result.y, result.zMode == RandomVectorModeZ.CopyX ? (int)result.xMode : (int)result.yMode);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        
            return result;
        }

        RandomBetween RandomFloatInput(RandomBetween source, int mode = 0, bool disabled = false)
        {
            RandomBetween result = source;
            if (!disabled)
            {
                if (mode == 0)
                {
                    result.min = EditorGUILayout.FloatField(result.min);
                }
                else if (mode == 1)
                {
                    result.min = EditorGUILayout.FloatField(result.min);
                    result.max = EditorGUILayout.FloatField(result.max);
                }
            }
            else
            {
                if (mode == 0)
                {
                    EditorGUILayout.LabelField(result.min.ToString());
                }
                else if (mode == 1)
                {
                    EditorGUILayout.LabelField(result.min.ToString());
                    EditorGUILayout.LabelField(result.max.ToString());
                }
            }
            return result;
        }
        string stringDefault(string source, string _default)
        {
            // Return a string if the source if empty
            return source != string.Empty ? source : _default;
        }

        static List<int> layerNumbers = new List<int>();
        static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            // Custom layer mask inspector
            var layers = InternalEditorUtility.layers;

            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField(label, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;

            return layerMask;
        }

        // Object layers functions
        void CloseAllLayers()
        {
            CloseAllLayers(true);
        }
        bool CloseAllLayers(bool target)
        {
            if (target)
            {
                foreach (ObjectLayer objectLayer in spawner.objectLayers)
                    objectLayer.opened = false;
            }

            return target;
        }
        void CloseAllColorGroups()
        {
            CloseAllColorGroups(true);
        }
        bool CloseAllColorGroups(bool target)
        {
            if (target)
            {
                foreach (ColorGroup colorGroup in spawner.colorGroups)
                    colorGroup.opened = false;
            }

            return target;
        }
        void UpdateSelectedColorGroup()
        {
            spawner.selectedColorGroupIndex = -1;
            int index = 0;
            foreach(ColorGroup colorGroup in spawner.colorGroups)
            {
                if (colorGroup.opened)
                {
                    spawner.selectedColorGroupIndex = index;
                    return;
                }
                index++;
            }
        }
        void UpdateSelectedObject()
        {
            spawner.selectedObjectLayerIndex = -1;
            int index = 0;
            foreach (ObjectLayer objectLayer in spawner.objectLayers)
            {
                if (objectLayer.opened)
                {
                    spawner.selectedObjectLayerIndex = index;
                    return;
                }
                index++;
            }
        }

        // Heightmap values change check
        Resolutions CheckChange(Resolutions source, Resolutions updated)
        {
            if (source != updated)
                heightmapUpdate = true;

            return updated;
        }
        float CheckChange(float source, float updated)
        {
            if (source != updated)
                heightmapUpdate = true;

            return updated;
        }
        Vector2 CheckChange(Vector2 source, Vector2 updated)
        {
            if (source != updated)
                heightmapUpdate = true;

            return updated;
        }
        LayerMask CheckChange(LayerMask source, LayerMask updated)
        {
            if (source != updated)
                heightmapUpdate = true;

            return updated;
        }
    }
}