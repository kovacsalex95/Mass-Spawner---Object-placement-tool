using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace lxkvcs
{
    [CustomEditor(typeof(MassSpawner))]
    public class MassSpawnerEditor : Editor
    {
        MassSpawner spawner = null;

        private void Awake()
        {
            spawner = target as MassSpawner;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Mass Spawner window", GUILayout.Height(60)))
                MassSpawnerEditorWindow.Init();
        }
    }
}
