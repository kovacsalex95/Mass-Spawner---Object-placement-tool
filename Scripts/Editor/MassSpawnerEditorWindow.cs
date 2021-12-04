
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*

    TODO:

    Features:
    -   Terrain normal as rotation (with ratio)

*/

namespace lxkvcs
{
    public class MassSpawnerEditorWindow : EditorWindow
    {
        MassSpawner spawner = null;

        bool placeObjects = false;

        int iconsize = 50;

        string assetsFolder = "";
        string[] names = null;
        Texture2D[] icons = null;
        Texture2D objectIcon = null;

        static MassSpawner target
        {
            get
            {
                MassSpawner spawner = GameObject.FindObjectOfType<MassSpawner>();
                if (spawner == null)
                {
                    GameObject newObject = new GameObject("Mass Spawner");
                    newObject.transform.position = Vector3.zero;

                    return newObject.AddComponent<MassSpawner>();
                }
                return spawner.GetComponent <MassSpawner>();
            }
        }

        Rect previewArea;
        Rect settingsArea;
        Rect settingsLayersArea;
        Rect settingsSettingsArea;
        Vector2 layersScrollPos;
        Vector2 layerSettingsScrollPos = Vector2.zero;


        [MenuItem("Tools/Mass Spawner")]
        public static void Init()
        {
            MassSpawnerEditorWindow window = (MassSpawnerEditorWindow)EditorWindow.GetWindow(typeof(MassSpawnerEditorWindow));
            window.titleContent.text = "Mass Spawner";
            window.Show();
        }

