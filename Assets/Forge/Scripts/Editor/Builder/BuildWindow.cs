using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


public class BuildWindow : EditorWindow
{
    [Flags]
    enum ForgeBuildTargets
    {
        DL_NTSC = 1,
        UYA_NTSC = 2,
        RAC3_PAL = 4
    }

    static readonly Dictionary<ForgeBuildTargets, int> BuildTargetRacVersions = new Dictionary<ForgeBuildTargets, int>()
    {
        { ForgeBuildTargets.DL_NTSC, RCVER.DL },
        { ForgeBuildTargets.UYA_NTSC, RCVER.UYA },
        { ForgeBuildTargets.RAC3_PAL, RCVER.UYA },
    };

    static readonly Dictionary<ForgeBuildTargets, GameRegion> BuildTargetGameRegions = new Dictionary<ForgeBuildTargets, GameRegion>()
    {
        { ForgeBuildTargets.DL_NTSC, GameRegion.NTSC },
        { ForgeBuildTargets.UYA_NTSC, GameRegion.NTSC },
        { ForgeBuildTargets.RAC3_PAL, GameRegion.PAL },
    };

    // rebuild
    Toggle toggleRebuildCollision;
    Toggle toggleRebuildTfrags;
    Toggle toggleRebuildTies;
    Toggle toggleRebuildShrubs;
    Toggle toggleRebuildMobys;
    Toggle toggleRebuildCuboidsSplinesAreas;
    //Toggle toggleRebuildCode;
    Toggle toggleRebuildLighting;
    Toggle toggleRebuildDZO;

    // pack
    Toggle togglePackAssets;
    Toggle togglePackWorldInstances;
    Toggle togglePackOcclusion;
    Toggle togglePackLevel;
    Toggle togglePackGameplay;
    Toggle togglePackSound;

    // games
    EnumFlagsField flagsGames;

    Toggle togglePatch;
    Button buildButton;

    [MenuItem("Forge/Builder/Open Build Window")]
    public static void CreateNewWindow()
    {
        var wnd = GetWindow<BuildWindow>();
        wnd.titleContent = new GUIContent("Builder");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        root.Clear();

        // VisualElements objects can contain other VisualElement following a tree hierarchy
        var titleLabel = new Label("Build Level");
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        titleLabel.style.fontSize = 18;
        root.BuildPadding();
        root.Add(titleLabel);
        root.BuildPadding();

        CreateGUI_Rebuild(root);
        root.BuildPadding();
        CreateGUI_Pack(root);
        root.BuildPadding();

        root.BuildPadding();
        CreateGUI_Build(root);
    }

    void CreateGUI_Rebuild(VisualElement root)
    {
        root.BuildLabel("Rebuild");
        root.BuildPadding();

        //toggleRebuildCode = toggleRebuildCode ?? new Toggle("Code") { value = true };
        toggleRebuildCollision = toggleRebuildCollision ?? new Toggle("Collision") { value = true };
        toggleRebuildCuboidsSplinesAreas = toggleRebuildCuboidsSplinesAreas ?? new Toggle("Cuboids/Splines/Areas") { value = true };
        toggleRebuildDZO = toggleRebuildDZO ?? new Toggle("DZO") { value = true };
        toggleRebuildMobys = toggleRebuildMobys ?? new Toggle("Mobys") { value = true };
        toggleRebuildShrubs = toggleRebuildShrubs ?? new Toggle("Shrubs") { value = true };
        toggleRebuildTfrags = toggleRebuildTfrags ?? new Toggle("Tfrags") { value = true };
        toggleRebuildTies = toggleRebuildTies ?? new Toggle("Ties") { value = true };
        toggleRebuildLighting = toggleRebuildLighting ?? new Toggle("World Lighting") { value = true };

        //root.Add(toggleRebuildCode);
        root.Add(toggleRebuildCollision);
        root.Add(toggleRebuildCuboidsSplinesAreas);
        root.Add(toggleRebuildDZO);
        root.Add(toggleRebuildMobys);
        root.Add(toggleRebuildShrubs);
        root.Add(toggleRebuildTfrags);
        root.Add(toggleRebuildTies);
        root.Add(toggleRebuildLighting);
    }

