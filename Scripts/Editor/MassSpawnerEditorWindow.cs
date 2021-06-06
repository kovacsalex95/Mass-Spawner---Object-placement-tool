
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Reflection;

/*

    TODO:

    Priority:
    -   Non-rectangle height and slopemaps

    -   Preview gameObject (custom scene?)

    -   Get rid of MassSpawner instance
        -   Parameters into Scriptable Object
        -   Replace gameObject with World transform and presets
        -   World and preset selector

    -   Noise seed change (placement)


    Future:
    -   Realtime height & slopemap generation (CS)

    -   Terrain snapping
        -   RefreshTerrainSnap function
        -   World Bounds drag snap rect while dragging (for snap)
        -   Automatic terrain alignment (snap / attach)

    -   Terrain normal as rotation (with ratio)

    -   UI features
        -   Toggle panels
        -   Reset view button
        -   Editor mode select icons
        -   Color group settings

*/

namespace lxkvcs
{
    public class MassSpawnerEditorWindow : EditorWindow
    {
        MassSpawner spawner = null;

        bool placeObjects = false;

        string assetsFolder = "";
        string[] names = null;
        Texture2D[] icons = null;
        Texture2D objectIcon = null;


        List<Rect> ignoreMouseAreas = null;
        Vector2 mousePosition;
        Vector2 mouseDelta = Vector2.zero;
        bool mouseInGUI = false;


        GameObject cameraRotationPivot = null;
        Camera renderCamera = null;
        RenderTexture renderTexture = null;



        Rect previewArea;
        Vector2 layersScrollPos;
        Vector2 layerSettingsScrollPos;


        Vector3 cameraRotationEuler = new Vector3(45, 45, 0);

        bool leftMouseButtonDown = false;
        bool rightMouseButtonDown = false;

        Ray cameraRay;

        Vector3 worldBoundDragDirection = Vector3.zero;
        Vector3 worldBountDragHandlePosition;


        float leftPanelWidth => Mathf.Max(position.width * 0.2f, 220f);
        Rect leftPanelRect => new Rect(20, position.height - (position.height * 0.5f), leftPanelWidth, position.height * 0.5f - 20);

        float rightPanelWidth => Mathf.Max(position.width * 0.3f, 320f);

        static Color _DefaultBackgroundColor;
        static Color DefaultBackgroundColor
        {
            get
            {
                if (_DefaultBackgroundColor.a == 0)
                {
                    var method = typeof(EditorGUIUtility)
                        .GetMethod("GetDefaultBackgroundColor", BindingFlags.NonPublic | BindingFlags.Static);
                    _DefaultBackgroundColor = (Color)method.Invoke(null, null);
                }
                return _DefaultBackgroundColor;
            }
        }
        Color BackgroundColor(float alpha = 1f)
        {
            Color c = DefaultBackgroundColor;
            return new Color(c.r, c.g, c.b, alpha);
        }


        public OpenedView openedView = OpenedView.Placement;
        public LayerSettingsTab selectedSettingsTab = LayerSettingsTab.Placement;
        public EditorMode editorMode = EditorMode.Placement;


        public Texture2D terrainHeight = null;
        public Texture2D terrainSlope = null;
        public Texture2D terrain3D = null;

        public LayerMask surveyMask
        {
            get
            {
                LayerMask result = spawner.includeMask;

                bool[] excludeLayers = Util.LayerMaskHasLayers(spawner.excludeMask);
                for (int i = 0; i < excludeLayers.Length; i++)
                {
                    if (excludeLayers[i] && !Util.LayerMaskContainsLayer(result, i))
                        result = Util.AddLayerToLayerMask(result, i);
                }

                return result;
            }
        }

        public int selectedObjectLayerIndex = -1;
        public int selectedColorGroupIndex = -1;

        //public PreviewMode preview = PreviewMode.Heightmap;
        public bool showPlacement = true;

        int generatedPreviewTexturesResolution = -1;
        RenderTexture heightmapPreviewTexture = null;
        RenderTexture layerPreviewTexture = null;
        ComputeShader previewComputeShader = null;
        ComputeShader placementComputeShader = null;

        [System.NonSerialized]
        public bool mapsChecked = false;


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


        ObjectLayer selectedLayer
        {
            get
            {
                if (selectedObjectLayerIndex == -1)
                    return null;

                if (spawner.objectLayers == null)
                    return null;

                if (spawner.objectLayers[selectedObjectLayerIndex] == null)
                    return null;

                return spawner.objectLayers[selectedObjectLayerIndex];
            }
        }
        ColorGroup selectedColor
        {
            get
            {
                if (selectedColorGroupIndex == -1)
                    return null;

                if (spawner.colorGroups == null)
                    return null;

                if (spawner.colorGroups[selectedColorGroupIndex] == null)
                    return null;

                return spawner.colorGroups[selectedColorGroupIndex];
            }
        }


        [MenuItem("Tools/Mass Spawner")]
        public static void Init()
        {
            MassSpawnerEditorWindow window = (MassSpawnerEditorWindow)GetWindow(typeof(MassSpawnerEditorWindow), false);

            window.titleContent = new GUIContent("Mass Spawner", (Texture2D)AssetDatabase.LoadAssetAtPath(Util.AssetFolder + "/Resources/Icons/EditorIcon.png", typeof(Texture2D)));

            window.Show();

        }

        private void Update()
        {
            Repaint();
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


            if (terrainHeight == null)
                openedView = OpenedView.HeightmapWorld;


            if (spawner.objectLayers == null)
                spawner.objectLayers = new ObjectLayer[0];
            if (spawner.colorGroups == null)
                spawner.colorGroups = new ColorGroup[0];


            if ((terrainHeight == null || terrainSlope == null || terrain3D == null) && !mapsChecked)
                TryToLoadSavedMaps();


            if (selectedObjectLayerIndex != -1 && selectedLayer.objectPlaces == null)
                GenerateObjectPoints(selectedLayer);

            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;

            mouseInGUI = MouseInGUI();

            if (renderCamera != null)
                cameraRay = renderCamera.ScreenPointToRay(new Vector2(mousePosition.x, position.height - mousePosition.y));
        }



