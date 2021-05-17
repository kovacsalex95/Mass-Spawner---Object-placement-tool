
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

/*

    TODO:

    Features:
    -   Terrain normal as rotation (with ratio)

*/

namespace lxkvcs
{
    [CustomEditor(typeof(MassSpawner))]
    public class MassSpawnerEditor : Editor
    {
        MassSpawner spawner;

        bool placeObjects = false;

        const int TAB_LAYERS = 1;
        const int TAB_COLORS = 2;
        const int TAB_WORLD_HEIGHTMAP = 0;
        int selectedTab = -1;

        int iconsize = 40;
        int columnWidth = 100;

        string assetsFolder = "";
        string[] names = null;
        Texture2D[] icons = null;
        Texture2D objectIcon = null;
        Texture2D upIcon = null;
        Texture2D downIcon = null;

        float inspectorSize;
        Rect previewRect;


        private void Awake()
        {
            spawner = target as MassSpawner;
        }


        public override void OnInspectorGUI()
        {
            // variables
            CheckVariables();


            // Middle toolbar (below the preview)
            TabModeSelector();


            EditorGUILayout.Space(10);


            // World & Heightmap settings
            TabWorldAndHeightmap();

            // Object layers
            TabObjectLayers();

            // Color groups
            TabColorGroups();


            // Update logic
            UpdateSelectedObject();
            UpdateSelectedColorGroup();


            // Do the placement
            if (placeObjects)
            {
                if (spawner.terrainHeight == null)
                    spawner.GenerateHeightmap();

                spawner.GenerateObjectPoints();
                spawner.PlaceObjects();
                placeObjects = false;
            }
        }



