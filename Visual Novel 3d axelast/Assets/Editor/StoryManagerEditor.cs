using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoryManager))]
public class StoryManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StoryManager manager = (StoryManager)target;
        if (GUILayout.Button("Activeer Next Chapter (Debug)"))
        {
            manager.ActivateNextChapterManual();
        }
    }
}
