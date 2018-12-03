Shader "Hidden/ScriptableRenderPipeline/DrawShadowAreas"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "DrawShadowAreas"
            Cull Off
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM

            CBUFFER_START(UnityGlobal)
            Texture2DArray<int> _ShadowIndexBuffer;
            CBUFFER_END

            #pragma vertex Vert_0
            #pragma fragment Frag

            float4 Vert_0(uint vertexID : VERTEXID_SEMANTIC) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition(vertexID, UNITY_RAW_FAR_CLIP_VALUE);
            }

            float4 Frag(float4 positionCS : SV_Position) : SV_Target
            {
                int bufferValue = _ShadowIndexBuffer[uint3(positionCS.xy, 0)];
                return float4(0, 0, bufferValue, 0.2);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