        void CheckVariables()
        {
            if (assetsFolder == "")
                assetsFolder = Util.AssetFolder;

            if (names == null)
                names = Util.EnumItemNames<PreviewMode>();

            if (icons == null)
            {
                icons = new Texture2D[]
                {
                    (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_heightmap.png", typeof(Texture2D)),
                    (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_slopemap.png", typeof(Texture2D)),
                    (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_3d.png", typeof(Texture2D))
                };
            }

            if (objectIcon == null)
                objectIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_object.png", typeof(Texture2D));

            if (upIcon == null)
                upIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_up.png", typeof(Texture2D));

            if (downIcon == null)
                downIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(assetsFolder + "/Resources/Icons/icon_down.png", typeof(Texture2D));


            inspectorSize = EditorGUIUtility.currentViewWidth;
            float previewSize = inspectorSize - 120;
            float scaledSize = Mathf.Min(previewSize, 400);
            float hOffset = (previewSize - scaledSize) / 2f;
            previewRect = new Rect(70 + hOffset, 80, scaledSize, scaledSize);

            inspectorSize = scaledSize;


            if (selectedTab == -1)
                selectedTab = spawner.terrainHeight != null ? TAB_LAYERS : TAB_WORLD_HEIGHTMAP;


            if (spawner.objectLayers == null)
                spawner.objectLayers = new ObjectLayer[0];
            if (spawner.colorGroups == null)
                spawner.colorGroups = new ColorGroup[0];


            if (spawner.selectedObjectLayerIndex != -1 && spawner.objectLayers[spawner.selectedObjectLayerIndex].objectPlaces == null)
                spawner.GenerateObjectPoints(spawner.objectLayers[spawner.selectedObjectLayerIndex]);
        }



        void ObjectButtons()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(140);
            if (spawner.objectLayers.Length > 0 && spawner.terrainHeight != null)
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
            if ((spawner.objectLayers.Length == 0 || spawner.terrainHeight == null) && spawner.transform.childCount == 0)
                EditorGUILayout.Space(32);
            GUILayout.EndHorizontal();
        }

        void PreviewModeSelection()
        {
            string[] labels = new string[]
            {
                "Terrain height",
                "Steepness and slopes",
                "Stylized 3D"
            };

            GUI.Label(new Rect(20, 40, 120, 20), labels[(int)spawner.preview]);
            spawner.preview = (PreviewMode)GUILayout.SelectionGrid((int)spawner.preview, icons, 1, GUILayout.Width(iconsize));

            if (spawner.selectedObjectLayerIndex != -1)
                spawner.showPlacement = GUILayout.Toggle(spawner.showPlacement, objectIcon, "Button", GUILayout.Height(iconsize), GUILayout.Width(iconsize));
            else
                EditorGUILayout.Space(iconsize + 4);
        }

        void PreviewRender()
        {
            Texture2D previewTexture = spawner.terrainHeight;
            if (spawner.preview == PreviewMode.Slopemap)
                previewTexture = spawner.terrainSlope;
            else if (spawner.preview == PreviewMode._3D)
                previewTexture = spawner.terrain3D;

            Rect clipRect = new Rect(80, 80, EditorGUIUtility.currentViewWidth - 80 - 50, previewRect.height);
            GUI.BeginClip(clipRect);

            float scaledWidth = previewRect.width * spawner.previewScale;
            float scaledHeight = previewRect.height * spawner.previewScale;
            float offsetX = (scaledWidth - previewRect.width) * (spawner.previewOffsetX);
            float offsetY = (scaledHeight - previewRect.height) * (spawner.previewOffsetY);

            Rect scaledRect = new Rect(previewRect.x - clipRect.x - offsetX, previewRect.y - clipRect.y - offsetY, scaledWidth, scaledHeight);

            if (previewTexture != null)
                GUI.DrawTexture(scaledRect, spawner.GenerateHeightmapPreviewTexture(previewTexture), ScaleMode.ScaleToFit);

            if (spawner.terrainHeight != null && spawner.terrainSlope != null && spawner.selectedObjectLayerIndex != -1)
                GUI.DrawTexture(scaledRect, spawner.GenerateLayerPreviewTexture(), ScaleMode.ScaleToFit);

            GUI.EndClip();

            spawner.previewScale = GUI.VerticalSlider(new Rect(40, previewRect.y + (names.Length + 1) * iconsize, 30, inspectorSize - (names.Length + 1) * iconsize), spawner.previewScale, 1f, 3f);

            EditorGUI.BeginDisabledGroup(spawner.previewScale == 1);

            spawner.previewOffsetX = GUI.HorizontalSlider(new Rect(previewRect.x, previewRect.y + previewRect.height + 10, previewRect.width, 20), spawner.previewOffsetX, 0f, 1f);
            spawner.previewOffsetY = GUI.VerticalSlider(new Rect(EditorGUIUtility.currentViewWidth - 35, previewRect.y, 30, previewRect.height), spawner.previewOffsetY, 0f, 1f);

            EditorGUI.EndDisabledGroup();

            if (GUI.Button(new Rect(EditorGUIUtility.currentViewWidth - 50, previewRect.y + previewRect.height, 30, 30), "○"))
            {
                spawner.previewScale = 1f;
                spawner.previewOffsetX = 0.5f;
                spawner.previewOffsetY = 0.5f;
            }


            EditorGUILayout.Space(inspectorSize - (names.Length + 1) * iconsize + 30);
        }

        void TabModeSelector()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, new string[] {
                "World & Heightmap",
                string.Format("Layers({0}) & Objects", spawner.objectLayers.Length),
                string.Format("Color groups ({0})", spawner.colorGroups.Length)
            });
        }

        void TabWorldAndHeightmap()
        {
            if (selectedTab != TAB_WORLD_HEIGHTMAP)
                return;

            spawner.terrainTop = EditorGUILayout.FloatField("Top", spawner.terrainTop);
            spawner.terrainBottom = EditorGUILayout.FloatField("Bottom", spawner.terrainBottom);
            spawner.terrainOffset = EditorGUILayout.Vector2Field("Offset", spawner.terrainOffset);
            spawner.terrainSize = EditorGUILayout.Vector2Field("Size", spawner.terrainSize);


            GUILayout.Space(20);

            spawner.heightmapResolution = (Resolutions)EditorGUILayout.EnumPopup("Heightmap resolution", spawner.heightmapResolution);

            spawner.includeMask = EditorUI.LayerMaskField("Terrain layers", spawner.includeMask);
            spawner.excludeMask = EditorUI.LayerMaskField("Exclude layers", spawner.excludeMask);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate heightmap"))
                spawner.GenerateHeightmap();

            EditorGUILayout.EndHorizontal();
        }

