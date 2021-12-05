using UnityEngine;
using lxkvcs;

[CreateAssetMenu(fileName = "mass-spawner-project", menuName = "Mass Spawner Project", order = 1)]
public class MassSpawnerProject : ScriptableObject
{
    // Resolution
    [SerializeField, HideInInspector]
    public Resolutions heightmapResolution = Resolutions._1024x1024;

    // World boundaries
    [SerializeField, HideInInspector]
    public Vector2 terrainOffset = new Vector2(500, 500);
    [SerializeField, HideInInspector]
    public Vector2 terrainSize = new Vector2(1000, 1000);
    [SerializeField, HideInInspector]
    public float terrainTop = 500f;
    [SerializeField, HideInInspector]
    public float terrainBottom = 0;

    // Raycast masks
    [SerializeField, HideInInspector]
    public LayerMask includeMask;
    [SerializeField, HideInInspector]
    public LayerMask excludeMask;

    // Object and Color groups
    [SerializeField, HideInInspector]
    public ObjectLayer[] objectLayers = null;
    [SerializeField, HideInInspector]
    public ColorGroup[] colorGroups = null;
}