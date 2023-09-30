#ifndef __INDIRECT_INCLUDE__
#define __INDIRECT_INCLUDE__

struct Matrix2x2
{
    float4 Row0;
    float4 Row1;
};

struct BoundsData
{
    float3 BoundsCenter;
    float3 BoundsExtents;
};

struct SortingData
{
    uint  DrawCallInstanceIndex;
    float DistanceToCamera;
};

#endif
