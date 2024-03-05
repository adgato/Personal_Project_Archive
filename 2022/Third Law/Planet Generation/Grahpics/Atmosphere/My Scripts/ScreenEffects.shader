Shader "Custom/ScreenEffects"
{
    Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		
	}
    SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Math.cginc"
			#include "PerlinNoise.cginc"
			//

			struct appdata {
					float4 vertex : POSITION;
					float4 uv : TEXCOORD0;
			};

			struct v2f {
					float4 pos : SV_POSITION;
					
					float2 uv : TEXCOORD0;
					float3 viewVector : TEXCOORD1;
			};

			v2f vert (appdata v) {
					v2f output;
					output.pos = UnityObjectToClipPos(v.vertex);

					output.uv = v.uv;
					// Camera space matches OpenGL convention where cam forward is -z. In unity forward is positive z.
					// (https://docs.unity3d.com/ScriptReference/Camera-cameraToWorldMatrix.html)
					float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv.xy * 2 - 1, 0, -1));
					output.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));
					return output;
			}

			float2 squareUV(float2 uv) {
				float width = _ScreenParams.x;
				float height =_ScreenParams.y;
				//float minDim = min(width, height);
				float scale = 1000;
				float x = uv.x * width;
				float y = uv.y * height;
				return float2 (x/scale, y/scale);
			}

            sampler2D _MainTex;

            float danger01;
            float blur01;
            float dark01;
            float temperature01; //hot = 1, i wish this was the convention throughout my code but alas...
            float4 linesCol;

            float2 group;
            float3 forward;

            float3 target;
            float3 b1;
            float3 b2;
            float3 b3;
            float3 b4;
            float3 b5;
            float3 b6;
            float3 b7;
            float3 b8;

            //Convert world pos to uv
            float2 w(float3 pos) 
            {
                pos = normalize(pos - _WorldSpaceCameraPos) * (_ProjectionParams.y + _ProjectionParams.z - _ProjectionParams.y) + _WorldSpaceCameraPos;
                fixed2 uv = 0;
                fixed3 toCam = mul(unity_WorldToCamera, pos);
                fixed camPosZ = toCam.z;
                fixed height = 2 * camPosZ / unity_CameraProjection._m11;
                fixed width = _ScreenParams.x / _ScreenParams.y * height;
                uv.x = (toCam.x + 0.5 * width) / width;
                uv.y = (toCam.y + 0.5 * height) / height;
                return (float2)uv;
            }

            //Find the minimum and maximum X and Y UV values for each of the bounds of the target to highlight
            float4 DetectionXYMinMax()
            {
                float minX = min(min(min(min(min(min(min(w(b1).x, w(b2).x), w(b3).x), w(b4).x), w(b5).x), w(b6).x), w(b7).x), w(b8).x);
                float maxX = max(max(max(max(max(max(max(w(b1).x, w(b2).x), w(b3).x), w(b4).x), w(b5).x), w(b6).x), w(b7).x), w(b8).x);
                float minY = min(min(min(min(min(min(min(w(b1).y, w(b2).y), w(b3).y), w(b4).y), w(b5).y), w(b6).y), w(b7).y), w(b8).y);
                float maxY = max(max(max(max(max(max(max(w(b1).y, w(b2).y), w(b3).y), w(b4).y), w(b5).y), w(b6).y), w(b7).y), w(b8).y);
                return float4(minX, maxX, minY, maxY);
            }

            float4 DrawDetection(float4 originalCol, float4 squareCol, float2 uv)
            {
                float4 uvBounds = DetectionXYMinMax();

                //Pixalate bounds
                uvBounds.xz = floor(uvBounds.xz * group) / group;
                uvBounds.yw = floor(uvBounds.yw * group) / group;

                if (dot(normalize(target - _WorldSpaceCameraPos), forward) < 0)
                    return originalCol;

                if ((uv.x  == uvBounds.x || uv.x  == uvBounds.y) && uv.y >= uvBounds.z && uv.y <= uvBounds.w ||
                    (uv.y  == uvBounds.z || uv.y  == uvBounds.w) && uv.x >= uvBounds.x && uv.x <= uvBounds.y)
                    return lerp(originalCol, squareCol, 0.1);

                return originalCol;
            }

            float nrand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 frag (v2f i) : SV_Target
			{
                forward = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                blur01 =  pow(blur01, 0.5);

                float2 temperatureUV;
                //1 is hot, 0 is cold, offset UV based to evoke temperature (heat waves & shaking for hot & cold)
                if (temperature01 > 0.5) 
                    temperatureUV = i.uv + 0.01 * pow(clamp(remap01(0.5, 1, temperature01), 0, 1), 3) * float2(PerlinNoise3D(float3(3 * i.uv, _Time.w)), 0.2 * PerlinNoise3D(float3(3 * i.uv, -1 - _Time.w)));
                else
                    temperatureUV = i.uv + 0.0005 * pow(clamp(remap01(0.5, 0, temperature01), 0, 1), 3) * float2(floor(_Time.w * unity_DeltaTime.w) % 2 * 2 - 1, 0.2 * (floor(_Time.w * unity_DeltaTime.w) % 2 * 2 - 1));
                    
                temperatureUV = float2(clamp(temperatureUV.x, 0, 1), clamp(temperatureUV.y, 0, 1));
                //Sample original color from texture and apply pixalating and darkening effect
                float4 originalCol = tex2D(_MainTex, floor(temperatureUV * _ScreenParams * blur01) / (_ScreenParams * blur01)) * pow(dark01, 0.15);

                int pixelWidth = 6;
                group = _ScreenParams / pixelWidth * blur01;
                float2 modulatedUV = floor(i.uv * group) / group;
                
                float length01 = remap01(lerp(0.55, 0.75, 1 - danger01), 1, length(modulatedUV - float2(0.5, 0.5)) * 1.41);

                pixelWidth = floor(lerp(pixelWidth * 0.5, 6, length01) * 2) * 0.5;

                float2 localGroup = _ScreenParams / pixelWidth * blur01;
                float2 localModulatedUV = floor(temperatureUV * localGroup) / localGroup;
                
                float2 seed = 10 * localModulatedUV;

                float pixelNoise1 = nrand(seed * forward.xy);
                float pixelNoise2 = nrand(seed * forward.xz);
                float pixelNoise3 = nrand(seed * forward.yz);
                float4 pixelNoise = float4(pixelNoise1, pixelNoise2, pixelNoise3, 1) * linesCol;

                float border01 = clamp(length(localModulatedUV - float2(0.5, 0.5)) * 1.41 - 0.1 * pixelNoise1 - 0.25, 0, 1);

                float borderLerp = 1 - clamp(danger01 + 0.2 * sin(_Time.w) - 0.2, 0, 1);

                originalCol = DrawDetection(originalCol, pixelNoise, modulatedUV);

                //Close to centre of screen return originalCol (with blur (pixelate) and dark and temperature effects applied)
                if (border01 < lerp(0.25, 0.45, borderLerp))
                    return originalCol;

                float4 modulatedCol = tex2D(_MainTex, localModulatedUV);

                //Further out return modulated (even more pixalated) originalCol
                if (border01 < lerp(0.35, 0.45, borderLerp) + 0.05)
                    return lerp(originalCol, pixelNoise, 0.01 * border01);

                //Apply moving grid lines to screen border

                float4 newCol = lerp(modulatedCol, pixelNoise, 0.01 * border01);

                uint2 pixel = (uint2)floor(i.uv * group * blur01);
                uint n = (uint)lerp(15, 20, danger01);
                uint timeMod = (uint)(_Time.x * lerp(100, 200, danger01) + pixelNoise1) % n;
                uint xTimeMod = timeMod;
                uint yTimeMod = timeMod;
                if (i.uv.x < 0.5)
                    xTimeMod = n - timeMod - 1;
                if (i.uv.y < 0.5)
                    yTimeMod = n - timeMod - 1;

                if (pixel.x % n == xTimeMod || pixel.y % n == yTimeMod)
                    newCol = lerp(newCol, linesCol, 0.01);

                return newCol * pow(lerp(1 - length01, 1, 1 - danger01), 1);
            }

            ENDCG
        }
    }
}