        public void OnGUI()
        {
            if (position.width < 350 || position.height < 350)
            {
                GUILayout.Label("Window is too small to operate!", GUILayout.Height(100));
                return;
            }

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
            if (spawner == null)
                spawner = target;

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


            if (spawner.selectedTab == -1)
                spawner.selectedTab = spawner.terrainHeight != null ? MassSpawner.TAB_LAYERS : MassSpawner.TAB_WORLD_HEIGHTMAP;

            if (spawner.selectedSettingsTab == -1)
                spawner.selectedSettingsTab = MassSpawner.STAB_PLACEMENT;


            if (spawner.objectLayers == null)
                spawner.objectLayers = new ObjectLayer[0];
            if (spawner.colorGroups == null)
                spawner.colorGroups = new ColorGroup[0];


            if ((spawner.terrainHeight == null || spawner.terrainSlope == null || spawner.terrain3D == null) && !spawner.mapsChecked)
                spawner.TryToLoadSavedMaps();


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

            GUI.Label(new Rect(20, 5, 130, 20), labels[(int)spawner.preview]);
            spawner.preview = (PreviewMode)GUILayout.SelectionGrid((int)spawner.preview, icons, 1, GUILayout.Width(iconsize));

            GUILayout.Space(5);

            if (spawner.selectedObjectLayerIndex != -1)
            {
                bool oldShow = spawner.showPlacement;
                spawner.showPlacement = GUILayout.Toggle(spawner.showPlacement, objectIcon, "Button", GUILayout.Height(iconsize), GUILayout.Width(iconsize));

                if (oldShow != spawner.showPlacement)
                    SceneView.RepaintAll();
            }
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

            Rect backRect = new Rect(60, 35, previewArea.width - 60, previewArea.height - 35);
            Rect clipRect = new Rect(backRect.x + 20, backRect.y, backRect.width - 40, backRect.height - 20);

            EditorGUI.DrawRect(clipRect, new Color(0, 0, 0, 0.25f));

            float previewSize = Mathf.Min(clipRect.width, clipRect.height) * spawner.previewScale;

            float xOffset = (clipRect.width - previewSize) * spawner.previewOffsetX;
            float yOffset = (clipRect.height - previewSize) * spawner.previewOffsetY;

            Rect previewRect = new Rect(clipRect.x + xOffset, clipRect.y + yOffset, previewSize, previewSize);



            // Zoom scale
            float oldScale = spawner.previewScale;
            spawner.previewScale = GUI.VerticalSlider(new Rect(backRect.x + 5, backRect.y + 10, 15, backRect.height - 20), spawner.previewScale, 1f, 3f);
            if (spawner.previewScale != oldScale && spawner.previewScale == 1f)
                spawner.previewOffsetX = spawner.previewOffsetY = 0.5f;

            Rect actualPreviewRect = new Rect(previewRect.x - clipRect.x, previewRect.y - clipRect.y, previewRect.width, previewRect.height);

            // Offsets
            if (actualPreviewRect.width > clipRect.width)
                spawner.previewOffsetX = GUI.HorizontalScrollbar(new Rect(backRect.x + 20, backRect.y + backRect.height - 20, backRect.width - 40, 20), spawner.previewOffsetX, 0, 0, 1);
            else
                spawner.previewOffsetX = 0.5f;

            if (actualPreviewRect.height > clipRect.height)
                spawner.previewOffsetY = GUI.VerticalScrollbar(new Rect(backRect.x + backRect.width - 20, backRect.y + 20, 20, backRect.height - 40), spawner.previewOffsetY, 0, 0, 1);
            else
                spawner.previewOffsetY = 0.5f;



            // DRAWING

            GUI.BeginClip(new Rect(clipRect.x, clipRect.y, clipRect.width, clipRect.height));

            if (previewTexture != null)
                GUI.DrawTexture(actualPreviewRect, spawner.GenerateHeightmapPreviewTexture(previewTexture), ScaleMode.ScaleToFit);

            if (spawner.terrainHeight != null && spawner.terrainSlope != null && spawner.selectedObjectLayerIndex != -1)
                GUI.DrawTexture(actualPreviewRect, spawner.GenerateLayerPreviewTexture(), ScaleMode.ScaleToFit);

            GUI.EndClip();

            /*

            Matrix4x4 matrixBackup = GUI.matrix;

            Rect labelRect = new Rect(10, previewRect.y + (names.Length + 1) * iconsize + 10, 40, 20);
            GUIUtility.RotateAroundPivot(90, new Vector2(labelRect.x + labelRect.width / 2, labelRect.y + labelRect.height / 2));
            GUI.Label(labelRect, "Zoom");
            GUI.matrix = matrixBackup;
            
            if (GUI.Button(new Rect(EditorGUIUtility.currentViewWidth - 50, previewRect.y + previewRect.height, 30, 30), "1:1"))
            {
                spawner.previewScale = 1f;
                spawner.previewOffsetX = 0.5f;
                spawner.previewOffsetY = 0.5f;
            }

            */
        }


        void TabModeSelector()
        {
            int oldSelected = spawner.selectedTab;
            spawner.selectedTab = GUILayout.Toolbar(spawner.selectedTab, new string[] {
                "World & Heightmap",
                string.Format("Layers({0}) & Objects", spawner.objectLayers.Length),
                string.Format("Color groups ({0})", spawner.colorGroups.Length)
            }, GUILayout.Height(30));

            if (oldSelected != spawner.selectedTab)
                SceneView.RepaintAll();
        }


        void TabWorldAndHeightmap()
        {
            if (spawner.selectedTab != MassSpawner.TAB_WORLD_HEIGHTMAP)
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
            {
                spawner.GenerateHeightmap();
                spawner.selectedTab = MassSpawner.TAB_LAYERS;
            }

            EditorGUILayout.EndHorizontal();
        }


