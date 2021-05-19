using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace lxkvcs
{
    public class Util
    {
        public static bool LayerMaskContainsLayer(LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static LayerMask AddLayerToLayerMask(LayerMask mask, int layer)
        {
            LayerMask newMask = mask;
            newMask |= (1 << layer);
            return newMask;
        }
        public static bool[] LayerMaskHasLayers(LayerMask layerMask)
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

        public static Color ColorLerp(Color a, Color b, float r)
        {
            float _r = Mathf.Clamp01(r);
            return new Color(Mathf.Lerp(a.r, b.r, _r), Mathf.Lerp(a.g, b.g, _r), Mathf.Lerp(a.b, b.b, _r), Mathf.Lerp(a.a, b.a, _r));
        }

        public static bool ProjectIsSRP => GraphicsSettings.renderPipelineAsset != null;

        public static string[] EnumItemNames<T>()
        {
            string[] result = Enum.GetNames(typeof(T));

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = result[i].Replace("_", "");
            }

            return result;
        }

        public static string GetMonoScriptPathFor(Type type)
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

        public static string AssetFolder
        {
            get
            {
                string scriptPath = Util.GetMonoScriptPathFor(typeof(MassSpawner));
                return scriptPath.Replace("/Scripts/MassSpawner.cs", "");
            }
        }

        public static bool AssetExists(string path)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(path) != null;
        }

        public static int PlacementPointCount(int resolution, int everyN)
        {
            int result = 0;
            for (int x = 0; x < resolution; x += everyN)
            {
                for (int y = 0; y < resolution; y += everyN)
                {
                    result++;
                }
            }
            return result;
        }
    }
}