    void CreateGUI_Pack(VisualElement root)
    {
        root.BuildLabel("Pack");
        root.BuildPadding();

        togglePackAssets = togglePackLevel ?? new Toggle("Assets") { value = true };
        togglePackWorldInstances = togglePackLevel ?? new Toggle("World Instances") { value = true };
        togglePackOcclusion = togglePackLevel ?? new Toggle("Occlusion") { value = true };
        togglePackLevel = togglePackLevel ?? new Toggle("Level WAD") { value = true };
        togglePackGameplay = togglePackGameplay ?? new Toggle("Gameplay WAD") { value = true };
        togglePackSound = togglePackSound ?? new Toggle("Sound WAD") { value = true };

        root.Add(togglePackAssets);
        root.Add(togglePackWorldInstances);
        root.Add(togglePackOcclusion);
        root.Add(togglePackLevel);
        root.Add(togglePackGameplay);
        root.Add(togglePackSound);
    }

    void CreateGUI_Build(VisualElement root)
    {
        flagsGames = new EnumFlagsField("Game(s)", ForgeBuildTargets.DL_NTSC | ForgeBuildTargets.UYA_NTSC | ForgeBuildTargets.RAC3_PAL);
        root.Add(flagsGames);

        togglePatch = togglePatch ?? new Toggle("Patch ISO(s)") { value = true };
        root.Add(togglePatch);

        buildButton = new Button(OnBuild);
        buildButton.text = "Build";
        root.Add(buildButton);
    }

    async void OnBuild()
    {
        if (await RebuildLevel(EditorSceneManager.GetActiveScene(), (ForgeBuildTargets)flagsGames.value))
        {
            ForgeBuilder.CopyToBuildFolders(EditorSceneManager.GetActiveScene());
        }
    }

