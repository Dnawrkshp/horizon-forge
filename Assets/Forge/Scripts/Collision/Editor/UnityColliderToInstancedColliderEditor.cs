using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityColliderToInstancedCollider)), CanEditMultipleObjects]
public class UnityColliderToInstancedColliderEditor : Editor
{
    private SerializedProperty m_ColliderProperty;
    private SerializedProperty m_MaterialIdProperty;
    private SerializedProperty m_NormalsProperty;
    private SerializedProperty m_RecalculateNormalsFactorProperty;
    private SerializedProperty m_ResolutionProperty;
    private SerializedProperty m_RenderProperty;

    private void OnEnable()
    {
        m_ColliderProperty = serializedObject.FindProperty("m_Collider");
        m_MaterialIdProperty = serializedObject.FindProperty("m_MaterialId");
        m_NormalsProperty = serializedObject.FindProperty("m_Normals");
        m_RecalculateNormalsFactorProperty = serializedObject.FindProperty("m_RecalculateNormalsFactor");
        m_ResolutionProperty = serializedObject.FindProperty("m_Resolution");
        m_RenderProperty = serializedObject.FindProperty("m_Render");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // disabled
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_ColliderProperty);
        EditorGUI.EndDisabledGroup();

        // misc properties
        EditorGUILayout.PropertyField(m_MaterialIdProperty);
        EditorGUILayout.PropertyField(m_NormalsProperty);
        if (m_NormalsProperty.enumValueIndex == (int)CollisionRenderHandleNormalMode.RecalculateOutside
            || m_NormalsProperty.enumValueIndex == (int)CollisionRenderHandleNormalMode.RecalculateInside)
            EditorGUILayout.PropertyField(m_RecalculateNormalsFactorProperty);

        if (m_ColliderProperty.objectReferenceValue is SphereCollider)
            EditorGUILayout.PropertyField(m_ResolutionProperty);

        EditorGUILayout.PropertyField(m_RenderProperty);

        if (targets.Select(x => x as UnityColliderToInstancedCollider).Any(x => x.HasInstancedCollider() && !x.GetInstancedCollider()?.AssetInstance))
        {
            EditorGUILayout.HelpBox("One or more instances have no configured collider. No collision will be built for those instances.", MessageType.Warning);
        }

        var changed = serializedObject.hasModifiedProperties;
        serializedObject.ApplyModifiedProperties();

        // refresh
        GUILayout.Space(20);
        if (GUILayout.Button("Refresh Collider"))
            changed = true;

        // update asset on changes
        if (changed)
        {
            foreach (var target in targets)
                if (target is UnityColliderToInstancedCollider collider)
                    collider.UpdateAsset();
        }
    }
}
