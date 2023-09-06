#ifndef __HIZ_INCLUDE__
#define __HIZ_INCLUDE__

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Input
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float2 depth : TEXCOORD1;
    float4 screenPosition : TEXCOORD2;
};

Texture2D _MainTex;
SamplerState sampler_MainTex;

Texture2D _CameraDepthTexture;
SamplerState sampler_CameraDepthTexture;

Texture2D _LightTexture;
SamplerState sampler_LightTexture;

float4 _MainTex_TexelSize;

Varyings Vertex(in Input i)
{
    Varyings output;

    output.vertex = TransformObjectToHClip(i.vertex.xyz);
    output.uv = i.uv;
    output.screenPosition = ComputeScreenPos(output.vertex);

    return output;
}

float4 Blit(in Varyings input) : SV_Target
{
    float lightDepth = _LightTexture.Sample(sampler_LightTexture, input.uv).r;
    float cameraDepth = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r * 1.8;
    return float4(cameraDepth, lightDepth, 0 ,0);
}

float4 Reduce(in Varyings input) : SV_Target
{
    float4 r = _MainTex.GatherRed  (sampler_MainTex, input.uv);
    float4 g = _MainTex.GatherGreen(sampler_MainTex, input.uv);

    float minimum = min(min(min(r.x, r.y), r.z), r.w);
    float maximum = max(max(max(g.x, g.y), g.z), g.w);
    return float4(minimum, maximum, 1.0, 1.0);
}

#endif