        // GUI EVENTS
        void HandleGUIEvents(Event currentEvent)
        {
            switch (currentEvent.type)
            {
                case EventType.MouseUp:

                    if (currentEvent.button == 0)
                        leftMouseButtonDown = false;
                    if (currentEvent.button == 1)
                        rightMouseButtonDown = false;

                    break;

                case EventType.MouseDown:

                    if (currentEvent.button == 0)
                        leftMouseButtonDown = true;
                    if (currentEvent.button == 1)
                        rightMouseButtonDown = true;

                    break;

                case EventType.MouseMove:

                    mouseDelta = currentEvent.mousePosition - mousePosition;
                    mousePosition = currentEvent.mousePosition;

                    GUIEventMouseMove();

                    break;

                case EventType.MouseDrag:

                    mouseDelta = currentEvent.mousePosition - mousePosition;
                    mousePosition = currentEvent.mousePosition;

                    GUIEventMouseMove();

                    break;

                case EventType.ScrollWheel:

                    GUIEventMouseScroll();

                    break;

                case EventType.MouseEnterWindow:

                    GUIEventMouseEnter();
                    break;

                case EventType.MouseLeaveWindow:

                    leftMouseButtonDown = false;
                    rightMouseButtonDown = false;

                    GUIEventMouseLeave();
                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(GetHashCode(), FocusType.Passive));
                    break;
            }

            ignoreMouseAreas.Clear();
        }
        void GUIEventMouseEnter()
        {
            // mouse enter
        }
        void GUIEventMouseLeave()
        {
            // mouse leave
            worldBoundDragDirection = Vector3.zero;
        }
        void GUIEventMouseDown()
        {

        }
        void GUIEventMouseUp()
        {

        }
        void GUIEventMouseScroll()
        {

        }
        void GUIEventMouseMove()
        {
            if (mouseInGUI)
                return;

            if (rightMouseButtonDown)
                RotateView();
        }
        bool MouseInGUI()
        {
            if (ignoreMouseAreas == null)
                ignoreMouseAreas = new List<Rect>();

            try
            {
                foreach (Rect rect in ignoreMouseAreas)
                {
                    if (rect.Contains(mousePosition))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        void RotateView()
        {
            if (cameraRotationPivot == null)
                return;

            Vector2 viewRotateSpeed = new Vector2(0.3f, 0.15f);

            float cameraEulerY = cameraRotationEuler.y + mouseDelta.x * viewRotateSpeed.x;
            while (cameraEulerY >= 360f)
                cameraEulerY -= 360f;
            while (cameraEulerY < 0f)
                cameraEulerY += 360f;

            float cameraEulerX = Mathf.Clamp(cameraRotationEuler.x + mouseDelta.y * viewRotateSpeed.y, -90f, 90f);

            Vector3 vel = Vector3.zero;
            cameraRotationEuler = new Vector3(cameraEulerX, cameraEulerY, cameraRotationEuler.z);

            cameraRotationPivot.transform.localEulerAngles = cameraRotationEuler;
        }



        // DRAWING
        public void OnGUI()
        {
            CheckVariables();


            HandleGUIEvents(Event.current);

            
            RenderPreview();
            RenderGizmos();

            WorldBoundsDrag();


            ObjectButtons();


            UpdateLogic();


            ModalSelectButtons();


            // Object layers
            TabObjectLayers();


            // Layer settings
            TabObjectLayersSettings();

            // World & Heightmap settings
            TabWorldAndHeightmap();

            return;

            // Color groups
            TabColorGroups();


            // Do the placement
        }


        bool DrawPanel(Rect rect, string title = "", string buttonText = "", float buttonWidth = 60, float normalOpacity = 0.5f, float hoverOpacity = 1f)
        {
            return DrawPanel(rect, 0, title, buttonText, buttonWidth, normalOpacity, hoverOpacity);
        }
        bool DrawPanel(Rect rect, float padding = 0, string title = "", string buttonText = "", float buttonWidth = 60, float normalOpacity = 0.5f, float hoverOpacity = 1f)
        {
            return DrawPanel(rect, Vector3.one * padding, title, buttonText, buttonWidth, normalOpacity, hoverOpacity);
        }
        bool DrawPanel(Rect rect, Vector3 padding, string title = "", string buttonText = "", float buttonWidth = 60, float normalOpacity = 0.5f, float hoverOpacity = 1f)
        {
            bool result = false;

            float opacity = rect.Contains(mousePosition) ? hoverOpacity : normalOpacity;

            ignoreMouseAreas.Add(rect);

            EditorGUI.DrawRect(rect, BackgroundColor(opacity));


            float topOffset = 0;

            if (title != "")
            {
                topOffset = 30;

                float xOffset = padding.x > 0 ? padding.x : 5;

                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 30), EditorGUIUtility.isProSkin ? new Color(0, 0, 0, 0.2f) : new Color(1, 1, 1, 0.2f));
                GUI.Label(new Rect(rect.x + xOffset, rect.y + 6, rect.width - xOffset, 18), title);

                if (buttonText != "")
                    result = GUI.Button(new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, 30), buttonText);
            }


            GUILayout.BeginArea(new Rect(rect.x + padding.x, rect.y + topOffset, rect.width - padding.x - padding.z, rect.height - topOffset));

            if (padding.y > 0)
                GUILayout.Space(padding.y);

            return result;
        }
        void EndPanel(float padding = 0)
        {
            if (padding > 0)
                GUILayout.Space(padding);

            GUILayout.EndArea();
        }


