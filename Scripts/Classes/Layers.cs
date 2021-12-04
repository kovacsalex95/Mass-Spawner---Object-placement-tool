using System;
using System.Collections.Generic;
using UnityEngine;

namespace lxkvcs
{
    [System.Serializable]
    public enum PlaceType
    {
        Instantiate = 0,
        Prefabs = 1
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

        public PlaceObject()
        {
            _position = new RandomVector3();
            _rotation = new RandomVector3();
            _scale = new RandomVector3();
            colors = new MaterialColoring[0];
        }
        public PlaceObject(PlaceObject from)
        {
            prefab = from.prefab;
            _position = new RandomVector3(from._position);
            _rotation = new RandomVector3(from._rotation);
            _scale = new RandomVector3(from._scale);
            transform = from.transform;
            coloring = from.coloring;
            colors = new MaterialColoring[from.colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new MaterialColoring(from.colors[i]);
            }
        }

        public void AddColoring()
        {
            MaterialColoring coloring = new MaterialColoring(Util.ProjectIsSRP);
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

        public CollisionRule()
        {

        }

        public CollisionRule(CollisionRule from)
        {
            mode = from.mode;
            layerMask = from.layerMask;
            radius = from.radius;
            stop = from.stop;
        }
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

        public PlaceType placing = PlaceType.Instantiate;

        public RandomVector3 _position;
        public RandomVector3 _rotation;
        public RandomVector3 _scale;

        public RenderTexture objectPlaces = null;
        public PlacementPoint[] objectPoints = null;
        public int everyN = 10;
        public float angleOffset = 5f;
        public float organicity = 0.5f;
        public float minDistance = 0.5f;

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
            placing = target.placing;
            objectPlaces = target.objectPlaces;
            everyN = target.everyN;
            angleOffset = target.angleOffset;
            organicity = target.organicity;
            minDistance = target.minDistance;

            _position = new RandomVector3(target._position);
            _rotation = new RandomVector3(target._rotation);
            _scale = new RandomVector3(target._scale);

            prefabs = new PlaceObject[target.prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++)
                prefabs[i] = new PlaceObject(target.prefabs[i]);

            collisionRules = new CollisionRule[target.collisionRules.Length];
            for (int i=0; i < collisionRules.Length; i++)
                collisionRules[i] = new CollisionRule(target.collisionRules[i]);
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

        public void CheckArrays()
        {
            if (prefabs == null)
                prefabs = new PlaceObject[0];
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
    }

    public struct PlacementPoint
    {
        public Vector2 _point;
        public int _valid;
        public bool valid
        {
            get
            {
                return _valid == 1;
            }
            set
            {
                _valid = value ? 1 : 0;
            }
        }

        public static int stride => sizeof(float) * 2 + sizeof(int);

        public PlacementPoint(float x, float y)
        {
            this._point = new Vector2(x, y);
            _valid = 1;
            valid = true;
        }
        public PlacementPoint(float x, float y, bool isValid)
        {
            this._point = new Vector2(x, y);
            _valid = 1;
            valid = isValid;
        }
        public static PlacementPoint Invalid => new PlacementPoint(0, 0, false);

        public int x
        {
            get
            {
                int result = Mathf.FloorToInt(_point.x);

                if (result < 0)
                    result = 0;

                return result;
            }
        }
        public int y
        {
            get
            {
                int result = Mathf.FloorToInt(_point.y);

                if (result < 0)
                    result = 0;

                return result;
            }
        }
    }
}
