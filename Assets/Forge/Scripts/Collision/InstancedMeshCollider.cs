using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter)), DisallowMultipleComponent]
public class InstancedMeshCollider : RenderSelectionBase, IAsset, IInstancedCollider
{
    public MeshFilter m_MeshFilter;
    public string m_MaterialId = "2f";
    public CollisionRenderHandleNormalMode m_Normals;
    [Range(-2f, 2f)]public float m_RecalculateNormalsFactor = 1;
    public bool m_Render = true;

    public GameObject GameObject => this ? this.gameObject : null;
    public bool IsHidden => collisionRenderHandle?.IsHidden ?? false;
    public CollisionRenderHandle GetInstancedCollider() => collisionRenderHandle;
    public bool HasInstancedCollider() => this.isActiveAndEnabled;

    private CollisionRenderHandle collisionRenderHandle = new CollisionRenderHandle(null);


    private void OnEnable()
    {
        m_MeshFilter = GetComponent<MeshFilter>();

        // check for conflicting components
        if (GetComponent<Tie>() || GetComponent<Shrub>())
        {
            Dispatcher.RunOnMainThread(() =>
            {
                if (!this || !this.gameObject) return;

                if (EditorUtility.DisplayDialog($"{this.gameObject.name} cannot have an InstancedMeshCollider", "Tie and Shrubs may not have InstancedMeshColliders. Please use the built in InstancedCollider toggle.\n\nThe InstancedMeshCollider will be removed.", "Okay"))
                {
                    DestroyImmediate(this);
                    if (m_MeshFilter) DestroyImmediate(m_MeshFilter);
                    return;
                }
            });
        }

        AssetUpdater.RegisterAsset(this);
        UpdateAsset();
        collisionRenderHandle?.UpdateMaterials();
    }

    private void OnDisable()
    {
        AssetUpdater.UnregisterAsset(this);
        collisionRenderHandle?.DestroyAsset();
    }

    private void OnValidate()
    {
        collisionRenderHandle?.UpdateMaterials();
    }

    public void UpdateAsset()
    {
        // configure collider
        if (HasInstancedCollider())
        {
            int instancedColliderMaterialId = int.TryParse(m_MaterialId, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) ? id : 0x2f;
            collisionRenderHandle.IsHidden = SceneVisibilityManager.instance.IsHidden(this.gameObject) || !m_Render;
            collisionRenderHandle.IsSelected = Selection.activeGameObject == this.gameObject || Selection.gameObjects.Contains(this.gameObject);
            collisionRenderHandle.IsPicking = !SceneVisibilityManager.instance.IsPickingDisabled(this.gameObject);
            collisionRenderHandle.Normals = m_Normals;
            collisionRenderHandle.RecalculateNormalsFactor = m_RecalculateNormalsFactor;
            collisionRenderHandle.Update(this.gameObject, m_MeshFilter.sharedMesh, instancedColliderMaterialId);
        }
        else
        {
            collisionRenderHandle.Update(this.gameObject, null, null);
        }
    }

    #region Asset Selection

    public  void OnPreBake(Color32 uidColor)
    {
        if (collisionRenderHandle == null) UpdateAsset();
        collisionRenderHandle.IdColor = uidColor;
        UpdateAsset();
    }

    public void OnPostBake()
    {

    }

    #endregion
}
