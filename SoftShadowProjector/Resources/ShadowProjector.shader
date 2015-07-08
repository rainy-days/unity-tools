Shader "Hidden/RainyDays/ShadowProjector" {
	// Adapted from the ProjectorMultiply shader that ships with Unity
	Properties {
		_ShadowTex ("Cookie", 2D) = "black" {}
		_ShadowTint ("Shadow Tint", Color) = (0.5,0.5,0.5,1)
	}
	Subshader {
		Tags {"Queue"="Transparent"}
		Pass {
			ZWrite Off
			ColorMask RGB
			Blend DstColor Zero
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
			};
			
			struct v2f {
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				float4 pos : SV_POSITION;
			};
			
			// Filled by Unity when using the UnityEngine.Projector component
			float4x4 _Projector;
			float4x4 _ProjectorClip;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uvShadow = mul (_Projector, v.vertex);
				o.uvFalloff = mul (_ProjectorClip, v.vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			sampler2D _ShadowTex;
			float4 _ShadowTint;

			// x=blur step, y=blur slope, z,w=unused
			float4 _ShadowFactors;

			float SmoothStep(float x, float c, float r)
			{
				// x in [0..1], variable
				// c in [0..1], step location
				// r >= 0, slope strength
				// TODO: optimize / find approximation
				float eCR = exp(c*r);
				float eRX = exp(r*x);
				return eRX / (eCR + eRX);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 shadowUV = UNITY_PROJ_COORD(i.uvShadow);
				float4 texS = tex2Dproj (_ShadowTex, shadowUV);
				fixed3 attenTint = fixed3(1,1,1) - _ShadowTint.xyz;
				
				float intensity = texS.x; // red = crisp
				float blurredIntensity = texS.z; // blue = blurred
				float depth = texS.w; // alpha = depth

				float x = UNITY_PROJ_COORD(i.uvFalloff).x; // Near=0, Far=1
				if (x < depth)
				{
					return fixed4(1,1,1,1);
				}
				x -= depth;
				float falloff = saturate(SmoothStep(x, _ShadowFactors.x, _ShadowFactors.y * 25));

				float crispAtten = (1 - intensity);
				float blurredAtten = (1 - blurredIntensity);
				float attenFactor = lerp(crispAtten, blurredAtten, falloff);
				fixed3 atten = attenFactor.xxx * attenTint;
				// todo: falloff based on x
				//atten *= 1 - x;
				fixed4 attenColor = fixed4(atten, 0);

				fixed4 res = 1 - attenColor;

				UNITY_APPLY_FOG_COLOR(i.fogCoord, res, fixed4(1,1,1,1));
				return res;
			}
			ENDCG
		}
	}
}
