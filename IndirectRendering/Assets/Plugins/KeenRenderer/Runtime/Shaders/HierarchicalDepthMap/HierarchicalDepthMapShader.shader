Shader "IndirectRendering/HiZ/Buffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _DepthMapPrecision("Depth Map Precision", Range(1.8, 2)) = 1.8
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            // #pragma fragment Blit
            #include "HierarchicalDepthMap.hlsl"
            ENDHLSL
        }

//        Pass
//        {
//            HLSLPROGRAM
//            #pragma target 4.5
//            #pragma vertex Vertex
//            #pragma fragment Reduce
//            #include "HierarchicalDepthMap.hlsl"
//            ENDHLSL
//        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex Vertex
            #pragma fragment Fragment
            // #pragma fragment Blit
            #include "HierarchicalDepthMap.hlsl"
            ENDHLSL
        }

//        Pass
//        {
//            HLSLPROGRAM
//            #pragma target 4.6
//            #pragma vertex Vertex
//            #pragma fragment Reduce
//            #include "HierarchicalDepthMap.hlsl"
//            ENDHLSL
//        }
    }
}