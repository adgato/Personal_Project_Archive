float invlerp(float a, float b, float value)
{
    if (a == b)
        return step(a, value);
    else
        return (value - a) / (b - a);
}

float remap(float a, float b, float min, float max, float value)
{
    return lerp(a, b, invlerp(min, max, value));
}

float remapClamp(float a, float b, float min, float max, float value)
{
    return lerp(a, b, saturate(invlerp(min, max, value)));
}

float3 mulTRS3x4(float4x4 TRS, float3 v)
{
    return float3(
        TRS[0][0] * v.x + TRS[0][1] * v.y + TRS[0][2] * v.z + TRS[0][3],
        TRS[1][0] * v.x + TRS[1][1] * v.y + TRS[1][2] * v.z + TRS[1][3],
        TRS[2][0] * v.x + TRS[2][1] * v.y + TRS[2][2] * v.z + TRS[2][3]);
}

void sqrDistToLineBetween_float(float4 a, float4 b, float3 p, out float sqrDist)
{
    if (b.a < 0.5 && a.a < 0.5)
    {
        sqrDist = 100;
        return;
    }
    else if (a.a < 0.5)
    {
        float3 pb = p - b.xyz;
        sqrDist = dot(pb, pb);
        return;
    }
    else if (b.a < 0.5)
    {
        float3 pa = p - a.xyz;
        sqrDist = dot(pa, pa);
        return;
    }
    
    float3 v = p - a.xyz;
    float3 d = b.xyz - a.xyz;
    float dotD = dot(d, d);
    
    if (dotD < 0.0001)
    {
        sqrDist = dot(v, v);
        return;
    }
    
    float3 pToLine = a.xyz + saturate(dot(v, d) / dotD) * d - p;
    sqrDist = dot(pToLine, pToLine);

}

// Smooth minimum of two values, controlled by smoothing factor k
// When k = 0, this behaves identically to min(a, b)
float smoothMin(float a, float b, float k) {
	 k = max(0, k);
	 // https://www.iquilezles.org/www/articles/smin/smin.htm
	 float h = max(0, min(1, (b - a + k) / (2 * k)));
	 return a * h + b * (1 - h) - k * h * (1 - h);
}

// Smooth maximum of two values, controlled by smoothing factor k
// When k = 0, this behaves identically to max(a, b)
float smoothMax(float a, float b, float k) {
	 k = min(0, -k);
	 float h = max(0, min(1, (b - a + k) / (2 * k)));
	 return a * h + b * (1 - h) - k * h * (1 - h);
}

float Blend(float startHeight, float blendDst, float height) {
	 return smoothstep(startHeight - blendDst / 2, startHeight + blendDst / 2, height);
}

float square(float x)
{
    return x * x;
}

// Returns vector (dstToSphere, dstThroughSphere)
// If ray origin is inside sphere, dstToSphere = 0
// If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir) {
	float3 offset = rayOrigin - sphereCentre;
	//float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
	float b = dot(offset, rayDir);
    float d = b * b + sphereRadius * sphereRadius - dot(offset, offset); // Discriminant from quadratic formula

	// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
	if (d > 0) {
		float s = sqrt(d);
		float dstToSphereNear = max(0, -b - s);
		float dstToSphereFar = -b + s;

		// Ignore intersections that occur behind the ray
		if (dstToSphereFar >= 0) {
			return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
		}
	}
	// Ray did not intersect sphere
    return float2(3.402823466e+38, 0);
}

void raySphere_float2(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir, out float2 output)
{
    output = raySphere(sphereCentre, sphereRadius, rayOrigin, rayDir);
}


float3 blend_rnm(float3 n1, float3 n2)
{
    n1.z += 1;
    n2.xy = -n2.xy;

    return n1 * dot(n1, n2) / n1.z - n2;
}


float3 UnpackNormal_(float4 packednormal)
{
    //This do the trick
    packednormal.x *= packednormal.w;

    float3 normal;
    normal.xy = packednormal.xy * 2 - 1;
    normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}


// Sample normal map with triplanar coordinates
// Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
// Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap)
{
    float3 absNormal = abs(normal);

	// Calculate triplanar blend
    float3 blendWeight = saturate(pow(normal, 4));
	// Divide blend weight by the sum of its components. This will make x + y + z = 1
    blendWeight /= dot(blendWeight, 1);

	// Calculate triplanar coordinates
    float2 uvX = vertPos.zy * scale.xy + offset;
    float2 uvY = vertPos.xz * scale.xy + offset;
    float2 uvZ = vertPos.xy * scale.xy + offset;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
    float3 tangentNormalX = UnpackNormal_(tex2D(normalMap, uvX));
    float3 tangentNormalY = UnpackNormal_(tex2D(normalMap, uvY));
    float3 tangentNormalZ = UnpackNormal_(tex2D(normalMap, uvZ));

	// Swizzle normals to match tangent space and apply reoriented normal mapping blend
    tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
    tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
    tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

	// Apply input normal sign to tangent space Z
    float3 axisSign = sign(normal);
    tangentNormalX.z *= axisSign.x;
    tangentNormalY.z *= axisSign.y;
    tangentNormalZ.z *= axisSign.z;

	// Swizzle tangent normals to match input normal and blend together
    float3 outputNormal = normalize(
		tangentNormalX.zyx * blendWeight.x +
		tangentNormalY.xzy * blendWeight.y +
		tangentNormalZ.xyz * blendWeight.z
	);

    return outputNormal;
}


float3 linear_srgb_to_oklab(float3 c)
{
    float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
    float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
    float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

    float l_ = pow(l, 0.3333333333f);
    float m_ = pow(m, 0.3333333333f);
    float s_ = pow(s, 0.3333333333f);

    return float3(
        0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
        1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
        0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_);
}

float3 oklab_to_linear_srgb(float3 c)
{
    float l_ = c.x + 0.3963377774f * c.y + 0.2158037573f * c.z;
    float m_ = c.x - 0.1055613458f * c.y - 0.0638541728f * c.z;
    float s_ = c.x - 0.0894841775f * c.y - 1.2914855480f * c.z;

    float l = l_ * l_ * l_;
    float m = m_ * m_ * m_;
    float s = s_ * s_ * s_;

    return float3(
        +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
		-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
		-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
}