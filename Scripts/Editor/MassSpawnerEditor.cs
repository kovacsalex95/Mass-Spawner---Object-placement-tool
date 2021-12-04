
// Created by Alex Kovács
// 2021
//
// Support: kovacsalex95@gmail.com

using UnityEditor;
using UnityEngine;

namespace lxkvcs
{
    [CustomEditor(typeof(MassSpawner))]
    public class MassSpawnerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Mass Spawner window", GUILayout.Height(60)))
                MassSpawnerEditorWindow.Init();
        }
    }
}
