Shader "Hidden/RainyDays/ShadowBlur" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
	
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

        Pass {
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

            #include "UnityCG.cginc"
             
            uniform sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;
			uniform half _Spread;

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv20 : TEXCOORD0;
				half2 uv21 : TEXCOORD1;
				half2 uv22 : TEXCOORD2;
				half2 uv23 : TEXCOORD3;
				
				// TODO: optimize
				half2 uvCenter : TEXCOORD4;
			};

			v2f vert ( appdata_img v )
			{
				v2f o;

				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

				half2 halfPixelSize = _MainTex_TexelSize.xy * 0.5h;
				half2 dUV = ( _MainTex_TexelSize.xy * half2( _Spread, _Spread ) ) + halfPixelSize.xy;

        		o.uv20 = v.texcoord + half2(-dUV.x, dUV.y);
				o.uv21 = v.texcoord + half2(dUV.x, dUV.y);
				o.uv22 = v.texcoord + half2(dUV.x, -dUV.y);
				o.uv23 = v.texcoord + half2(-dUV.x, -dUV.y);
				o.uvCenter = v.texcoord;

				return o; 
			}

            fixed4 frag ( v2f i ) : SV_Target
			{				
				// only blur blue channel, keep others intact
				fixed4 color = tex2D (_MainTex, i.uvCenter);
				color.z = 0.25 * (
					tex2D (_MainTex, i.uv20).z +
					tex2D (_MainTex, i.uv21).z +
					tex2D (_MainTex, i.uv22).z +
					tex2D (_MainTex, i.uv23).z);

				return color;
			}
            ENDCG
        }
    }
}
