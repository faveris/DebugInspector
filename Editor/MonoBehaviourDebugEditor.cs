using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MonoBehaviour), true), CanEditMultipleObjects]
public class MonoBehaviourDebugEditor : Editor
{
    //
    // Events
    //

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DebugInspectorLayout.DrawDebugView(target as MonoBehaviour);
    }
}
