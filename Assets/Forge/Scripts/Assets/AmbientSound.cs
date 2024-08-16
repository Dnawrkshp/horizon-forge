using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode, SelectionBase, AddComponentMenu("")]
public class AmbientSound : RenderSelectionBase
{
    const int AMBIENT_SOUND_VERSION = 0;

    public int AmbientSoundType = 0;
    public float Unknown_0C = 0;

    [SerializeField, HideInInspector] private int _version = 0;
    [SerializeField, HideInInspector] public byte[] PVars;

    private bool changed = true;
    private bool lastHidden = false;
    private bool lastPicking = false;
    private bool lastSelected = false;
    private GameObject assetInstance;

    public Renderer[] GetRenderers() => assetInstance?.GetComponentsInChildren<Renderer>();

    private void Start()
    {
        Update();
    }

    void Update()
    {
        var hidden = SceneVisibilityManager.instance.IsHidden(this.gameObject);
        if (hidden != lastHidden)
        {
            changed = true;
            lastHidden = hidden;
        }

        var picking = !SceneVisibilityManager.instance.IsPickingDisabled(this.gameObject);
        if (picking != lastPicking)
        {
            changed = true;
            lastPicking = picking;
        }

        var selected = IsSelected();
        if (selected != lastSelected)
        {
            changed = true;
            lastSelected = selected;
        }

        if (!assetInstance)
        {
            RefreshAsset();
        }

        if (changed) UpdateMaterials();
        UpdateTransform();
    }

    private bool IsSelected()
    {
        if (!Selection.activeGameObject) return false;
        if (Selection.activeGameObject == this.gameObject) return true;

        return false;
    }

    private void OnValidate()
    {
        Upgrade();
        UpdateTransform();
        UpdateMaterials();
    }

    private void UpdateTransform()
    {
        if (assetInstance)
        {
            assetInstance.transform.localScale = Vector3.one;
        }
    }

    private void UpdateMaterials()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        var mpb = new MaterialPropertyBlock();

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);
            mpb.SetInteger("_Selected", lastSelected ? 1 : 0);
            mpb.SetInteger("_Faded2", lastHidden ? 1 : 0);
            mpb.SetInteger("_Picking", lastPicking ? 1 : 0);
            mpb.SetInteger("_WorldLightIndex", -1);
            renderer.SetPropertyBlock(mpb);

            renderer.allowOcclusionWhenDynamic = false;
        }

        changed = false;
    }

    public void RefreshAsset()
    {
        // destroy all children
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        // instantiate
        var prefab = UnityHelper.GetCuboidPrefab(CuboidMaskType.None);
        if (prefab)
        {
            GameObject go = null;
            try
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (go)
                {
                    assetInstance = go;
                    go.transform.SetParent(this.transform, false);

                    UnityHelper.RecurseHierarchy(go.transform, (t) =>
                    {
                        t.gameObject.layer = gameObject.layer;
                        t.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable | HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                    });

                    UpdateMaterials();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                if (go) DestroyImmediate(go);
            }
        }
    }

    #region Binary

    public void Read(BinaryReader reader)
    {
        AmbientSoundType = reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt32();
        Unknown_0C = reader.ReadSingle();

        var worldMatrix = Matrix4x4.identity;
        for (int i = 0; i < 16; ++i)
            worldMatrix[i] = reader.ReadSingle();

        for (int i = 0; i < 12; ++i)
            reader.ReadSingle();

        worldMatrix = worldMatrix.SwizzleXZY();
        worldMatrix.GetReflectionMatrix(out var pos, out var rot, out var scale, out var reflection);

        this.transform.position = pos;
        this.transform.rotation = rot * Quaternion.Euler(0, 90f, 0);
        this.transform.localScale = scale;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(AmbientSoundType);
        writer.Write(0);
        writer.Write(0);
        writer.Write(Unknown_0C);

        var trs = this.transform.localToWorldMatrix.SwizzleXZY();
        var inverse = this.transform.worldToLocalMatrix.SwizzleXZY();

        for (int i = 0; i < 16; ++i)
            writer.Write(trs[i]);
        for (int i = 0; i < 12; ++i)
            writer.Write(inverse[i]);

        var iEuler = -MathHelper.WrapEuler(this.transform.rotation * Quaternion.Euler(0, -90f, 0).eulerAngles).SwizzleXZY();
        writer.Write(iEuler.x * Mathf.Deg2Rad);
        writer.Write(iEuler.y * Mathf.Deg2Rad);
        writer.Write(iEuler.z * Mathf.Deg2Rad);
        writer.Write(0f);
    }

    #endregion

    #region Versioning

    public void InitializeVersion()
    {
        _version = AMBIENT_SOUND_VERSION;
    }

    private void Upgrade()
    {
        // wait for import to finish before upgrading
        if (LevelImporterWindow.IsImporting) return;

        // upgrade
        if (_version < AMBIENT_SOUND_VERSION)
        {
            while (_version < AMBIENT_SOUND_VERSION)
            {
                RunMigration(_version + 1);
                ++_version;
            }

            Debug.Log($"Ambient sound upgraded to v{_version}");
            UnityHelper.MarkActiveSceneDirty();
        }
    }

    private void RunMigration(int version)
    {
        switch (version)
        {
        }
    }

    #endregion

}
