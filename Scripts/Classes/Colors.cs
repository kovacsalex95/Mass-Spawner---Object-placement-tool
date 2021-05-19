using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace lxkvcs
{
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
    public class MaterialColoring
    {
        public Material material = null;
        public string propertyName = "_BaseColor";
        public int colorGroup = -1;

        public MaterialColoring(bool isSrp)
        {
            propertyName = isSrp ? "_BaseColor" : "_Color";
        }

        public MaterialColoring(MaterialColoring from)
        {
            material = from.material;
            propertyName = from.propertyName;
            colorGroup = from.colorGroup;
        }
    }
}
