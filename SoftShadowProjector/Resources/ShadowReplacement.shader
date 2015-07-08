Shader "Hidden/RainyDays/ShadowReplacement" {
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Blend One Zero
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			fixed4 _ShadowColor;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 depth : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.depth = float4(COMPUTE_DEPTH_01, 0,0,0);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// red: crisp
				// green: unused
				// blue: blurred
				// alpha: depth
				return fixed4(0, 0, 0, i.depth.x);
			}

			ENDCG
		}
	}
}
