#ifndef __INDIRECT_INCLUDE__
#define __INDIRECT_INCLUDE__

struct BoundsData
{
    float3 BoundsCenter;
    float3 BoundsExtents;
};

struct Indirect2x2Matrix
{
    float4 FirstRow;
    float4 SecondRow;
};

struct SortingData
{
    uint  DrawCallInstanceIndex;
    float DistanceToCamera;
};

#endif
