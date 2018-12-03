using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL]
    public enum FullScreenDebugMode
    {
        None,

        // Lighting
        MinLightingFullScreenDebug,
        SSAO,
        ScreenSpaceReflections,
        ContactShadows,
        PreRefractionColorPyramid,
        DepthPyramid,
        FinalColorPyramid,
        MaxLightingFullScreenDebug,

        // Rendering
        MinRenderingFullScreenDebug,
        MotionVectors,
        NanTracker,
        MaxRenderingFullScreenDebug
    }

    public class DebugDisplaySettings : IDebugData
    {
        static string k_PanelDisplayStats = "Display Stats";
        static string k_PanelMaterials = "Material";
        static string k_PanelLighting = "Lighting";
        static string k_PanelRendering = "Rendering";
        static string k_PanelDecals = "Decals";

        DebugUI.Widget[] m_DebugDisplayStatsItems;
        DebugUI.Widget[] m_DebugMaterialItems;
        DebugUI.Widget[] m_DebugLightingItems;
        DebugUI.Widget[] m_DebugRenderingItems;
        DebugUI.Widget[] m_DebugDecalsItems;

        static GUIContent[] s_LightingFullScreenDebugStrings = null;
        static int[] s_LightingFullScreenDebugValues = null;
        static GUIContent[] s_RenderingFullScreenDebugStrings = null;
        static int[] s_RenderingFullScreenDebugValues = null;
        static GUIContent[] s_MsaaSamplesDebugStrings = null;
        static int[] s_MsaaSamplesDebugValues = null;

        static List<GUIContent> s_CameraNames = new List<GUIContent>();
        static GUIContent[] s_CameraNamesStrings = null;
        static int[] s_CameraNamesValues = null;

        internal class DebugData
        {
            public float debugOverlayRatio = 0.33f;
            public FullScreenDebugMode fullScreenDebugMode = FullScreenDebugMode.None;
            public float fullscreenDebugMip = 0.0f;
            public bool showSSSampledColor = false;

            public MaterialDebugSettings materialDebugSettings = new MaterialDebugSettings();
            public LightingDebugSettings lightingDebugSettings = new LightingDebugSettings();
            public MipMapDebugSettings mipMapDebugSettings = new MipMapDebugSettings();
            public ColorPickerDebugSettings colorPickerDebugSettings = new ColorPickerDebugSettings();
            public FalseColorDebugSettings falseColorDebugSettings = new FalseColorDebugSettings();
            public DecalsDebugSettings decalsDebugSettings = new DecalsDebugSettings();
            public MSAASamples msaaSamples = MSAASamples.None;
            
            public int debugCameraToFreeze = 0;

            //saved enum fields for when repainting
            public int lightingDebugModeEnumIndex;
            public int lightingFulscreenDebugModeEnumIndex;
            public int tileClusterDebugEnumIndex;
            public int mipMapsEnumIndex;
            public int materialEnumIndex;
            public int engineEnumIndex;
            public int attributesEnumIndex;
            public int propertiesEnumIndex;
            public int gBufferEnumIndex;
            public int shadowDebugModeEnumIndex;
            public int tileClusterDebugByCategoryEnumIndex;
            public int lightVolumeDebugTypeEnumIndex;
            public int renderingFulscreenDebugModeEnumIndex;
            public int terrainTextureEnumIndex;
            public int colorPickerDebugModeEnumIndex;
            public int msaaSampleDebugModeEnumIndex;
            public int debugCameraToFreezeEnumIndex;
        }
        internal DebugData m_Data;

        static bool needsRefreshingCameraFreezeList = true;

        public DebugDisplaySettings()
        {
            FillFullScreenDebugEnum(ref s_LightingFullScreenDebugStrings, ref s_LightingFullScreenDebugValues, FullScreenDebugMode.MinLightingFullScreenDebug, FullScreenDebugMode.MaxLightingFullScreenDebug);
            FillFullScreenDebugEnum(ref s_RenderingFullScreenDebugStrings, ref s_RenderingFullScreenDebugValues, FullScreenDebugMode.MinRenderingFullScreenDebug, FullScreenDebugMode.MaxRenderingFullScreenDebug);

            s_MsaaSamplesDebugStrings = Enum.GetNames(typeof(MSAASamples))
                .Select(t => new GUIContent(t))
                .ToArray();
            s_MsaaSamplesDebugValues = (int[])Enum.GetValues(typeof(MSAASamples));

            m_Data = new DebugData();
        }
        
        Action IDebugData.GetReset() => () => m_Data = new DebugData();
        
        public int GetDebugMaterialIndex()
        {
            return m_Data.materialDebugSettings.GetDebugMaterialIndex();
        }

        public DebugLightingMode GetDebugLightingMode()
        {
            return m_Data.lightingDebugSettings.debugLightingMode;
        }

        public ShadowMapDebugMode GetDebugShadowMapMode()
        {
            return m_Data.lightingDebugSettings.shadowDebugMode;
        }

        public DebugMipMapMode GetDebugMipMapMode()
        {
            return m_Data.mipMapDebugSettings.debugMipMapMode;
        }

        public DebugMipMapModeTerrainTexture GetDebugMipMapModeTerrainTexture()
        {
            return m_Data.mipMapDebugSettings.terrainTexture;
        }

        public ColorPickerDebugMode GetDebugColorPickerMode()
        {
            return m_Data.colorPickerDebugSettings.colorPickerMode;
        }

        public bool IsCameraFreezeEnabled()
        {
            return m_Data.debugCameraToFreeze != 0;
        }
        public string GetFrozenCameraName()
        {
            return s_CameraNamesStrings[m_Data.debugCameraToFreeze].text;
        }

        public bool IsDebugDisplayEnabled()
        {
            return m_Data.materialDebugSettings.IsDebugDisplayEnabled() || m_Data.lightingDebugSettings.IsDebugDisplayEnabled() || m_Data.mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugDisplayRemovePostprocess()
        {
            // We want to keep post process when only the override more are enabled and none of the other
            return m_Data.materialDebugSettings.IsDebugDisplayEnabled() || m_Data.lightingDebugSettings.IsDebugDisplayRemovePostprocess() || m_Data.mipMapDebugSettings.IsDebugDisplayEnabled() || IsDebugFullScreenEnabled();
        }

        public bool IsDebugMaterialDisplayEnabled()
        {
            return m_Data.materialDebugSettings.IsDebugDisplayEnabled();
        }

        public bool IsDebugFullScreenEnabled()
        {
            return m_Data.fullScreenDebugMode != FullScreenDebugMode.None;
        }

        public bool IsDebugMipMapDisplayEnabled()
        {
            return m_Data.mipMapDebugSettings.IsDebugDisplayEnabled();
        }

        private void DisableNonMaterialDebugSettings()
        {
            m_Data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            m_Data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
        }

        public void SetDebugViewMaterial(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            m_Data.materialDebugSettings.SetDebugViewMaterial(value);
        }

        public void SetDebugViewEngine(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            m_Data.materialDebugSettings.SetDebugViewEngine(value);
        }

        public void SetDebugViewVarying(DebugViewVarying value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            m_Data.materialDebugSettings.SetDebugViewVarying(value);
        }

        public void SetDebugViewProperties(DebugViewProperties value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            m_Data.materialDebugSettings.SetDebugViewProperties(value);
        }

        public void SetDebugViewGBuffer(int value)
        {
            if (value != 0)
                DisableNonMaterialDebugSettings();
            m_Data.materialDebugSettings.SetDebugViewGBuffer(value);
        }

        public void SetFullScreenDebugMode(FullScreenDebugMode value)
        {
            if (m_Data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
                value = 0;
            
            m_Data.fullScreenDebugMode = value;
        }

        public void SetShadowDebugMode(ShadowMapDebugMode value)
        {
            // When SingleShadow is enabled, we don't render full screen debug modes
            if (value == ShadowMapDebugMode.SingleShadow)
                m_Data.fullScreenDebugMode = 0;
            m_Data.lightingDebugSettings.shadowDebugMode = value;
        }

        public void SetDebugLightingMode(DebugLightingMode value)
        {
            if (value != 0)
            {
                m_Data.materialDebugSettings.DisableMaterialDebug();
                m_Data.mipMapDebugSettings.debugMipMapMode = DebugMipMapMode.None;
            }
            m_Data.lightingDebugSettings.debugLightingMode = value;
        }

        public void SetMipMapMode(DebugMipMapMode value)
        {
            if (value != 0)
            {
                m_Data.materialDebugSettings.DisableMaterialDebug();
                m_Data.lightingDebugSettings.debugLightingMode = DebugLightingMode.None;
            }
            m_Data.mipMapDebugSettings.debugMipMapMode = value;
        }

        public void UpdateMaterials()
        {
            if (m_Data.mipMapDebugSettings.debugMipMapMode != 0)
                Texture.SetStreamingTextureMaterialDebugProperties();
        }

        public void UpdateCameraFreezeOptions()
        {
            if(needsRefreshingCameraFreezeList)
            {
                s_CameraNames.Insert(0, new GUIContent("None"));

                s_CameraNamesStrings = s_CameraNames.ToArray();
                s_CameraNamesValues = Enumerable.Range(0, s_CameraNames.Count()).ToArray();

                UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
                RegisterRenderingDebug();
                needsRefreshingCameraFreezeList = false;
            }
        }

        public bool DebugNeedsExposure()
        {
            DebugLightingMode debugLighting = m_Data.lightingDebugSettings.debugLightingMode;
            DebugViewGbuffer debugGBuffer = (DebugViewGbuffer)m_Data.materialDebugSettings.debugViewGBuffer;
            return (debugLighting == DebugLightingMode.DiffuseLighting || debugLighting == DebugLightingMode.SpecularLighting) ||
                (debugGBuffer == DebugViewGbuffer.BakeDiffuseLightingWithAlbedoPlusEmissive) ||
                (m_Data.fullScreenDebugMode == FullScreenDebugMode.PreRefractionColorPyramid || m_Data.fullScreenDebugMode == FullScreenDebugMode.FinalColorPyramid || m_Data.fullScreenDebugMode == FullScreenDebugMode.ScreenSpaceReflections);
        }

        void RegisterDisplayStatsDebug()
        {
            m_DebugDisplayStatsItems = new DebugUI.Widget[]
            {
                new DebugUI.Value { displayName = "Frame Rate (fps)", getter = () => 1f / Time.smoothDeltaTime, refreshRate = 1f / 30f },
                new DebugUI.Value { displayName = "Frame Time (ms)", getter = () => Time.smoothDeltaTime * 1000f, refreshRate = 1f / 30f }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDisplayStats, true);
            panel.flags = DebugUI.Flags.RuntimeOnly;
            panel.children.Add(m_DebugDisplayStatsItems);
        }

        public void RegisterMaterialDebug()
        {
            m_DebugMaterialItems = new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Material", getter = () => m_Data.materialDebugSettings.debugViewMaterial, setter = value => SetDebugViewMaterial(value), enumNames = MaterialDebugSettings.debugViewMaterialStrings, enumValues = MaterialDebugSettings.debugViewMaterialValues, getIndex = () => m_Data.materialEnumIndex, setIndex = value => m_Data.materialEnumIndex = value },
                new DebugUI.EnumField { displayName = "Engine", getter = () => m_Data.materialDebugSettings.debugViewEngine, setter = value => SetDebugViewEngine(value), enumNames = MaterialDebugSettings.debugViewEngineStrings, enumValues = MaterialDebugSettings.debugViewEngineValues, getIndex = () => m_Data.engineEnumIndex, setIndex = value => m_Data.engineEnumIndex = value },
                new DebugUI.EnumField { displayName = "Attributes", getter = () => (int)m_Data.materialDebugSettings.debugViewVarying, setter = value => SetDebugViewVarying((DebugViewVarying)value), autoEnum = typeof(DebugViewVarying), getIndex = () => m_Data.attributesEnumIndex, setIndex = value => m_Data.attributesEnumIndex = value },
                new DebugUI.EnumField { displayName = "Properties", getter = () => (int)m_Data.materialDebugSettings.debugViewProperties, setter = value => SetDebugViewProperties((DebugViewProperties)value), autoEnum = typeof(DebugViewProperties), getIndex = () => m_Data.propertiesEnumIndex, setIndex = value => m_Data.propertiesEnumIndex = value },
                new DebugUI.EnumField { displayName = "GBuffer", getter = () => m_Data.materialDebugSettings.debugViewGBuffer, setter = value => SetDebugViewGBuffer(value), enumNames = MaterialDebugSettings.debugViewMaterialGBufferStrings, enumValues = MaterialDebugSettings.debugViewMaterialGBufferValues, getIndex = () => m_Data.gBufferEnumIndex, setIndex = value => m_Data.gBufferEnumIndex = value }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelMaterials, true);
            panel.children.Add(m_DebugMaterialItems);
        }

        // For now we just rebuild the lighting panel if needed, but ultimately it could be done in a better way
        void RefreshLightingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            RegisterLightingDebug();
        }

        void RefreshDecalsDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            RegisterDecalsDebug();
        }

        void RefreshRenderingDebug<T>(DebugUI.Field<T> field, T value)
        {
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            RegisterRenderingDebug();
        }

        public void RegisterLightingDebug()
        {
            var list = new List<DebugUI.Widget>();

            list.Add(new DebugUI.Foldout
            {
                displayName = "Show Light By Type",
                children = {
                    new DebugUI.BoolField { displayName = "Show Directional Lights", getter = () => m_Data.lightingDebugSettings.showDirectionalLight, setter = value => m_Data.lightingDebugSettings.showDirectionalLight = value },
                    new DebugUI.BoolField { displayName = "Show Punctual Lights", getter = () => m_Data.lightingDebugSettings.showPunctualLight, setter = value => m_Data.lightingDebugSettings.showPunctualLight = value },
                    new DebugUI.BoolField { displayName = "Show Area Lights", getter = () => m_Data.lightingDebugSettings.showAreaLight, setter = value => m_Data.lightingDebugSettings.showAreaLight = value },
                    new DebugUI.BoolField { displayName = "Show Reflection Probe", getter = () => m_Data.lightingDebugSettings.showReflectionProbe, setter = value => m_Data.lightingDebugSettings.showReflectionProbe = value },
                }
            });

            list.Add(new DebugUI.EnumField { displayName = "Shadow Debug Mode", getter = () => (int)m_Data.lightingDebugSettings.shadowDebugMode, setter = value => SetShadowDebugMode((ShadowMapDebugMode)value), autoEnum = typeof(ShadowMapDebugMode), onValueChanged = RefreshLightingDebug, getIndex = () => m_Data.shadowDebugModeEnumIndex, setIndex = value => m_Data.shadowDebugModeEnumIndex = value });

            if (m_Data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.VisualizeShadowMap || m_Data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow)
            {
                var container = new DebugUI.Container();
                container.children.Add(new DebugUI.BoolField { displayName = "Use Selection", getter = () => m_Data.lightingDebugSettings.shadowDebugUseSelection, setter = value => m_Data.lightingDebugSettings.shadowDebugUseSelection = value, flags = DebugUI.Flags.EditorOnly, onValueChanged = RefreshLightingDebug });

                if (!m_Data.lightingDebugSettings.shadowDebugUseSelection)
                    container.children.Add(new DebugUI.UIntField { displayName = "Shadow Map Index", getter = () => m_Data.lightingDebugSettings.shadowMapIndex, setter = value => m_Data.lightingDebugSettings.shadowMapIndex = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetCurrentShadowCount() - 1u });

                list.Add(container);
            }

            list.Add(new DebugUI.FloatField
            {
                displayName = "Global Shadow Scale Factor",
                getter = () => m_Data.lightingDebugSettings.shadowResolutionScaleFactor,
                setter = (v) => m_Data.lightingDebugSettings.shadowResolutionScaleFactor = v,
                min = () => 0.01f,
                max = () => 4.0f,
            });

            list.Add(new DebugUI.BoolField{
                displayName = "Clear Shadow atlas",
                getter = () => m_Data.lightingDebugSettings.clearShadowAtlas,
                setter = (v) => m_Data.lightingDebugSettings.clearShadowAtlas = v
            });

            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Min Value", getter = () => m_Data.lightingDebugSettings.shadowMinValue, setter = value => m_Data.lightingDebugSettings.shadowMinValue = value });
            list.Add(new DebugUI.FloatField { displayName = "Shadow Range Max Value", getter = () => m_Data.lightingDebugSettings.shadowMaxValue, setter = value => m_Data.lightingDebugSettings.shadowMaxValue = value });

            list.Add(new DebugUI.EnumField { displayName = "Lighting Debug Mode", getter = () => (int)m_Data.lightingDebugSettings.debugLightingMode, setter = value => SetDebugLightingMode((DebugLightingMode)value), autoEnum = typeof(DebugLightingMode), onValueChanged = RefreshLightingDebug, getIndex = () => m_Data.lightingDebugModeEnumIndex, setIndex = value => m_Data.lightingDebugModeEnumIndex = value });
            list.Add(new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)m_Data.fullScreenDebugMode, setter = value => SetFullScreenDebugMode((FullScreenDebugMode)value), enumNames = s_LightingFullScreenDebugStrings, enumValues = s_LightingFullScreenDebugValues, onValueChanged = RefreshLightingDebug, getIndex = () => m_Data.lightingFulscreenDebugModeEnumIndex, setIndex = value => m_Data.lightingFulscreenDebugModeEnumIndex = value });
            switch (m_Data.fullScreenDebugMode)
            {
                case FullScreenDebugMode.PreRefractionColorPyramid:
                case FullScreenDebugMode.FinalColorPyramid:
                case FullScreenDebugMode.DepthPyramid:
                {
                    list.Add(new DebugUI.Container
                    {
                        children =
                        {
                            new DebugUI.UIntField
                            {
                                displayName = "Fullscreen Debug Mip",
                                getter = () =>
                                    {
                                        int id;
                                        switch (m_Data.fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        return (uint)(m_Data.fullscreenDebugMip * lodCount);
                                    },
                                setter = value =>
                                    {
                                        int id;
                                        switch (m_Data.fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        m_Data.fullscreenDebugMip = (float)Convert.ChangeType(value, typeof(float)) / lodCount;
                                    },
                                min = () => 0u,
                                max = () =>
                                    {
                                        int id;
                                        switch (m_Data.fullScreenDebugMode)
                                        {
                                            case FullScreenDebugMode.FinalColorPyramid:
                                            case FullScreenDebugMode.PreRefractionColorPyramid:
                                                id = HDShaderIDs._ColorPyramidScale;
                                                break;
                                            default:
                                                id = HDShaderIDs._DepthPyramidScale;
                                                break;
                                        }
                                        var size = Shader.GetGlobalVector(id);
                                        float lodCount = size.z;
                                        return (uint)lodCount;
                                    }
                            }
                        }
                    });
                    break;
                }
                default:
                    m_Data.fullscreenDebugMip = 0;
                    break;
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Smoothness", getter = () => m_Data.lightingDebugSettings.overrideSmoothness, setter = value => m_Data.lightingDebugSettings.overrideSmoothness = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.overrideSmoothness)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Smoothness", getter = () => m_Data.lightingDebugSettings.overrideSmoothnessValue, setter = value => m_Data.lightingDebugSettings.overrideSmoothnessValue = value, min = () => 0f, max = () => 1f, incStep = 0.025f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Albedo", getter = () => m_Data.lightingDebugSettings.overrideAlbedo, setter = value => m_Data.lightingDebugSettings.overrideAlbedo = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.overrideAlbedo)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Albedo", getter = () => m_Data.lightingDebugSettings.overrideAlbedoValue, setter = value => m_Data.lightingDebugSettings.overrideAlbedoValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Normal", getter = () => m_Data.lightingDebugSettings.overrideNormal, setter = value => m_Data.lightingDebugSettings.overrideNormal = value });

            list.Add(new DebugUI.BoolField { displayName = "Override Specular Color", getter = () => m_Data.lightingDebugSettings.overrideSpecularColor, setter = value => m_Data.lightingDebugSettings.overrideSpecularColor = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.overrideSpecularColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Specular Color", getter = () => m_Data.lightingDebugSettings.overrideSpecularColorValue, setter = value => m_Data.lightingDebugSettings.overrideSpecularColorValue = value, showAlpha = false, hdr = false }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Override Emissive Color", getter = () => m_Data.lightingDebugSettings.overrideEmissiveColor, setter = value => m_Data.lightingDebugSettings.overrideEmissiveColor = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.overrideEmissiveColor)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.ColorField { displayName = "Emissive Color", getter = () => m_Data.lightingDebugSettings.overrideEmissiveColorValue, setter = value => m_Data.lightingDebugSettings.overrideEmissiveColorValue = value, showAlpha = false, hdr = true }
                    }
                });
            }

            list.Add(new DebugUI.EnumField { displayName = "Tile/Cluster Debug", getter = () => (int)m_Data.lightingDebugSettings.tileClusterDebug, setter = value => m_Data.lightingDebugSettings.tileClusterDebug = (LightLoop.TileClusterDebug)value, autoEnum = typeof(LightLoop.TileClusterDebug), onValueChanged = RefreshLightingDebug, getIndex = () => m_Data.tileClusterDebugEnumIndex, setIndex = value => m_Data.tileClusterDebugEnumIndex = value });
            if (m_Data.lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.None && m_Data.lightingDebugSettings.tileClusterDebug != LightLoop.TileClusterDebug.MaterialFeatureVariants)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Tile/Cluster Debug By Category", getter = () => (int)m_Data.lightingDebugSettings.tileClusterDebugByCategory, setter = value => m_Data.lightingDebugSettings.tileClusterDebugByCategory = (LightLoop.TileClusterCategoryDebug)value, autoEnum = typeof(LightLoop.TileClusterCategoryDebug), getIndex = () => m_Data.tileClusterDebugByCategoryEnumIndex, setIndex = value => m_Data.tileClusterDebugByCategoryEnumIndex = value }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Sky Reflection", getter = () => m_Data.lightingDebugSettings.displaySkyReflection, setter = value => m_Data.lightingDebugSettings.displaySkyReflection = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.displaySkyReflection)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.FloatField { displayName = "Sky Reflection Mipmap", getter = () => m_Data.lightingDebugSettings.skyReflectionMipmap, setter = value => m_Data.lightingDebugSettings.skyReflectionMipmap = value, min = () => 0f, max = () => 1f, incStep = 0.05f }
                    }
                });
            }

            list.Add(new DebugUI.BoolField { displayName = "Display Light Volumes", getter = () => m_Data.lightingDebugSettings.displayLightVolumes, setter = value => m_Data.lightingDebugSettings.displayLightVolumes = value, onValueChanged = RefreshLightingDebug });
            if (m_Data.lightingDebugSettings.displayLightVolumes)
            {
                list.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Light Volume Debug Type", getter = () => (int)m_Data.lightingDebugSettings.lightVolumeDebugByCategory, setter = value => m_Data.lightingDebugSettings.lightVolumeDebugByCategory = (LightLoop.LightVolumeDebug)value, autoEnum = typeof(LightLoop.LightVolumeDebug), getIndex = () => m_Data.lightVolumeDebugTypeEnumIndex, setIndex = value => m_Data.lightVolumeDebugTypeEnumIndex = value },
                        new DebugUI.UIntField { displayName = "Max Debug Light Count", getter = () => (uint)m_Data.lightingDebugSettings.maxDebugLightCount, setter = value => m_Data.lightingDebugSettings.maxDebugLightCount = value, min = () => 0, max = () => 24, incStep = 1 }
                    }
                });
            }

            if (DebugNeedsExposure())
                list.Add(new DebugUI.FloatField { displayName = "Debug Exposure", getter = () => m_Data.lightingDebugSettings.debugExposure, setter = value => m_Data.lightingDebugSettings.debugExposure = value });


            m_DebugLightingItems = list.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelLighting, true);
            panel.children.Add(m_DebugLightingItems);
        }

        public void RegisterRenderingDebug()
        {
            var widgetList = new List<DebugUI.Widget>();

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "Fullscreen Debug Mode", getter = () => (int)m_Data.fullScreenDebugMode, setter = value => m_Data.fullScreenDebugMode = (FullScreenDebugMode)value, enumNames = s_RenderingFullScreenDebugStrings, enumValues = s_RenderingFullScreenDebugValues, getIndex = () => m_Data.renderingFulscreenDebugModeEnumIndex, setIndex = value => m_Data.renderingFulscreenDebugModeEnumIndex = value },
                new DebugUI.EnumField { displayName = "MipMaps", getter = () => (int)m_Data.mipMapDebugSettings.debugMipMapMode, setter = value => SetMipMapMode((DebugMipMapMode)value), autoEnum = typeof(DebugMipMapMode), onValueChanged = RefreshRenderingDebug, getIndex = () => m_Data.mipMapsEnumIndex, setIndex = value => m_Data.mipMapsEnumIndex = value },
            });

            if (m_Data.mipMapDebugSettings.debugMipMapMode != DebugMipMapMode.None)
            {
                widgetList.Add(new DebugUI.Container
                {
                    children =
                    {
                        new DebugUI.EnumField { displayName = "Terrain Texture", getter = ()=>(int)m_Data.mipMapDebugSettings.terrainTexture, setter = value => m_Data.mipMapDebugSettings.terrainTexture = (DebugMipMapModeTerrainTexture)value, autoEnum = typeof(DebugMipMapModeTerrainTexture), getIndex = () => m_Data.terrainTextureEnumIndex, setIndex = value => m_Data.terrainTextureEnumIndex = value }
                    }
                });
            }

            widgetList.AddRange(new []
            {
                new DebugUI.Container
                {
                    displayName = "Color Picker",
                    flags = DebugUI.Flags.EditorOnly,
                    children =
                    {
                        new DebugUI.EnumField  { displayName = "Debug Mode", getter = () => (int)m_Data.colorPickerDebugSettings.colorPickerMode, setter = value => m_Data.colorPickerDebugSettings.colorPickerMode = (ColorPickerDebugMode)value, autoEnum = typeof(ColorPickerDebugMode), getIndex = () => m_Data.colorPickerDebugModeEnumIndex, setIndex = value => m_Data.colorPickerDebugModeEnumIndex = value },
                        new DebugUI.ColorField { displayName = "Font Color", flags = DebugUI.Flags.EditorOnly, getter = () => m_Data.colorPickerDebugSettings.fontColor, setter = value => m_Data.colorPickerDebugSettings.fontColor = value }
                    }
                }
            });
            
            widgetList.Add(new DebugUI.BoolField  { displayName = "False Color Mode", getter = () => m_Data.falseColorDebugSettings.falseColor, setter = value => m_Data.falseColorDebugSettings.falseColor = value, onValueChanged = RefreshRenderingDebug });
            if (m_Data.falseColorDebugSettings.falseColor)
            {
                widgetList.Add(new DebugUI.Container{
                    flags = DebugUI.Flags.EditorOnly,
                    children = 
                    {
                        new DebugUI.FloatField { displayName = "Range Threshold 0", getter = () => m_Data.falseColorDebugSettings.colorThreshold0, setter = value => m_Data.falseColorDebugSettings.colorThreshold0 = Mathf.Min(value, m_Data.falseColorDebugSettings.colorThreshold1) },
                        new DebugUI.FloatField { displayName = "Range Threshold 1", getter = () => m_Data.falseColorDebugSettings.colorThreshold1, setter = value => m_Data.falseColorDebugSettings.colorThreshold1 = Mathf.Clamp(value, m_Data.falseColorDebugSettings.colorThreshold0, m_Data.falseColorDebugSettings.colorThreshold2) },
                        new DebugUI.FloatField { displayName = "Range Threshold 2", getter = () => m_Data.falseColorDebugSettings.colorThreshold2, setter = value => m_Data.falseColorDebugSettings.colorThreshold2 = Mathf.Clamp(value, m_Data.falseColorDebugSettings.colorThreshold1, m_Data.falseColorDebugSettings.colorThreshold3) },
                        new DebugUI.FloatField { displayName = "Range Threshold 3", getter = () => m_Data.falseColorDebugSettings.colorThreshold3, setter = value => m_Data.falseColorDebugSettings.colorThreshold3 = Mathf.Max(value, m_Data.falseColorDebugSettings.colorThreshold2) },
                    }
                });
            }

            widgetList.AddRange(new DebugUI.Widget[]
            {
                new DebugUI.EnumField { displayName = "MSAA Samples", getter = () => (int)m_Data.msaaSamples, setter = value => m_Data.msaaSamples = (MSAASamples)value, enumNames = s_MsaaSamplesDebugStrings, enumValues = s_MsaaSamplesDebugValues, getIndex = () => m_Data.msaaSampleDebugModeEnumIndex, setIndex = value => m_Data.msaaSampleDebugModeEnumIndex = value },
            });

            widgetList.AddRange(new DebugUI.Widget[]
            {
                    new DebugUI.EnumField { displayName = "Freeze Camera for culling", getter = () => m_Data.debugCameraToFreeze, setter = value => m_Data.debugCameraToFreeze = value, enumNames = s_CameraNamesStrings, enumValues = s_CameraNamesValues, getIndex = () => m_Data.debugCameraToFreezeEnumIndex, setIndex = value => m_Data.debugCameraToFreezeEnumIndex = value },
            });

            m_DebugRenderingItems = widgetList.ToArray();
            var panel = DebugManager.instance.GetPanel(k_PanelRendering, true);
            panel.children.Add(m_DebugRenderingItems);
        }

        public void RegisterDecalsDebug()
        {
            m_DebugDecalsItems = new DebugUI.Widget[]
            {
                new DebugUI.BoolField { displayName = "Display atlas", getter = () => m_Data.decalsDebugSettings.displayAtlas, setter = value => m_Data.decalsDebugSettings.displayAtlas = value},
                new DebugUI.UIntField { displayName = "Mip Level", getter = () => m_Data.decalsDebugSettings.mipLevel, setter = value => m_Data.decalsDebugSettings.mipLevel = value, min = () => 0u, max = () => (uint)(RenderPipelineManager.currentPipeline as HDRenderPipeline).GetDecalAtlasMipCount() }
            };

            var panel = DebugManager.instance.GetPanel(k_PanelDecals, true);
            panel.children.Add(m_DebugDecalsItems);
        }

        public void RegisterDebug()
        {
            RegisterDecalsDebug();
            RegisterDisplayStatsDebug();
            RegisterMaterialDebug();
            RegisterLightingDebug();
            RegisterRenderingDebug();
            DebugManager.instance.RegisterData(this);
        }

        public void UnregisterDebug()
        {
            UnregisterDebugItems(k_PanelDecals, m_DebugDecalsItems);
            UnregisterDebugItems(k_PanelDisplayStats, m_DebugDisplayStatsItems);
            UnregisterDebugItems(k_PanelMaterials, m_DebugMaterialItems);
            UnregisterDebugItems(k_PanelLighting, m_DebugLightingItems);
            UnregisterDebugItems(k_PanelRendering, m_DebugRenderingItems);
            DebugManager.instance.UnregisterData(this);
        }

        void UnregisterDebugItems(string panelName, DebugUI.Widget[] items)
        {
            var panel = DebugManager.instance.GetPanel(panelName);
            if (panel != null)
                panel.children.Remove(items);
        }

        void FillFullScreenDebugEnum(ref GUIContent[] strings, ref int[] values, FullScreenDebugMode min, FullScreenDebugMode max)
        {
            int count = max - min - 1;
            strings = new GUIContent[count + 1];
            values = new int[count + 1];
            strings[0] = new GUIContent(FullScreenDebugMode.None.ToString());
            values[0] = (int)FullScreenDebugMode.None;
            int index = 1;
            for (int i = (int)min + 1; i < (int)max; ++i)
            {
                strings[index] = new GUIContent(((FullScreenDebugMode)i).ToString());
                values[index] = i;
                index++;
            }
        }

        static string FormatVector(Vector3 v)
        {
            return string.Format("({0:F6}, {1:F6}, {2:F6})", v.x, v.y, v.z);
        }

        public static void RegisterCamera(Camera camera, HDAdditionalCameraData additionalData)
        {
            string name = camera.name;
            if (s_CameraNames.FindIndex(x => x.text.Equals(name)) < 0)
            {
                s_CameraNames.Add(new GUIContent(name));
                needsRefreshingCameraFreezeList = true;
            }
            
            FrameSettings.RegisterDebug(name, additionalData.GetFrameSettings());
            DebugManager.instance.RegisterData(additionalData);
        }

        public static void UnRegisterCamera(Camera camera, HDAdditionalCameraData additionalData)
        {
            string name = camera.name;
            int indexOfCamera = s_CameraNames.FindIndex(x => x.text.Equals(camera.name));
            if (indexOfCamera > 0)
            {
                s_CameraNames.RemoveAt(indexOfCamera);
                needsRefreshingCameraFreezeList = true;
            }
            FrameSettings.UnRegisterDebug(name);
            DebugManager.instance.UnregisterData(additionalData);
        }
    }
}