        void TabObjectLayers()
        {
            if (selectedTab != TAB_LAYERS)
                return;

            // Update & Delete objects buttons
            ObjectButtons();

            EditorGUILayout.Space(2);


            // Preview mode selection
            PreviewModeSelection();

            EditorGUILayout.Space(10);


            // Preview drawing
            PreviewRender();

            EditorGUILayout.Space(10);


            EditorGUILayout.BeginHorizontal();

            // Layer selector
            EditorGUILayout.BeginVertical();
            int index = 0;
            foreach (ObjectLayer objectLayer in spawner.objectLayers)
            {
                objectLayer.opened = CloseAllLayers(EditorUI.ToggleButton(EditorUI.StringMaxLength(objectLayer.name, 15), objectLayer.opened, string.Format("Object layer #{0}", index), columnWidth));
                index++;
            }
            GUILayout.Space(10);
            if (GUILayout.Button("[+] Add layer", GUILayout.Width(columnWidth)))
            {
                CloseAllLayers();
                AddObjectLayer();
            }

            EditorGUILayout.EndVertical();

            // Layer settings
            TabObjectLayersSettings();

            EditorGUILayout.EndHorizontal();
        }

        void TabObjectLayersSettings()
        {
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
                float organicity = spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity;
                spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN = 30 - EditorGUILayout.IntSlider("Placement density", 30 - spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN, 0, 27);
                spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset = EditorGUILayout.Slider("Placement chatotics", spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset, 0, 1);
                spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity = EditorGUILayout.Slider("Placement organicity", spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity, 0, 1);

                if (everyN != spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN ||
                    angleOffset != spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset ||
                    organicity != spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity)
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

                        spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask = EditorUI.LayerMaskField("Detection layers", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask);

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
                    spawner.objectLayers[spawner.selectedObjectLayerIndex]._position = RandomVector3.InputGUI("Offset", spawner.objectLayers[spawner.selectedObjectLayerIndex]._position);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation = RandomVector3.InputGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation);
                    spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale = RandomVector3.InputGUI("Scale", spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space(14);

                spawner.objectLayers[spawner.selectedObjectLayerIndex].placing = (PlaceType)EditorGUILayout.EnumPopup("Placement method", spawner.objectLayers[spawner.selectedObjectLayerIndex].placing);

                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].placing == PlaceType.Prefabs)
                {
                    for (int i = 0; i < spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length; i++)
                    {
                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring)
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring = false;
                    }

