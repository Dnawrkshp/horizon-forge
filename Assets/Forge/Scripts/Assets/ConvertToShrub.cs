using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ConvertToShrub : MonoBehaviour
{
    public int GroupId;
    public float RenderDistance = 64;
    [ColorUsage(false)] public Color Tint = Color.white * 0.5f;
    [Tooltip("If true and Export Shrubs is enabled, the DZO exporter will automatically include this object.")] public bool DZOExportWithShrubs = true;

    public IEnumerable<ConvertToShrubChild> GetChildren()
    {
        if (!Validate()) return null;

        var children = new List<ConvertToShrubChild>();
        var foundPrefabs = new HashSet<GameObject>();
        var renderers = this.gameObject.GetComponentsInChildren<Renderer>();
        

        // iterate hierarchy, group 
        foreach (var renderer in renderers)
        {
            var t = renderer.transform;

            // default prefab to root gameobject instance
            var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(this.gameObject);
            if (!prefab) prefab = this.gameObject;

            // move up hierarchy searching for prefab parent
            while (t != null)
            {
                prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                if (prefab)
                    break;

                t = t.parent;
            }

            // 
            var root = t;
            if (!root)
                root = this.transform;

            // add child
            children.Add(new ConvertToShrubChild() { PrefabOrObject = prefab, InstanceRootTransform = root });
        }

        return children;
    }

    public bool GetGeometry(out GameObject root)
    {
        root = null;
        if (!Validate())
            return false;

        // prefab
        var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(this.gameObject);
        if (prefab)
        {
            root = prefab;
            return true;
        }

        root = this.gameObject;
        return true;
    }

    public bool Validate()
    {
        var meshFilters = this.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Any(m => !m.gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy) && m.sharedMesh && m.sharedMesh.isReadable == false))
            return false;

        return true;
    }

    public string GetAssetHash()
    {
        return null;
    }
}