        void UpdateLogic()
        {
            if (placeObjects)
            {
                if (terrainHeight == null)
                    GenerateHeightmap();

                GenerateObjectPoints();
                PlaceObjects();
                placeObjects = false;
            }
        }

        void CheckCamera()
        {
            if (renderCamera != null)
                return;

            GameObject old = GameObject.Find("massspawner_cameraholder");
            while (old != null) {
                DestroyImmediate(old);
                old = GameObject.Find("massspawner_cameraholder");
            }

            GameObject cameraHolder = new GameObject("massspawner_cameraholder");
            cameraHolder.hideFlags = HideFlags.HideAndDontSave;
            cameraHolder.transform.position = worldCenter;
            cameraHolder.transform.eulerAngles = spawner.transform.eulerAngles;

            cameraRotationPivot = new GameObject("massspawner_camerarotate");
            cameraRotationPivot.transform.parent = cameraHolder.transform;
            cameraRotationPivot.transform.localPosition = Vector3.zero;
            cameraRotationPivot.transform.localEulerAngles = Vector3.zero;

            GameObject cameraObject = new GameObject("massspawner_camera");
            cameraObject.transform.parent = cameraRotationPivot.transform;
            cameraObject.transform.localEulerAngles = Vector3.zero;
            cameraObject.transform.localPosition = new Vector3(0, 0, -(Mathf.Max(spawner.terrainSize.x, spawner.terrainSize.y)));

            cameraRotationPivot.transform.localEulerAngles = cameraRotationEuler;

            Color backColor = BackgroundColor();

            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.farClipPlane = -cameraObject.transform.localPosition.z * 2;
            renderCamera.allowHDR = false;
            renderCamera.allowMSAA = false;
            renderCamera.backgroundColor = new Color(backColor.r / 2, backColor.g / 2, backColor.b / 2, 1);
            renderCamera.cameraType = CameraType.SceneView;
            renderCamera.clearFlags = CameraClearFlags.Color;
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = Mathf.Max(spawner.terrainSize.x, spawner.terrainSize.y) / 2;
        }

        void RenderPreview()
        {
            CheckCamera();

            int screenW = Mathf.CeilToInt(position.width);
            int screenH = Mathf.CeilToInt(position.height);

            if (renderTexture == null || renderTexture.width != screenW || renderTexture.height != screenH)
            {
                renderTexture = new RenderTexture(screenW, screenH, 0);
                renderTexture.depth = 32;
                renderTexture.Create();
                renderCamera.targetTexture = renderTexture;
            }

            renderCamera.cullingMask = surveyMask;
            renderCamera.Render();

            GUI.DrawTexture(new Rect(0, 0, position.width, position.height), renderTexture);
        }

        void RenderGizmos()
        {
            if (renderCamera == null)
                return;

            Handles.SetCamera(renderCamera);

            WorldResizeGizmos();
        }

        bool DrawResizeHandle(Ray ray, Vector3 cameraForward, Vector3 direction, float distance, float size, out Vector3 dragDirection)
        {
            dragDirection = Vector3.zero;

            if (Mathf.Abs(Vector3.Dot(cameraForward, direction)) > 0.96f)
                return false;

            Vector3 handlePosition = worldCenter + direction * distance;

            Plane checkPlane = new Plane(direction, handlePosition);
            float hit;

            if (checkPlane.Raycast(ray, out hit))
            {
                Vector3 point = ray.GetPoint(hit);

                if (Vector3.Distance(point, handlePosition) <= size)
                {
                    Handles.DrawSolidDisc(handlePosition, direction, size);

                    if (leftMouseButtonDown)
                    {
                        worldBountDragHandlePosition = handlePosition;
                        dragDirection = direction;
                    }

                    return true;
                }
            }

            Handles.SphereHandleCap(0, handlePosition, Quaternion.LookRotation(direction), size, EventType.Repaint);

            return false;
        }