        void TabObjectLayers()
        {
            if (spawner.selectedTab != MassSpawner.TAB_LAYERS)
                return;

            bool isHorizontal = position.width > position.height;

            Rect usefulSpace = new Rect(0, 35, position.width, position.height - 35);

            float previewHeight = Mathf.Clamp(usefulSpace.height / 2, 300, position.width);

            previewArea = new Rect(usefulSpace.x, usefulSpace.y, usefulSpace.width, previewHeight);
            settingsArea = new Rect(usefulSpace.x + 10, usefulSpace.y + previewHeight, usefulSpace.width - 20, usefulSpace.height - previewHeight - 10);

            if (isHorizontal)
            {
                float previewWidth = Mathf.Clamp(usefulSpace.width / 2f, 300, position.height);

                previewArea = new Rect(usefulSpace.x, usefulSpace.y, previewWidth, usefulSpace.height);
                settingsArea = new Rect(usefulSpace.x + previewWidth + 10, usefulSpace.y, usefulSpace.width - previewWidth - 20, usefulSpace.height - 10);
            }

            GUILayout.BeginArea(previewArea);

            // Update & Delete objects buttons
            ObjectButtons();

            EditorGUILayout.Space(2);


            // Preview mode selection
            PreviewModeSelection();

            EditorGUILayout.Space(10);


            // Preview drawing
            PreviewRender();

            EditorGUILayout.Space(10);


            GUILayout.EndArea();


            int toolbarWidth = 20 * 4;

            settingsLayersArea = new Rect(settingsArea.x, settingsArea.y, toolbarWidth + 100, settingsArea.height);
            settingsSettingsArea = new Rect(settingsArea.x + settingsLayersArea.width + 10, settingsArea.y, settingsArea.width - settingsLayersArea.width - 10, settingsArea.height);

            bool isTabsTop = settingsSettingsArea.width < settingsArea.width *0.6f;

            if (isTabsTop)
            {
                int layersHeight = Mathf.Min(spawner.objectLayers.Length * 28 + 40, 4 * 28 + 40);

                settingsLayersArea = new Rect(settingsArea.x, settingsArea.y, settingsArea.width, layersHeight);
                settingsSettingsArea = new Rect(settingsArea.x, settingsArea.y + settingsLayersArea.height + 10, settingsArea.width, settingsArea.height - settingsLayersArea.height - 10);
            }


            GUILayout.BeginArea(settingsLayersArea);

            GUILayout.Space(40);
            EditorGUI.DrawRect(new Rect(0, 0, settingsLayersArea.width, 30), new Color(0, 0, 0, 0.2f));
            GUI.Label(new Rect(5, 0, settingsLayersArea.width - 5, 30), "Layers");

            // Layer selector
            int oldSelectedLayer = spawner.selectedObjectLayerIndex;

            EditorGUILayout.BeginVertical();

            if (GUI.Button(new Rect(settingsLayersArea.width - 60, 0, 60, 30), "[+] New"))
            {
                CloseAllLayers();
                AddObjectLayer();
            }

            layersScrollPos = EditorGUILayout.BeginScrollView(layersScrollPos);

            int index = 0;
            foreach (ObjectLayer objectLayer in spawner.objectLayers)
            {
                GUILayout.BeginHorizontal();

                objectLayer.opened = CloseAllLayers(EditorUI.ToggleButton(EditorUI.StringMaxLength(objectLayer.name, 15), objectLayer.opened, string.Format("Object layer #{0}", index)));//, -(int)settingsLayersArea.width + toolbarWidth + 4));

                int operation = GUILayout.Toolbar(-1, new string[] { "↑", "↓", "+", "×" }, GUILayout.Height(24), GUILayout.Width(toolbarWidth - 8));


                GUILayout.EndHorizontal();


                if (operation >= 0 && operation <= 3)
                    CloseAllLayers();

                switch (operation)
                {
                    case 0: MoveObjectLayerUp(index); break;
                    case 1: MoveObjectLayerDown(index); break;
                    case 2: DuplicateObjectLayer(index); break;
                    case 3: RemoveObjectLayer(index); break;
                }

                if (operation >= 0 && operation <= 3)
                    break;

                index++;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();


            GUILayout.BeginArea(settingsSettingsArea);

            GUILayout.Space(40);
            EditorGUI.DrawRect(new Rect(0, 0, settingsSettingsArea.width, 30), new Color(0, 0, 0, 0.2f));
            GUI.Label(new Rect(5, 0, settingsSettingsArea.width - 5, 30), "Layer settings");


            if (isTabsTop)
                GUILayout.Space(10);



            // Layer settings
            TabObjectLayersSettings();



            GUILayout.EndArea();



            UpdateSelectedObject();

            if (oldSelectedLayer != spawner.selectedObjectLayerIndex)
                SceneView.RepaintAll();
        }

        void TabObjectLayersSettings()
        {
            if (spawner.selectedObjectLayerIndex != -1)
            {
                GUILayout.BeginVertical();

                // Name
                spawner.objectLayers[spawner.selectedObjectLayerIndex].name = EditorGUILayout.TextField("Name", spawner.objectLayers[spawner.selectedObjectLayerIndex].name);

                EditorGUILayout.Space(10);



                spawner.selectedSettingsTab = GUILayout.Toolbar(spawner.selectedSettingsTab, new string[] { "Terrain", "Transform", "Rules", "Prefabs" }, GUILayout.Height(24));

                EditorGUILayout.Space();



                layerSettingsScrollPos = GUILayout.BeginScrollView(layerSettingsScrollPos);

                GUILayout.BeginVertical(EditorStyles.helpBox);



                spawner.objectLayers[spawner.selectedObjectLayerIndex].CheckArrays();


                TabObjectLayerSettingPlacement();


                TabObjectLayerSettingCollision();


                TabObjectLayerSettingTransform();


                TabObjectLayerSettingPrefabs();



                GUILayout.EndVertical();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a layer from the left side to customize its settings", MessageType.Info);
            }
        }

        void TabObjectLayerSettingPlacement()
        {
            if (spawner.selectedSettingsTab != MassSpawner.STAB_PLACEMENT)
                return;

            bool smallAF = settingsSettingsArea.width < 500;

            // Terrain height
            if (smallAF)
                GUILayout.Label("Terrain height");
            GUILayout.BeginHorizontal();
            if (!smallAF)
                GUILayout.Label("Terrain height");
            GUILayout.Space(10);
            spawner.objectLayers[spawner.selectedObjectLayerIndex].from = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].from, GUILayout.MaxWidth(40)));
            EditorGUILayout.MinMaxSlider(ref spawner.objectLayers[spawner.selectedObjectLayerIndex].from, ref spawner.objectLayers[spawner.selectedObjectLayerIndex].to, 0, 1);
            spawner.objectLayers[spawner.selectedObjectLayerIndex].to = Mathf.Clamp01(EditorGUILayout.FloatField(spawner.objectLayers[spawner.selectedObjectLayerIndex].to, GUILayout.MaxWidth(40)));
            GUILayout.EndHorizontal();

