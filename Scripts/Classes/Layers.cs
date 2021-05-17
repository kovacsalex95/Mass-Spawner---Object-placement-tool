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

        public PlaceType placing = PlaceType.Instantiate;

        public bool _transform = false;
        public RandomVector3 _position;
        public RandomVector3 _rotation;
        public RandomVector3 _scale;

        public RenderTexture objectPlaces = null;
        public PlacementPoint[] objectPoints = null;
        public int everyN = 10;
        public float angleOffset = 5f;
        public float organicity = 0.5f;
        public float minDistance = 0.5f;

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
