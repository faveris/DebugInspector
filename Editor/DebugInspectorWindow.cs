using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DebugInspectorWindow : EditorWindow
{
    //
    // Fields
    //

    private Vector2 m_scrollPos;

    //
    // Interface
    //

    [MenuItem("Window/Debug Inspector")]
    public static void GetWindow()
    {
        EditorWindow.GetWindow<DebugInspectorWindow>("Debug Inspector");
    }

    //
    // Events
    //

    public void OnSelectionChange()
    {
        Repaint();
    }

    public void OnGUI()
    {
        GameObject target = Selection.activeGameObject;
        if (target == null)
        {
            return;
        }

        EditorGUILayout.InspectorTitlebar(true, target);
       
        m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

        Component[] components = target.GetComponents<Component>();
        foreach (Component component in components)
        {
            DebugInspectorLayout.ObjectField(component.GetType().Name, component, AssetPreview.GetMiniThumbnail(component));

            EditorGUILayout.Separator();
        }

        EditorGUILayout.EndScrollView();
    }
}
