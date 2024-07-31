using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
public class MapRender : MonoBehaviour
{
    const int MAP_RENDER_VERSION = 1;

    public int RenderScale = 512;
    public Color BackgroundColor = new Color32(0x66, 0x66, 0x66, 0);
    [SerializeField, HideInInspector] public string SavePath = null;
    [SerializeField, HideInInspector] private int _version = 0;

    private void OnValidate()
    {
        Upgrade();
        UpdateCamera();
    }

    public void UpdateCamera()
    {
        // setup camera
        var camera = GetComponent<Camera>();
        camera.backgroundColor = BackgroundColor;
        camera.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        camera.orthographicSize = transform.localScale.z / 2f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = transform.localScale.y;

        Shader.SetGlobalVector("_MapRenderCameraZRange", new Vector2(this.transform.position.y - this.transform.localScale.y, this.transform.position.y));
    }

    #region Versioning

    public void InitializeVersion()
    {
        _version = MAP_RENDER_VERSION;
    }

    private void Upgrade()
    {
        // wait for import to finish before upgrading
        if (LevelImporterWindow.IsImporting) return;

        // upgrade
        if (_version < MAP_RENDER_VERSION)
        {
            while (_version < MAP_RENDER_VERSION)
            {
                RunMigration(_version + 1);
                ++_version;
            }

            Debug.Log($"Map Render upgraded to v{_version}");
        }
    }

    private void RunMigration(int version)
    {
        switch (version)
        {
            case 1: // USE TIE/TFRAG/SHRUB layers
                {
                    var camera = GetComponent<Camera>();
                    if (camera)
                    {
                        camera.cullingMask = LayerMask.GetMask("TIE", "TFRAG", "SHRUB", "MAPRENDER");
                    }
                    break;
                }
        }
    }

    #endregion
}