                    EditorGUILayout.HelpBox("When you use Prefab placement mode you cannot use coloring options.\nThe objects going to have Prefab connection.", MessageType.Warning);
                }


                EditorGUILayout.Space(4);

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
                        
                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].placing == PlaceType.Instantiate)
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring = GUILayout.Toggle(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring, "Colors", "Button");
                    }
                    EditorGUILayout.EndHorizontal();

                    if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].prefab != null)
                    {
                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform || spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].coloring)
                            EditorGUILayout.Space();

                        if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].transform)
                        {
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._position = RandomVector3.InputGUI("Offset", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._position, true);
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation = RandomVector3.InputGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation, true);
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._scale = RandomVector3.InputGUI("Scale", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._scale, true);
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
                                    propertyName = Util.ProjectIsSRP ? "_BaseColor" : "_Color";

                                spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].propertyName = propertyName;

                                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup != -1 && !ColorGroupExists(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup))
                                    spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup = -1;

                                int selectedGroup = EditorGUILayout.Popup(spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].colors[c].colorGroup, colorGroupLabels);
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

                GUILayout.EndVertical();

                if (remove)
                {
                    CloseAllLayers();
                    RemoveObjectLayer(spawner.selectedObjectLayerIndex);
                }
                if (move_up)
                {
                    CloseAllLayers();
                    MoveObjectLayerUp(spawner.selectedObjectLayerIndex);
                }
                else if (move_down)
                {
                    CloseAllLayers();
                    MoveObjectLayerDown(spawner.selectedObjectLayerIndex);
                }
                if (duplicate)
                {
                    CloseAllLayers();
                    DuplicateObjectLayer(spawner.selectedObjectLayerIndex);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a layer from the left side to customize its settings", MessageType.Info);
            }
        }

        void TabColorGroups()
        {
            if (selectedTab != TAB_COLORS)
                return;

            EditorGUILayout.BeginHorizontal();

            // Color group selector
            EditorGUILayout.BeginVertical();
            int index = 0;
            foreach (ColorGroup colorGroup in spawner.colorGroups)
            {
                colorGroup.opened = CloseAllColorGroups(EditorUI.ToggleButton(EditorUI.StringMaxLength(colorGroup.name, 15), colorGroup.opened, string.Format("Color group #{0}", index), columnWidth));
                index++;
            }
            GUILayout.Space(10);
            if (GUILayout.Button("[+] Add group", GUILayout.Width(columnWidth)))
            {
                CloseAllColorGroups();
                AddColorGroup();
            }
            EditorGUILayout.EndVertical();


            // Color group settings
            TabColorGroupsSettings();

            EditorGUILayout.EndHorizontal();
        }

        void TabColorGroupsSettings()
        {
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
                    spawner.colorGroups[spawner.selectedColorGroupIndex].rgb = RandomVector3.InputGUI(spawner.colorGroups[spawner.selectedColorGroupIndex].rgb, "R", "G", "B");
                else if (spawner.colorGroups[spawner.selectedColorGroupIndex].mode == ColorGroupMode.HSV)
                    spawner.colorGroups[spawner.selectedColorGroupIndex].hsv = RandomVector3.InputGUI(spawner.colorGroups[spawner.selectedColorGroupIndex].hsv, "H", "S", "V");
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
                    RemoveColorGroup(spawner.selectedColorGroupIndex);
                }
                if (duplicate)
                {
                    CloseAllColorGroups();
                    DuplicateColorGroup(spawner.selectedColorGroupIndex);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select a color group from the left side to customize its settings", MessageType.Info);
            }
        }



        // [UI] COLOR GROUPS
        public void AddColorGroup()
        {
            ColorGroup group = new ColorGroup();
            AddColorGroup(group);
        }

        public void AddColorGroup(ColorGroup group)
        {
            ColorGroup[] oldGroups = spawner.colorGroups;
            spawner.colorGroups = new ColorGroup[oldGroups.Length + 1];
            for (int i = 0; i < oldGroups.Length; i++)
            {
                spawner.colorGroups[i] = oldGroups[i];
            }
            spawner.colorGroups[oldGroups.Length] = group;
            spawner.selectedColorGroupIndex = spawner.colorGroups.Length - 1;
            spawner.colorGroups[spawner.selectedColorGroupIndex].opened = true;
        }

        public void RemoveColorGroup(int index)
        {
            List<ColorGroup> newList = new List<ColorGroup>();
            for (int i = 0; i < spawner.colorGroups.Length; i++)
            {
                if (i != index)
                    newList.Add(spawner.colorGroups[i]);
            }
            spawner.colorGroups = newList.ToArray();
            if (spawner.colorGroups.Length > 0)
            {
                spawner.selectedColorGroupIndex = spawner.colorGroups.Length - 1;
                spawner.colorGroups[spawner.selectedColorGroupIndex].opened = true;
            }
            else
                spawner.selectedColorGroupIndex = -1;
        }

        public void DuplicateColorGroup(int index)
        {
            if (spawner.selectedColorGroupIndex != -1)
                spawner.colorGroups[spawner.selectedColorGroupIndex].opened = false;

            ColorGroup newGroup = new ColorGroup(spawner.colorGroups[index]);
            newGroup.name += " (clone)";
            newGroup.opened = true;
            AddColorGroup(newGroup);

            spawner.selectedColorGroupIndex = spawner.colorGroups.Length - 1;
        }

        public string[] colorGroupLabels
        {
            get
            {
                List<string> labels = new List<string>();
                if (spawner.colorGroups != null && spawner.colorGroups.Length > 0)
                {
                    foreach (ColorGroup g in spawner.colorGroups)
                        labels.Add(g.name);
                }
                else
                {
                    labels.Add("-");
                }
                return labels.ToArray();
            }
        }

        public bool ColorGroupExists(int index)
        {
            return spawner.colorGroups == null || spawner.colorGroups.Length == 0 ? false : (index >= 0 && index < spawner.colorGroups.Length);
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
            foreach (ColorGroup colorGroup in spawner.colorGroups)
            {
                if (colorGroup.opened)
                {
                    spawner.selectedColorGroupIndex = index;
                    return;
                }
                index++;
            }
        }



        // [UI] OBJECT LAYERS
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
            ObjectLayer[] oldLayers = spawner.objectLayers;
            spawner.objectLayers = new ObjectLayer[oldLayers.Length + 1];

            for (int i = 0; i < oldLayers.Length; i++)
            {
                spawner.objectLayers[i] = oldLayers[i];
            }
            spawner.objectLayers[oldLayers.Length] = layer;
            spawner.selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
            spawner.objectLayers[spawner.selectedObjectLayerIndex].opened = true;
        }

        public void RemoveObjectLayer(int index)
        {
            List<ObjectLayer> newList = new List<ObjectLayer>();
            for (int i = 0; i < spawner.objectLayers.Length; i++)
            {
                if (i != index)
                    newList.Add(spawner.objectLayers[i]);
            }
            spawner.objectLayers = newList.ToArray();
            if (spawner.objectLayers.Length > 0)
            {
                spawner.selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
                spawner.objectLayers[spawner.selectedObjectLayerIndex].opened = true;
            }
            else
                spawner.selectedObjectLayerIndex = -1;
        }

        public void MoveObjectLayerUp(int index)
        {
            int toIndex = Mathf.Max(0, index - 1);
            MoveObjectLayer(index, toIndex);
        }

        public void MoveObjectLayerDown(int index)
        {
            int toIndex = Mathf.Min(spawner.objectLayers.Length - 1, index + 1);
            MoveObjectLayer(index, toIndex);
        }

        void MoveObjectLayer(int fromIndex, int toIndex)
        {
            if (fromIndex != toIndex)
            {
                ObjectLayer fromLayer = spawner.objectLayers[fromIndex];
                ObjectLayer toLayer = spawner.objectLayers[toIndex];

                ObjectLayer[] oldLayers = spawner.objectLayers;
                spawner.objectLayers = new ObjectLayer[oldLayers.Length];
                for (int i = 0; i < oldLayers.Length; i++)
                {
                    if (i == fromIndex || i == toIndex)
                    {
                        if (i == fromIndex)
                        {
                            spawner.objectLayers[i] = toLayer;
                        }
                        else
                        {
                            spawner.objectLayers[i] = fromLayer;
                        }
                    }
                    else
                    {
                        spawner.objectLayers[i] = oldLayers[i];
                    }
                }

                if (spawner.selectedObjectLayerIndex == fromIndex)
                {
                    spawner.selectedObjectLayerIndex = toIndex;
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].opened = true;
                }
            }
        }

        public void DuplicateObjectLayer(int index)
        {
            if (spawner.selectedObjectLayerIndex != -1)
                spawner.objectLayers[spawner.selectedObjectLayerIndex].opened = false;

            ObjectLayer newLayer = new ObjectLayer(spawner.objectLayers[index]);
            newLayer.name += " (clone)";
            newLayer.opened = true;
            AddObjectLayer(newLayer);

            spawner.selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
        }

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
    }
}