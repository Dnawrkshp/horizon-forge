using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Forge", true)]
public class ForgeSceneOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement() { name = "My Toolbar Root" };

        {
            var renderInstancedCollidersToggle = new Toggle("Render All Instanced Colliders");
            renderInstancedCollidersToggle.RegisterValueChangedCallback<bool>(e => { UpdateForceRenderAllCollisionHandles(e.newValue); });
            renderInstancedCollidersToggle.SetValueWithoutNotify(CollisionRenderHandle.ForceRenderAllCollisionHandles);
            root.Add(renderInstancedCollidersToggle);
        }

        {
            var renderMapRenderLayersToggle = new Toggle("Render Map Render Layers");
            renderMapRenderLayersToggle.RegisterValueChangedCallback<bool>(e => { UpdateForceRenderAllMapRenderLayers(e.newValue); });
            renderMapRenderLayersToggle.SetValueWithoutNotify(MapRenderLayer.ForceRender);
            root.Add(renderMapRenderLayersToggle);
        }

        return root;
    }

    private void UpdateForceRenderAllCollisionHandles(bool force)
    {
        CollisionRenderHandle.ForceRenderAllCollisionHandles = force;

        var handles = GameObject.FindObjectsOfType<GameObject>(includeInactive: false).SelectMany(x => x.GetComponentsInChildren<IInstancedCollider>()).Where(x => x != null && x.HasInstancedCollider()).Select(x => x.GetInstancedCollider());
        foreach (var handle in handles)
        {
            handle.UpdateMaterials();
        }
    }

    private void UpdateForceRenderAllMapRenderLayers(bool force)
    {
        MapRenderLayer.ForceRender = force;

        if (force)
            Shader.EnableKeyword("_MAPRENDER_SCENEVIEW");
        else
            Shader.DisableKeyword("_MAPRENDER_SCENEVIEW");

        var mapRender = GameObject.FindObjectOfType<MapRender>();
        if (mapRender) mapRender.UpdateCamera();

        var handles = HierarchicalSorting.Sort(GameObject.FindObjectsOfType<MapRenderLayer>(includeInactive: false));
        foreach (var handle in handles)
        {
            if (force)
                handle.OnPreMapRender();
            else
                handle.OnPostMapRender();
        }
    }


}