    async Task<bool> RebuildLevel(UnityEngine.SceneManagement.Scene scene, ForgeBuildTargets buildTargets)
    {
        if (scene == null) return false;

        var mapConfig = GameObject.FindObjectOfType<MapConfig>();
        if (!mapConfig)
        {
            EditorUtility.DisplayDialog("Cannot build", $"Scene does not have a map config", "Ok");
            return false;
        }

        var buildBothRC3Regions = buildTargets.HasFlag(ForgeBuildTargets.UYA_NTSC) && buildTargets.HasFlag(ForgeBuildTargets.RAC3_PAL);

        // build dzo first
        if (toggleRebuildDZO.value) ForgeBuilder.BuildDZOFiles(scene);

        // rebuild
        foreach (ForgeBuildTargets buildTarget in Enum.GetValues(typeof(ForgeBuildTargets)))
        {
            if (!buildTargets.HasFlag(buildTarget)) continue;

            var racVersion = BuildTargetRacVersions[buildTarget];
            var region = BuildTargetGameRegions[buildTarget];
            var baseMap = racVersion == RCVER.DL ? (int)mapConfig.DLBaseMap : (int)mapConfig.UYABaseMap;

            var resourcesFolder = FolderNames.GetMapFolder(scene.name);
            var binFolder = FolderNames.GetMapBinFolder(scene.name, racVersion);

            // skip if not configured
            if (racVersion == RCVER.DL && !mapConfig.HasDeadlockedBaseMap()) continue;
            if (racVersion == RCVER.UYA && !mapConfig.HasUYABaseMap()) continue;

            // validate resources folder
            if (!Directory.Exists(resourcesFolder))
            {
                EditorUtility.DisplayDialog($"Cannot build (rc{racVersion} {region})", $"Scene does not have matching resources folder \"{scene.name}\"", "Ok");
                return false;
            }

            // validate level folder
            if (!Directory.Exists(binFolder))
            {
                EditorUtility.DisplayDialog($"Cannot build (rc{racVersion} {region})", $"Scene does not have matching level folder \"{scene.name}\"", "Ok");
                return false;
            }

            if (!scene.isLoaded || !mapConfig)
                return false;

            try
            {
                var ctx = new ForgeBuilder.RebuildContext()
                {
                    MapSceneName = scene.name,
                    RacVersion = racVersion,
                    Region = region
                };

                // run generators
                UnityHelper.RunGeneratorsPreBake(BakeType.BUILD);

                // PAL is always built after NTSC
                // PAL only needs to be rebuilt with new PAL code segment
                // unless we didn't build the NTSC version previously, then rebuild
                if (region == GameRegion.PAL && buildBothRC3Regions)
                {
                    // we need to rebuild code if PAL otherwise we 
                    ForgeBuilder.RebuildCode(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                }
                else
                {
                    if (toggleRebuildCollision.value) await ForgeBuilder.RebuildCollision(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildTfrags.value) ForgeBuilder.RebuildTfrags(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildTies.value) ForgeBuilder.RebuildTies(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildTies.value) ForgeBuilder.RebuildTieInstances(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildShrubs.value) await ForgeBuilder.RebuildShrubs(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildShrubs.value) ForgeBuilder.RebuildShrubInstances(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildMobys.value) ForgeBuilder.RebuildMobys(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildMobys.value) ForgeBuilder.RebuildMobyInstances(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildCuboidsSplinesAreas.value) ForgeBuilder.RebuildCuboids(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildCuboidsSplinesAreas.value) ForgeBuilder.RebuildSplines(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildCuboidsSplinesAreas.value) ForgeBuilder.RebuildAreas(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                    if (toggleRebuildLighting.value) ForgeBuilder.RebuildWorldLighting(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;

                    // always rebuild code
                    // to account for NTSC/PAL using different code segments
                    // we must always keep the build folder's code up-to-date
                    ForgeBuilder.RebuildCode(ctx, resourcesFolder, binFolder); if (ctx.Cancel) return false;
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayProgressBar($"Rebuilding Level (rc{racVersion} {region})", "Packing", 0);

                PackerHelper.PACKER_PACK_OPS packOps = PackerHelper.PACKER_PACK_OPS.PACK_CODE;
                if (togglePackOcclusion.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_OCCLUSION;
                if (togglePackWorldInstances.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_WORLD_INSTANCES;
                if (togglePackAssets.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_ASSETS;
                if (togglePackGameplay.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_GAMEPLAY;
                if (togglePackLevel.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_LEVEL_WAD;
                if (togglePackSound.value) packOps |= PackerHelper.PACKER_PACK_OPS.PACK_SOUND_WAD;

                var result = PackerHelper.Pack(binFolder, baseMap, racVersion, packOps, (p) => EditorUtility.DisplayProgressBar($"Rebuilding Level (rc{racVersion} {region})", "Packing", p));
                if (result != PackerHelper.PACKER_STATUS_CODES.SUCCESS)
                {
                    Debug.LogError($"Pack returned {result}");
                    return false;
                }

                ForgeBuilder.RebuildMapFiles(ctx, resourcesFolder, binFolder);
                Debug.Log($"Rebuild (rc{racVersion} {region}) complete");

                if (togglePatch.value) PatchLevel(scene, racVersion, region);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return false;
            }
            finally
            {
                // cleanup generators
                UnityHelper.RunGeneratorsPostBake(BakeType.BUILD);

                EditorUtility.ClearProgressBar();
            }
        }

        return true;
    }

    void PatchLevel(UnityEngine.SceneManagement.Scene scene, int racVersion, GameRegion region)
    {
        if (scene == null) return;

        var mapConfig = GameObject.FindObjectOfType<MapConfig>();
        if (!mapConfig)
        {
            EditorUtility.DisplayDialog("Cannot patch", $"Scene does not have a map config", "Ok");
            return;
        }

        var settings = ForgeSettings.Load();
        if (settings == null)
        {
            EditorUtility.DisplayDialog("Cannot patch", $"Missing ForgeSettings", "Ok");
            return;
        }

        var baseMap = racVersion == RCVER.DL ? (int)mapConfig.DLBaseMap : (int)mapConfig.UYABaseMap;
        var isoPath = settings.PathToOutputDeadlockedIso;
        var cleanIsoPath = settings.PathToCleanDeadlockedIso;
        var regionExt = region == GameRegion.NTSC ? "" : ".pal";

        if (racVersion == RCVER.UYA)
        {
            if (region == GameRegion.NTSC)
            {
                isoPath = settings.PathToOutputUyaNtscIso;
                cleanIsoPath = settings.PathToCleanUyaNtscIso;
            }
            else if (region == GameRegion.PAL)
            {
                isoPath = settings.PathToOutputUyaPalIso;
                cleanIsoPath = settings.PathToCleanUyaPalIso;
            }
        }

        if (racVersion == RCVER.DL && !mapConfig.HasDeadlockedBaseMap())
        {
            return; // skip
        }

        if (racVersion == RCVER.UYA && !mapConfig.HasUYABaseMap())
        {
            return; // skip
        }

        // validate level folder
        var binFolder = FolderNames.GetMapBinFolder(scene.name, racVersion);
        if (!Directory.Exists(binFolder))
        {
            EditorUtility.DisplayDialog("Cannot patch", $"Scene does not have matching level folder \"{scene.name}\"", "Ok");
            return;
        }

        if (String.IsNullOrEmpty(isoPath))
        {
            EditorUtility.DisplayDialog("Cannot patch", $"Missing ForgeSettings PathToOutputIso for rc{racVersion} {region}", "Ok");
            return;
        }

        if (String.IsNullOrEmpty(cleanIsoPath))
        {
            EditorUtility.DisplayDialog("Cannot patch", $"Missing ForgeSettings PathToCleanIso for rc{racVersion} {region}", "Ok");
            return;
        }

        // ensure modded iso exists
        if (!File.Exists(isoPath))
        {
            // clean iso also doesn't exist, exit
            if (!File.Exists(cleanIsoPath))
            {
                EditorUtility.DisplayDialog("Cannot patch", $"Clean iso \"{cleanIsoPath}\" does not exist.", "Okay");
                return;
            }

            // clean iso exists, give option to create modded iso by copy
            if (!EditorUtility.DisplayDialog("Cannot patch", $"Output iso \"{isoPath}\" does not exist.\n\nWould you like to create it?", "Create", "Cancel"))
                return;

            try
            {
                // copy
                EditorUtility.DisplayProgressBar("Copying iso", $"{cleanIsoPath} => {isoPath}...\n\nThis may take awhile...", 0.5f);
                File.Copy(cleanIsoPath, isoPath, true);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        if (PackerHelper.Patch(binFolder, isoPath, cleanIsoPath, baseMap, racVersion) != PackerHelper.PACKER_STATUS_CODES.SUCCESS)
            Debug.LogError($"Unable to patch {isoPath}. Please make sure PCSX2 is paused/closed.");
        else
            Debug.Log($"{isoPath} patched!");

        // patch minimap
        var mapPath = Path.Combine(FolderNames.GetMapBuildFolder(scene.name, racVersion), $"{mapConfig.MapFilename}{regionExt}.map");
        if (File.Exists(mapPath))
            PackerHelper.PatchMinimap(isoPath, cleanIsoPath, mapPath, baseMap, racVersion);

        // patch transition
        var bgPath = Path.Combine(FolderNames.GetMapBuildFolder(scene.name, racVersion), $"{mapConfig.MapFilename}{regionExt}.bg");
        if (File.Exists(bgPath))
            PackerHelper.PatchTransitionBackground(isoPath, cleanIsoPath, bgPath, baseMap, racVersion);
    }

    static bool CancelProgressBar(ref bool cancel, string title, string info, float progress)
    {
        cancel |= EditorUtility.DisplayCancelableProgressBar(title, info, progress);
        //System.Threading.Thread.Sleep(1);
        return cancel;
    }

}
