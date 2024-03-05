struct DirectionalLightData
{
    float4 colour;
    float intensity;
    float3 direction;
};
struct PointLightData
{
    float4 colour;
    float intensity;
    float3 worldPos;
};