        void WorldResizeGizmos()
        {
            if (openedView != OpenedView.HeightmapWorld)
                return;

            Vector3 worldSize = new Vector3(spawner.terrainSize.x, spawner.terrainTop - spawner.terrainBottom, spawner.terrainSize.y);
            Vector3 halfSize = worldSize / 2f;
            float handleSize = Mathf.Max(1, worldSize.z * 0.05f);

            Handles.color = Handles.selectedColor;
            Handles.DrawWireCube(worldCenter, worldSize);


            if (worldBoundDragDirection != Vector3.zero && !leftMouseButtonDown)
                worldBoundDragDirection = Vector3.zero;

            if (worldBoundDragDirection != Vector3.zero)
                return;

            Vector3 forward = renderCamera.transform.forward;


            Handles.color = Handles.xAxisColor;
            if (DrawResizeHandle(cameraRay, forward, Vector3.right, halfSize.x, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;
            if (DrawResizeHandle(cameraRay, forward, Vector3.left, halfSize.x, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;


            Handles.color = Handles.yAxisColor;
            if (DrawResizeHandle(cameraRay, forward, Vector3.up, halfSize.y, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;
            if (DrawResizeHandle(cameraRay, forward, Vector3.down, halfSize.y, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;


            Handles.color = Handles.zAxisColor;
            if (DrawResizeHandle(cameraRay, forward, Vector3.forward, halfSize.z, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;
            if (DrawResizeHandle(cameraRay, forward, Vector3.back, halfSize.z, handleSize, out worldBoundDragDirection) && leftMouseButtonDown)
                return;
        }

        void WorldBoundsDrag()
        {
            if (worldBoundDragDirection == Vector3.zero || renderCamera == null)
                return;

            Plane plane;


            // X
            if (worldBoundDragDirection.x != 0)
                plane = new Plane(Vector3.up, worldCenter);

            // Y
            if (worldBoundDragDirection.y != 0)
            {
                if (Vector3.Dot(-renderCamera.transform.forward, Vector3.right) > Vector3.Dot(-renderCamera.transform.forward, Vector3.forward))
                    plane = new Plane(Vector3.right, worldCenter);
                else
                    plane = new Plane(Vector3.forward, worldCenter);
            }

            // Z
            else
                plane = new Plane(Vector3.up, worldCenter);


            float hitPoint;

            if (!plane.Raycast(cameraRay, out hitPoint))
                return;

            Vector3 newPoint = cameraRay.GetPoint(hitPoint);
            Vector3 diff = newPoint - worldBountDragHandlePosition;


            if (worldBoundDragDirection.x != 0)
            {
                spawner.terrainSize.x += diff.x * worldBoundDragDirection.x;
                spawner.terrainOffset.x += diff.x / 2f;
            }
            else if (worldBoundDragDirection.y != 0)
            {
                if (worldBoundDragDirection.y == 1)
                    spawner.terrainTop += diff.y;
                else
                    spawner.terrainBottom += diff.y;
            }
            else
            {
                spawner.terrainSize.y += diff.z * worldBoundDragDirection.z;
                spawner.terrainOffset.y += diff.z / 2f;
            }

            worldBountDragHandlePosition = newPoint;
        }


        void ObjectButtons()
        {
            if (openedView != OpenedView.Placement)
                return;

            float buttonsWidth = Mathf.Min(spawner.transform.childCount > 0 ? 250 : 125, position.width);
            Rect buttonsRect = new Rect((position.width - buttonsWidth) / 2, 10, buttonsWidth, 40);

            ignoreMouseAreas.Add(buttonsRect);
            GUILayout.BeginArea(buttonsRect);

            GUILayout.BeginHorizontal();

            if (spawner.objectLayers.Length > 0 && terrainHeight != null)
            {
                if (GUILayout.Button(spawner.transform.childCount > 0 ? "Update objects" : "Place objects", GUILayout.Height(30)))
                    placeObjects = true;
            }
            if (spawner.transform.childCount > 0)
            {
                if (GUILayout.Button(string.Format("Delete objects ({0})", spawner.transform.childCount), GUILayout.Height(30)))
                    ClearObjects();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void ModalSelectButtons()
        {
            Rect selectRect = new Rect(10, 10, leftPanelWidth, 30);

            ignoreMouseAreas.Add(selectRect);

            GUILayout.BeginArea(selectRect);

            GUILayout.BeginHorizontal();

            OpenedView oldModal = openedView;
            openedView = (OpenedView)GUILayout.Toolbar((int)openedView, new string[] { "Placement", "Colors", "World" }, GUILayout.Height(selectRect.height));

            if (oldModal != openedView)
            {
                if (openedView == OpenedView.HeightmapWorld)
                    RefreshTerrainSnap();
            }

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        void RefreshTerrainSnap()
        {

        }

        /*void PreviewModeSelection()
        {
            string[] labels = new string[]
            {
                "Terrain height",
                "Steepness and slopes",
                "Stylized 3D"
            };

            GUI.Label(new Rect(20, 5, 130, 20), labels[(int)spawner.preview]);
            preview = (PreviewMode)GUILayout.SelectionGrid((int)spawner.preview, icons, 1, GUILayout.Width(iconsize));

            GUILayout.Space(5);

            if (selectedObjectLayerIndex != -1)
            {
                bool oldShow = spawner.showPlacement;
                showPlacement = GUILayout.Toggle(spawner.showPlacement, objectIcon, "Button", GUILayout.Height(iconsize), GUILayout.Width(iconsize));

                if (oldShow != showPlacement)
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

            
        }*/

        void TabWorldAndHeightmap()
        {
            if (openedView != OpenedView.HeightmapWorld)
                return;

            DrawPanel(leftPanelRect, 10, "World & Heightmap");

            spawner.terrainTop = EditorGUILayout.FloatField("Top", spawner.terrainTop);
            spawner.terrainBottom = EditorGUILayout.FloatField("Bottom", spawner.terrainBottom);
            spawner.terrainOffset = EditorGUILayout.Vector2Field("Offset", spawner.terrainOffset);
            spawner.terrainSize = EditorGUILayout.Vector2Field("Size", spawner.terrainSize);


            GUILayout.Space(20);

            spawner.heightmapResolution = (Resolutions)EditorGUILayout.EnumPopup("Heightmap resolution", spawner.heightmapResolution);

            spawner.includeMask = EditorUI.LayerMaskField("Terrain layers", spawner.includeMask);
            spawner.excludeMask = EditorUI.LayerMaskField("Exclude layers", spawner.excludeMask);

            if (GUILayout.Button("Generate heightmap"))
            {
                GenerateHeightmap();
                openedView = OpenedView.Placement;
            }

            EndPanel();

            if (terrainHeight != null)
                GUI.DrawTexture(new Rect(position.width - 210, 10, 200, 200), GenerateHeightmapPreviewTexture(terrainHeight), ScaleMode.ScaleToFit);
        }

        void TabObjectLayers()
        {
            if (openedView != OpenedView.Placement)
                return;


            bool newLayer = DrawPanel(leftPanelRect, 10, "Layers", "[+] New");

            if (newLayer)
            {
                CloseAllLayers();
                AddObjectLayer();
            }


            // Layer selector



            EditorGUILayout.BeginVertical();

            layersScrollPos = EditorGUILayout.BeginScrollView(layersScrollPos);

            int index = 0;
            foreach (ObjectLayer objectLayer in spawner.objectLayers)
            {
                GUILayout.BeginHorizontal();

                objectLayer.opened = CloseAllLayers(EditorUI.ToggleButton(EditorUI.StringMaxLength(objectLayer.name, 15), objectLayer.opened, string.Format("Object layer #{0}", index)));

                int operation = GUILayout.Toolbar(-1, new string[] { "↑", "↓", "+", "×" }, GUILayout.Height(24), GUILayout.Width(4 * 20));



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

                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            EndPanel(10);



            UpdateSelectedObject();
        }

        void TabObjectLayersSettings()
        {
            if (openedView != OpenedView.Placement)
                return;

            Rect layersSettinsRect = new Rect(position.width - rightPanelWidth - 20, position.height - (position.height * 0.5f), rightPanelWidth, position.height * 0.5f - 20);


            string buttonText = "";

            if (selectedSettingsTab == LayerSettingsTab.Collision)
                buttonText = "[+] Collision";
            else if (selectedSettingsTab == LayerSettingsTab.Prefabs)
                buttonText = "[+] Prefab";


            bool addAction = DrawPanel(layersSettinsRect, 10, "Layer settings", buttonText, 90);



            if (selectedObjectLayerIndex == -1)
            {
                EditorGUILayout.HelpBox("Select a layer from the left side to customize its settings", MessageType.Info);
                return;
            }


            GUILayout.BeginVertical();

            // Name
            selectedLayer.name = EditorGUILayout.TextField("Name", selectedLayer.name);

            EditorGUILayout.Space(10);



            selectedSettingsTab = (LayerSettingsTab)GUILayout.Toolbar((int)selectedSettingsTab, new string[] { "Terrain", "Transform", "Rules", "Prefabs" }, GUILayout.Height(24));

            EditorGUILayout.Space();



            layerSettingsScrollPos = GUILayout.BeginScrollView(layerSettingsScrollPos);

            GUILayout.BeginVertical();



            selectedLayer.CheckArrays();


            TabObjectLayerSettingPlacement();


            TabObjectLayerSettingCollision(addAction);


            TabObjectLayerSettingTransform();


            TabObjectLayerSettingPrefabs(addAction);



            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndVertical();


            EndPanel(10);
        }

        void TabObjectLayerSettingPlacement()
        {
            if (selectedSettingsTab != LayerSettingsTab.Placement)
                return;

            // Terrain height
            GUILayout.BeginHorizontal();
            GUILayout.Label("Terrain height");
            GUILayout.Space(10);
            selectedLayer.from = Mathf.Clamp01(EditorGUILayout.FloatField(selectedLayer.from, GUILayout.MaxWidth(40)));
            EditorGUILayout.MinMaxSlider(ref selectedLayer.from, ref selectedLayer.to, 0, 1);
            selectedLayer.to = Mathf.Clamp01(EditorGUILayout.FloatField(selectedLayer.to, GUILayout.MaxWidth(40)));
            GUILayout.EndHorizontal();

            // Terrain slope
            GUILayout.BeginHorizontal();
            GUILayout.Label("Terrain slope");
            GUILayout.Space(10);
            selectedLayer.minSlope = Mathf.Clamp01(EditorGUILayout.FloatField(selectedLayer.minSlope, GUILayout.MaxWidth(40)));
            EditorGUILayout.MinMaxSlider(ref selectedLayer.minSlope, ref selectedLayer.maxSlope, 0, 1);
            selectedLayer.maxSlope = Mathf.Clamp01(EditorGUILayout.FloatField(selectedLayer.maxSlope, GUILayout.MaxWidth(40)));
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Rarity & angleOffset
            float everyN = selectedLayer.everyN;
            float angleOffset = selectedLayer.angleOffset;
            float organicity = selectedLayer.organicity;
            selectedLayer.everyN = 30 - EditorGUILayout.IntSlider("Density", 30 - selectedLayer.everyN, 0, 27);
            selectedLayer.angleOffset = EditorGUILayout.Slider("Chaotics", selectedLayer.angleOffset, 0, 1);
            selectedLayer.organicity = EditorGUILayout.Slider("Organicity", selectedLayer.organicity, 0, 1);

            if (everyN != selectedLayer.everyN ||
                angleOffset != selectedLayer.angleOffset ||
                organicity != selectedLayer.organicity)
                GenerateObjectPoints(selectedLayer);
        }
        void TabObjectLayerSettingTransform()
        {
            if (selectedSettingsTab != LayerSettingsTab.Transform)
                return;

            selectedLayer._position = RandomVector3.InputGUI("Offset", selectedLayer._position);
            EditorGUILayout.Space();
            selectedLayer._rotation = RandomVector3.InputGUI("Rotation", selectedLayer._rotation);
            EditorGUILayout.Space();
            selectedLayer._scale = RandomVector3.InputGUI("Scale", selectedLayer._scale);
        }
        void TabObjectLayerSettingCollision(bool addAction)
        {
            if (selectedSettingsTab != LayerSettingsTab.Collision)
                return;



            if (selectedLayer.collisionRules.Length == 0)
                EditorGUILayout.HelpBox("You dont have collision rules yet. (↑)", MessageType.Info);

            for (int i = 0; i < selectedLayer.collisionRules.Length; i++)
            {
                EditorGUILayout.Space(12);

                GUILayout.BeginVertical(EditorStyles.textArea);

                selectedLayer.collisionRules[i].mode = (CollisionMode)EditorGUILayout.EnumPopup("Behaviour", selectedLayer.collisionRules[i].mode);

                selectedLayer.collisionRules[i].layerMask = EditorUI.LayerMaskField("Detection layers", selectedLayer.collisionRules[i].layerMask);

                float newRadius = EditorGUILayout.FloatField("Detection radius (m)", selectedLayer.collisionRules[i].radius);
                if (newRadius > 0)
                    selectedLayer.collisionRules[i].radius = newRadius;

                if (i < selectedLayer.collisionRules.Length - 1)
                    selectedLayer.collisionRules[i].stop = EditorGUILayout.Toggle("Ignore rules below if detected", selectedLayer.collisionRules[i].stop);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("↑", GUILayout.Width(24)))
                {
                    selectedLayer.MoveRuleUp(i);
                    break;
                }
                if (GUILayout.Button("↓", GUILayout.Width(24)))
                {
                    selectedLayer.MoveRuleDown(i);
                    break;
                }

                if (GUILayout.Button("Delete rule"))
                {
                    selectedLayer.RemoveRule(i);
                    break;
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            if (addAction)
                selectedLayer.AddRule();
        }
        void TabObjectLayerSettingPrefabs(bool addAction)
        {
            if (selectedSettingsTab != LayerSettingsTab.Prefabs)
                return;

            selectedLayer.placing = (PlaceType)EditorGUILayout.EnumPopup("Placement method", selectedLayer.placing);

            if (selectedLayer.placing == PlaceType.Prefabs)
            {
                for (int i = 0; i < selectedLayer.prefabs.Length; i++)
                {
                    if (selectedLayer.prefabs[i].coloring)
                        selectedLayer.prefabs[i].coloring = false;
                }

                EditorGUILayout.HelpBox("When you use Prefab placement mode you cannot use coloring options.\nThe objects going to have Prefab connection.", MessageType.Warning);
            }


            EditorGUILayout.Space(10);

            if (selectedLayer.prefabs.Length == 0)
                EditorGUILayout.HelpBox("You need to add prefabs to this layer in order to use it (↑)", MessageType.Info);


            for (int i = 0; i < selectedLayer.prefabs.Length; i++)
            {
                EditorGUILayout.Space(12);

                bool deleteLayer = false;

                GUILayout.BeginVertical(EditorStyles.textArea);

                EditorGUILayout.BeginHorizontal();
                selectedLayer.prefabs[i].prefab = (GameObject)EditorGUILayout.ObjectField(selectedLayer.prefabs[i].prefab, typeof(GameObject), false);
                if (GUILayout.Button("Delete"))
                    deleteLayer = true;
                if (selectedLayer.prefabs[i].prefab != null)
                {
                    selectedLayer.prefabs[i].transform = GUILayout.Toggle(selectedLayer.prefabs[i].transform, "Transform", "Button");

                    if (selectedLayer.placing == PlaceType.Instantiate)
                        selectedLayer.prefabs[i].coloring = GUILayout.Toggle(selectedLayer.prefabs[i].coloring, "Colors", "Button");
                }
                EditorGUILayout.EndHorizontal();

                if (selectedLayer.prefabs[i].prefab != null)
                {
                    if (selectedLayer.prefabs[i].transform || selectedLayer.prefabs[i].coloring)
                        EditorGUILayout.Space();

                    if (selectedLayer.prefabs[i].transform)
                    {
                        selectedLayer.prefabs[i]._position = RandomVector3.InputGUI("Offset", selectedLayer.prefabs[i]._position, true);
                        EditorGUILayout.Space();
                        selectedLayer.prefabs[i]._rotation = RandomVector3.InputGUI("Rotation", selectedLayer.prefabs[i]._rotation, true);
                        EditorGUILayout.Space();
                        selectedLayer.prefabs[i]._scale = RandomVector3.InputGUI("Scale", selectedLayer.prefabs[i]._scale, true);
                    }

                    if (selectedLayer.prefabs[i].transform && selectedLayer.prefabs[i].coloring)
                        EditorGUILayout.Space();

                    if (selectedLayer.prefabs[i].coloring)
                    {
                        if (selectedLayer.prefabs[i].colors == null)
                            selectedLayer.prefabs[i].colors = new MaterialColoring[0];

                        for (int c = 0; c < selectedLayer.prefabs[i].colors.Length; c++)
                        {
                            bool c_remove = false;

                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("X", GUILayout.Width(20)))
                                c_remove = true;
                            selectedLayer.prefabs[i].colors[c].material = (Material)EditorGUILayout.ObjectField(selectedLayer.prefabs[i].colors[c].material, typeof(Material), false);

                            string oldPropertyName = selectedLayer.prefabs[i].colors[c].propertyName;
                            string propertyName = EditorGUILayout.TextField(oldPropertyName);
                            if (propertyName == "")
                                propertyName = Util.ProjectIsSRP ? "_BaseColor" : "_Color";

                            selectedLayer.prefabs[i].colors[c].propertyName = propertyName;

                            if (selectedLayer.prefabs[i].colors[c].colorGroup != -1 && !ColorGroupExists(selectedLayer.prefabs[i].colors[c].colorGroup))
                                selectedLayer.prefabs[i].colors[c].colorGroup = -1;

                            int selectedGroup = EditorGUILayout.Popup(selectedLayer.prefabs[i].colors[c].colorGroup, colorGroupLabels);
                            selectedLayer.prefabs[i].colors[c].colorGroup = spawner.colorGroups.Length > 0 ? selectedGroup : -1;

                            EditorGUILayout.EndHorizontal();

                            if (c_remove)
                                selectedLayer.prefabs[i].RemoveColoring(c);
                            
                            EditorGUILayout.Space();
                        }

                        if (GUILayout.Button("Add material"))
                            selectedLayer.prefabs[i].AddColoring();
                    }
                }


                GUILayout.EndVertical();

                if (deleteLayer)
                    selectedLayer.RemovePrefab(i);
            }

            if (addAction)
                selectedLayer.AddPrefab();
        }



        void TabColorGroups()
        {
            if (openedView != OpenedView.ColorGroups)
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
            if (selectedColorGroupIndex != -1)
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
                selectedColor.name = EditorGUILayout.TextField("Name", selectedColor.name);

                // Mode
                selectedColor.mode = (ColorGroupMode)EditorGUILayout.EnumPopup("Mixing mode", selectedColor.mode);

                EditorGUILayout.Space();
                if (selectedColor.mode == ColorGroupMode.RGB)
                    selectedColor.rgb = RandomVector3.InputGUI(selectedColor.rgb, "R", "G", "B");
                else if (selectedColor.mode == ColorGroupMode.HSV)
                    selectedColor.hsv = RandomVector3.InputGUI(selectedColor.hsv, "H", "S", "V");
                else if (selectedColor.mode == ColorGroupMode.Gradient)
                    selectedColor.gradient = EditorGUILayout.GradientField(selectedColor.gradient);
                else if (selectedColor.mode == ColorGroupMode.ColorLerp)
                {
                    EditorGUILayout.BeginHorizontal();
                    selectedColor.color1 = EditorGUILayout.ColorField(selectedColor.color1);
                    selectedColor.color2 = EditorGUILayout.ColorField(selectedColor.color2);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.ColorField("Preview color", selectedColor.value);

                GUILayout.EndVertical();

                if (remove)
                {
                    CloseAllColorGroups();
                    RemoveColorGroup(selectedColorGroupIndex);
                }
                if (duplicate)
                {
                    CloseAllColorGroups();
                    DuplicateColorGroup(selectedColorGroupIndex);
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
            selectedColorGroupIndex = spawner.colorGroups.Length - 1;
            selectedColor.opened = true;
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
                selectedColorGroupIndex = spawner.colorGroups.Length - 1;
                selectedColor.opened = true;
            }
            else
                selectedColorGroupIndex = -1;
        }

        public void DuplicateColorGroup(int index)
        {
            if (selectedColorGroupIndex != -1)
                selectedColor.opened = false;

            ColorGroup newGroup = new ColorGroup(spawner.colorGroups[index]);
            newGroup.name += " (clone)";
            newGroup.opened = true;
            AddColorGroup(newGroup);

            selectedColorGroupIndex = spawner.colorGroups.Length - 1;
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
            selectedColorGroupIndex = -1;
            int index = 0;
            foreach (ColorGroup colorGroup in spawner.colorGroups)
            {
                if (colorGroup.opened)
                {
                    selectedColorGroupIndex = index;
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
            selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
            selectedLayer.opened = true;
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
                selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
                selectedLayer.opened = true;
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

                if (selectedObjectLayerIndex == fromIndex)
                {
                    selectedObjectLayerIndex = toIndex;
                    selectedLayer.opened = true;
                }
            }
        }

        public void DuplicateObjectLayer(int index)
        {
            if (selectedObjectLayerIndex != -1)
                selectedLayer.opened = false;

            ObjectLayer newLayer = new ObjectLayer(spawner.objectLayers[index]);
            newLayer.name += " (clone)";
            newLayer.opened = true;
            AddObjectLayer(newLayer);

            selectedObjectLayerIndex = spawner.objectLayers.Length - 1;
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
            selectedObjectLayerIndex = -1;
            int index = 0;
            foreach (ObjectLayer objectLayer in spawner.objectLayers)
            {
                if (objectLayer.opened)
                {
                    selectedObjectLayerIndex = index;
                    return;
                }
                index++;
            }
        }




        // HEIGHTMAP
        public void GenerateHeightmap()
        {
            this.terrainHeight = new Texture2D((int)spawner.heightmapResolution, (int)spawner.heightmapResolution, TextureFormat.RGBAFloat, false);

            float terrainHeight = spawner.terrainTop - spawner.terrainBottom;
            if (terrainHeight > 0)
            {
                RaycastHit hit;
                Color nullColor = new Color(0, 0, 0, 0);

                for (int x = 0; x < (int)spawner.heightmapResolution; x++)
                {
                    for (int y = 0; y < (int)spawner.heightmapResolution; y++)
                    {
                        Vector3 pos = HeightmapToWorld(x, y);
                        Color c = nullColor;
                        if (Physics.Raycast(pos, Vector3.down, out hit, terrainHeight + 1, surveyMask))
                        {
                            bool isExcluded = Util.LayerMaskContainsLayer(spawner.excludeMask, hit.collider.gameObject.layer);
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
            Vector3 centerTop = spawner.transform.position + new Vector3(spawner.terrainOffset.x, 0, spawner.terrainOffset.y) + Vector3.up * spawner.terrainTop;
            Vector3 centerCorner = centerTop + new Vector3(-spawner.terrainSize.x / 2, 0, -spawner.terrainSize.y / 2);

            float xRatio = (float)xx / (float)spawner.heightmapResolution;
            float yRatio = (float)yy / (float)spawner.heightmapResolution;

            float absX = spawner.terrainSize.x * xRatio;
            float absY = spawner.terrainSize.y * yRatio;

            return centerCorner + new Vector3(absX, 0, absY);
        }



        // SLOPEMAP
        public void GenerateTerrainSlope()
        {
            float heightRatioX = (spawner.terrainTop - spawner.terrainBottom) / spawner.terrainSize.x;
            float heightRatioY = (spawner.terrainTop - spawner.terrainBottom) / spawner.terrainSize.y;

            float SIN45 = 0.707106781f;

            float biggestSlope = 0f;

            terrainSlope = new Texture2D((int)spawner.heightmapResolution, (int)spawner.heightmapResolution, TextureFormat.RGBAFloat, false);
            terrain3D = new Texture2D((int)spawner.heightmapResolution, (int)spawner.heightmapResolution, TextureFormat.RGBAFloat, false);
            for (int x = 0; x < (int)spawner.heightmapResolution; x++)
            {
                for (int y = 0; y < (int)spawner.heightmapResolution; y++)
                {
                    Color targetPixel = new Color(0, 0, 0, 0);
                    Color target3D = new Color(0, 0, 0, 0);
                    Color terrainPixel = terrainHeight.GetPixel(x, y);

                    if (terrainPixel.a > 0)
                    {
                        float height = terrainPixel.r;

                        // Steepness
                        float xDiff = (x <= (int)spawner.heightmapResolution - 1) ? terrainPixel.r - terrainHeight.GetPixel(x + 1, y).r : 0;
                        float xAngle = Mathf.Abs(xDiff) / heightRatioX;
                        float xSlope = Mathf.Atan(xAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float yDiff = (y <= (int)spawner.heightmapResolution - 1) ? terrainPixel.r - terrainHeight.GetPixel(x, y + 1).r : 0;
                        float yAngle = Mathf.Abs(yDiff) / heightRatioY;
                        float ySlope = Mathf.Atan(yAngle) * Mathf.Rad2Deg / 45f + 0.5f;

                        float dDiff = 0;
                        if (x <= (int)spawner.heightmapResolution - 1 && y <= (int)spawner.heightmapResolution - 1)
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

                        distance *= (float)spawner.heightmapResolution / (float)Resolutions._512x512;
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

            for (int x = 0; x < (int)spawner.heightmapResolution; x++)
            {
                for (int y = 0; y < (int)spawner.heightmapResolution; y++)
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
            Texture2DArray mapsArray = new Texture2DArray((int)spawner.heightmapResolution, (int)spawner.heightmapResolution, 3, TextureFormat.RGBAFloat, false);

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
            foreach (ObjectLayer l in spawner.objectLayers)
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

            int placePointsCount = Util.PlacementPointCount((int)spawner.heightmapResolution, layer.everyN);
            PlacementPoint[] placePoints = new PlacementPoint[placePointsCount];


            layer.objectPlaces = new RenderTexture((int)spawner.heightmapResolution, (int)spawner.heightmapResolution, 0, RenderTextureFormat.ARGB32);
            layer.objectPlaces.enableRandomWrite = true;
            layer.objectPlaces.filterMode = FilterMode.Point;

            layer.objectPlaces.Create();


            int kernelIndex = placementComputeShader.FindKernel("CSClear");
            placementComputeShader.SetTexture(kernelIndex, "PlacementMap", layer.objectPlaces);
            placementComputeShader.Dispatch(kernelIndex, (int)spawner.heightmapResolution / 8, (int)spawner.heightmapResolution / 8, 1);


            ComputeBuffer placementPointsBuffer = new ComputeBuffer(placePointsCount, PlacementPoint.stride);
            placementPointsBuffer.SetData(placePoints);


            kernelIndex = placementComputeShader.FindKernel("CSMain");
            placementComputeShader.SetTexture(kernelIndex, "PlacementMap", layer.objectPlaces);
            placementComputeShader.SetFloat("TextureSize", (float)spawner.heightmapResolution);
            placementComputeShader.SetFloat("AngleOffset", layer.angleOffset);
            placementComputeShader.SetFloat("EveryN", layer.everyN);
            placementComputeShader.SetFloat("Organicity", layer.organicity);
            placementComputeShader.SetBuffer(kernelIndex, "Points", placementPointsBuffer);
            placementComputeShader.Dispatch(kernelIndex, (int)spawner.heightmapResolution / 8, (int)spawner.heightmapResolution / 8, 1);

            placementPointsBuffer.GetData(placePoints);

            placementPointsBuffer.Dispose();

            layer.objectPoints = placePoints;
        }

        public void ResetPlacementTextures()
        {
            foreach (ObjectLayer layer in spawner.objectLayers)
                ResetLayerPlacementTextures(layer);
        }

        public void ResetLayerPlacementTextures(ObjectLayer layer)
        {
            layer.objectPlaces = null;
            GenerateObjectPoints(layer);
        }


        public void ClearObjects()
        {
            int childs = spawner.transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(spawner.transform.GetChild(i).gameObject);
            }
        }

        public void PlaceObjects()
        {
            int placedCount = 0;
            ClearObjects();

            if (spawner.objectLayers.Length == 0)
                return;

            for (int i = 0; i < spawner.objectLayers.Length; i++)
            {
                ObjectLayer previewObjectLayer = selectedLayer;
                if (previewObjectLayer.objectPoints == null || previewObjectLayer.objectPoints.Length == 0)
                    GenerateObjectPoints(previewObjectLayer);
            }

            if (terrainHeight != null)
            {
                foreach (ObjectLayer layer in spawner.objectLayers)
                {
                    foreach (PlacementPoint point in layer.objectPoints)
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
                Vector3 pos = posTop + Vector3.down * heightRatio * (spawner.terrainTop - spawner.terrainBottom) + Offset.value;

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

                    newObject.transform.parent = spawner.transform;

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
                                    Color color = spawner.colorGroups[coloring.colorGroup].value;

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



        // PREVIEW
        void CheckPreviewTextures()
        {
            if (generatedPreviewTexturesResolution == (int)spawner.heightmapResolution && heightmapPreviewTexture != null && layerPreviewTexture != null && previewComputeShader != null && placementComputeShader != null)
                return;

            generatedPreviewTexturesResolution = (int)spawner.heightmapResolution;

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
            previewComputeShader.SetTexture(kernelIndex, "PlacementMap", selectedLayer.objectPlaces);
            previewComputeShader.SetVector("HeightRange", new Vector4(selectedLayer.from, selectedLayer.to, 0, 0));
            previewComputeShader.SetVector("SlopeRange", new Vector4(selectedLayer.minSlope, selectedLayer.maxSlope, 0, 0));
            previewComputeShader.SetBool("ShowPlacement", showPlacement);

            previewComputeShader.Dispatch(kernelIndex, heightmapPreviewTexture.width / 8, heightmapPreviewTexture.height / 8, 1);

            return layerPreviewTexture;
        }

        public Vector3 worldCenterBottom => spawner.transform.position + new Vector3(spawner.terrainOffset.x, 0, spawner.terrainOffset.y);
        public Vector3 worldCenter => worldCenterBottom + Vector3.up * ((spawner.terrainBottom + spawner.terrainTop) / 2f);
    }
}