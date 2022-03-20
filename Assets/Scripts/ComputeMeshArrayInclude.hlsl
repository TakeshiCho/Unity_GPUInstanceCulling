#ifndef COMPUTE_MESH_ARRAY_INCLUDE
#define COMPUTE_MESH_ARRAY_INCLUDE

struct MeshArrayData
{
    float3 Position;
    float4 Color;
};

float2 Index_to_Index2D(uint index , uint length)
{
    float2 index2D;
    index2D.x = (index+1)%length;
    index2D.y = (index)/length;
    return index2D;
}

float Random(uint number)
{
    return frac(sin(dot(float2(number,number+3),float2(12.9898,78.233)))*43758.5453123);
}

#endif