            // Terrain slope
            if (smallAF)
                GUILayout.Label("Terrain slope");
            GUILayout.BeginHorizontal();
            if (!smallAF)
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
            spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN = 30 - EditorGUILayout.IntSlider("Density", 30 - spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN, 0, 27);
            spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset = EditorGUILayout.Slider("Chaotics", spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset, 0, 1);
            spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity = EditorGUILayout.Slider("Organicity", spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity, 0, 1);

            if (everyN != spawner.objectLayers[spawner.selectedObjectLayerIndex].everyN ||
                angleOffset != spawner.objectLayers[spawner.selectedObjectLayerIndex].angleOffset ||
                organicity != spawner.objectLayers[spawner.selectedObjectLayerIndex].organicity)
                spawner.GenerateObjectPoints(spawner.objectLayers[spawner.selectedObjectLayerIndex]);
        }
        void TabObjectLayerSettingTransform()
        {
            if (spawner.selectedSettingsTab != MassSpawner.STAB_TRANSFORM)
                return;

            spawner.objectLayers[spawner.selectedObjectLayerIndex]._position = RandomVector3.InputGUI("Offset", spawner.objectLayers[spawner.selectedObjectLayerIndex]._position);
            EditorGUILayout.Space();
            spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation = RandomVector3.InputGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex]._rotation);
            EditorGUILayout.Space();
            spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale = RandomVector3.InputGUI("Scale", spawner.objectLayers[spawner.selectedObjectLayerIndex]._scale);
        }
        void TabObjectLayerSettingCollision()
        {
            if (spawner.selectedSettingsTab != MassSpawner.STAB_COLLISION)
                return;


            if (GUILayout.Button("[+] Add new rule"))
                spawner.objectLayers[spawner.selectedObjectLayerIndex].AddRule();

            bool disabledRule = false;

            for (int i = 0; i < spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules.Length; i++)
            {
                EditorGUI.BeginDisabledGroup(disabledRule);

                EditorGUILayout.Space(12);

                GUILayout.BeginVertical(EditorStyles.textArea);

                spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].mode = (CollisionMode)EditorGUILayout.EnumPopup("Behaviour", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].mode);

                spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask = EditorUI.LayerMaskField("Detection layers", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].layerMask);

                float newRadius = EditorGUILayout.FloatField("Detection radius", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].radius);
                if (newRadius > 0)
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].radius = newRadius;

                if (i < spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules.Length - 1)
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].stop = EditorGUILayout.Toggle("Ignore rules below if detected", spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].stop);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("↑", GUILayout.Width(24)))
                {
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].MoveRuleUp(i);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    EditorGUI.EndDisabledGroup();
                    break;
                }
                if (GUILayout.Button("↓", GUILayout.Width(24)))
                {
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].MoveRuleDown(i);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    EditorGUI.EndDisabledGroup();
                    break;
                }

                if (GUILayout.Button("Delete rule"))
                {
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].RemoveRule(i);
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    EditorGUI.EndDisabledGroup();
                    break;
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                EditorGUI.EndDisabledGroup();

                if (spawner.objectLayers[spawner.selectedObjectLayerIndex].collisionRules[i].stop)
                    disabledRule = true;
            }
        }
        void TabObjectLayerSettingPrefabs()
        {
            if (spawner.selectedSettingsTab != MassSpawner.STAB_PREFABS)
                return;

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


            EditorGUILayout.Space(10);

            if (spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length == 0)
                EditorGUILayout.HelpBox("You need to add prefabs to this layer in order to use it", MessageType.Info);

            if (GUILayout.Button("[+] Add prefab"))
                spawner.objectLayers[spawner.selectedObjectLayerIndex].AddPrefab();

            for (int i = 0; i < spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs.Length; i++)
            {
                EditorGUILayout.Space(12);

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
                        EditorGUILayout.Space();
                        spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation = RandomVector3.InputGUI("Rotation", spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i]._rotation, true);
                        EditorGUILayout.Space();
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
                            
                            EditorGUILayout.Space();
                        }

                        if (GUILayout.Button("Add material"))
                            spawner.objectLayers[spawner.selectedObjectLayerIndex].prefabs[i].AddColoring();
                    }
                }


                GUILayout.EndVertical();

                if (deleteLayer)
                    spawner.objectLayers[spawner.selectedObjectLayerIndex].RemovePrefab(i);
            }
        }


        void TabColorGroups()
        {
            if (spawner.selectedTab != MassSpawner.TAB_COLORS)
                return;

            EditorGUILayout.BeginHorizontal();

            // Color group selector
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            if (GUILayout.Button("[+] Add group"))
            {
                CloseAllColorGroups();
                AddColorGroup();
            }
            GUILayout.Space(10);
            int index = 0;
            foreach (ColorGroup colorGroup in spawner.colorGroups)
            {
                colorGroup.opened = CloseAllColorGroups(EditorUI.ToggleButton(EditorUI.StringMaxLength(colorGroup.name, 15), colorGroup.opened, string.Format("Color group #{0}", index)));
                index++;
            }
            EditorGUILayout.EndVertical();


            // Color group settings
            TabColorGroupsSettings();

            EditorGUILayout.EndHorizontal();

            UpdateSelectedColorGroup();
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