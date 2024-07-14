using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnityTerrainToTfrags))]
public class UnityTerrainToTfragsEditor : Editor
{
    private void OnEnable()
    {
        
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (target is UnityTerrainToTfrags tfragGen)
        {
            if (tfragGen.GetComponent<Terrain>() is Terrain terrain && terrain)
            {
                if (terrain.terrainData.terrainLayers.Length > 4)
                {
                    EditorGUILayout.HelpBox("Each terrain may only have up to 4 textures. Please consider adding an additional terrain if wish to use more.", MessageType.Error);
                }
            }

            if (tfragGen.m_RenderGenerated != EditorGUILayout.Toggle("Render Generated", tfragGen.m_RenderGenerated))
            {
                tfragGen.m_RenderGenerated = !tfragGen.m_RenderGenerated;
                tfragGen.SetVisible(tfragGen.m_RenderGenerated);
            }

        }

        if (GUILayout.Button("Generate"))
        {
            (target as UnityTerrainToTfrags).Generate();
        }
    }

}
