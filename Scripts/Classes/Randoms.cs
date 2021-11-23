using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


namespace lxkvcs
{
    [System.Serializable]
    public class RandomBetween
    {
        public float min = 0f;
        public float max = 1f;
        public bool random = false;

        public float value => random && min != max ? Random.Range(min, max) : min;
        public float avarage => random && min != max ? (min + max) / 2 : min;

        public RandomBetween()
        {

        }
        public RandomBetween(RandomBetween from)
        {
            min = from.min;
            max = from.max;
            random = from.random;
        }

        public static RandomBetween InputGUI(RandomBetween source, int mode = 0, bool disabled = false)
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
        public RandomVector3(RandomVector3 from)
        {
            x = new RandomBetween(from.x);
            y = new RandomBetween(from.y);
            z = new RandomBetween(from.z);
            _override = from._override;
            xMode = from.xMode;
            yMode = from.yMode;
            zMode = from.zMode;

            min = from.min;
            max = from.max;
            clamp = from.clamp;
            whole = from.whole;
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

        public static RandomVector3 InputGUI(RandomVector3 value, bool canOverride = false)
        {
            return InputGUI(string.Empty, value);
        }
        public static RandomVector3 InputGUI(RandomVector3 value, string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z")
        {
            return InputGUI(string.Empty, value, false, xAxisName, yAxisName, zAxisName);
        }
        public static RandomVector3 InputGUI(string name, RandomVector3 value, bool canOverride = false, string xAxisName = "X", string yAxisName = "Y", string zAxisName = "Z")
        {
            // Random vector settings (transform settings, coloring etc.)
            RandomVector3 result = value;

            bool hasTitle = name != string.Empty || canOverride;

            if (hasTitle)
                GUILayout.BeginHorizontal();

            if (name != string.Empty)
                GUILayout.Label(name);

            if (canOverride)
                result._override = GUILayout.Toggle(result._override, result._override ? "Custom" : "Inherit", "Button", GUILayout.Width(100));
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
                    result.x = RandomBetween.InputGUI(result.x, (int)result.xMode);
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
                    RandomBetween.InputGUI(result.xMode == RandomVectorModeX.CopyY ? result.y : result.z, result.xMode == RandomVectorModeX.CopyY ? (int)result.yMode : (int)result.zMode);
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
                    result.y = RandomBetween.InputGUI(result.y, (int)result.yMode);
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
                    RandomBetween.InputGUI(result.yMode == RandomVectorModeY.CopyX ? result.x : result.z, result.yMode == RandomVectorModeY.CopyX ? (int)result.xMode : (int)result.zMode);
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
                    result.z = RandomBetween.InputGUI(result.z, (int)result.zMode);
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
                    RandomBetween.InputGUI(result.zMode == RandomVectorModeZ.CopyX ? result.x : result.y, result.zMode == RandomVectorModeZ.CopyX ? (int)result.xMode : (int)result.yMode);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            return result;
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
}
