Shader "Hidden/RainyDays/ShadowCopy" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader {
		
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }
        
		Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            uniform sampler2D _MainTex;

            fixed4 frag(v2f_img i) : SV_Target
			{
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
