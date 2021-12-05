using UnityEngine;
using lxkvcs;

[CreateAssetMenu(fileName = "mass-spawner-project", menuName = "Mass Spawner Project", order = 1)]
public class MassSpawnerProject : ScriptableObject
{
    // Resolution
    [SerializeField, HideInInspector]
    public Resolutions heightmapResolution = Resolutions._1024x1024;

    // World boundaries
    [SerializeField]
    public Vector2 terrainOffset = new Vector2(500, 500);
    [SerializeField]
    public Vector2 terrainSize = new Vector2(1000, 1000);
    [SerializeField]
    public float terrainTop = 500f;
    [SerializeField]
    public float terrainBottom = 0;

    // Raycast masks
    [SerializeField]
    public LayerMask includeMask;
    [SerializeField]
    public LayerMask excludeMask;

    // Object and Color groups
    [SerializeField]
    public ObjectLayer[] objectLayers = null;
    [SerializeField]
    public ColorGroup[] colorGroups = null;
}