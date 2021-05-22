using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace lxkvcs
{
    public class EditorUI
    {
        public static bool ToggleButton(string title, bool source, string _default = "Button", int width = 0, int height = 24)
        {
            if (width > 0)
                return GUILayout.Toggle(source, StringDefault(title, _default), "Button", GUILayout.Height(height), GUILayout.Width(width));
            else if (width < 0)
                return GUILayout.Toggle(source, StringDefault(title, _default), "Button", GUILayout.Height(height), GUILayout.MaxWidth(-width));
            else
                return GUILayout.Toggle(source, StringDefault(title, _default), "Button", GUILayout.Height(height));
        }

        public static string StringMaxLength(string source, int length = 10)
        {
            return source.Length <= length ? source : source.Substring(0, Mathf.Max(1, length - 3)) + "...";
        }

        public static string StringDefault(string source, string _default)
        {
            return source != string.Empty ? source : _default;
        }

        public static LayerMask LayerMaskField(string label, LayerMask layerMask)
        {
            List<int> layerNumbers = new List<int>();

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
    }
}
