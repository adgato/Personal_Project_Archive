

// Remap a value from one range to another
float remap(float v, float minOld, float maxOld, float minNew, float maxNew) {
	 return saturate(minNew + (v-minOld) * (maxNew - minNew) / (maxOld-minOld));
}

// Remap the components of a vector from one range to another
float4 remap(float4 v, float minOld, float maxOld, float minNew, float maxNew) {
	 return saturate(minNew + (v-minOld) * (maxNew - minNew) / (maxOld-minOld));//
}

// Remap a float value (with a known mininum and maximum) to a value between 0 and 1
float remap01(float minOld, float maxOld, float v) {
	 return saturate((v-minOld) / (maxOld-minOld));
}

// Remap a float2 value (with a known mininum and maximum) to a value between 0 and 1
float2 remap01(float2 minOld, float2 maxOld, float2 v) {
	 return saturate((v-minOld) / (maxOld-minOld));
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


// Returns vector (dstToSphere, dstThroughSphere)
// If ray origin is inside sphere, dstToSphere = 0
// If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir) {
	float3 offset = rayOrigin - sphereCentre;
	float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
	float b = 2 * dot(offset, rayDir);
	float c = dot (offset, offset) - sphereRadius * sphereRadius;
	float d = b * b - 4 * a * c; // Discriminant from quadratic formula

	// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
	if (d > 0) {
		float s = sqrt(d);
		float dstToSphereNear = max(0, (-b - s) / (2 * a));
		float dstToSphereFar = (-b + s) / (2 * a);

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


float3 UnpackNormal(float4 packednormal)
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
    float2 uvX = vertPos.zy * scale + offset;
    float2 uvY = vertPos.xz * scale + offset;
    float2 uvZ = vertPos.xy * scale + offset;

	// Sample tangent space normal maps
	// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
    float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
    float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
    float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

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


