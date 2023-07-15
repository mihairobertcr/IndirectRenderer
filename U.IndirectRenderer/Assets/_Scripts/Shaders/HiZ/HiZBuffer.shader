Shader "IndirectRendering/HiZ/Buffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            //HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vertex
            #pragma fragment blit
            #include "../IndirectData.hlsl"
            #include "HiZ.cginc"
            //ENDHLSL
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            //HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vertex
            #pragma fragment reduce
            #include "../IndirectData.hlsl"
            #include "HiZ.cginc"
            //ENDHLSL
            ENDCG
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            //HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vertex
            #pragma fragment blit
            #include "../IndirectData.hlsl"
            #include "HiZ.cginc"
            //ENDHLSL
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            //HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex vertex
            #pragma fragment reduce
            #include "../IndirectData.hlsl"
            #include "HiZ.cginc"
            //ENDHLSL
            ENDCG
        }
    }
}