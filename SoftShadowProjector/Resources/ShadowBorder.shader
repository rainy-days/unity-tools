﻿Shader "Hidden/RainyDays/ShadowBorder" {
  SubShader {
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
     
      struct v2f {
          float4 pos : SV_POSITION;
          fixed4 color : COLOR;
      };
      
      v2f vert (
		float4 vertex : POSITION,
		fixed4 color : COLOR)
      {
          v2f o;
          o.pos = mul (UNITY_MATRIX_MVP, vertex);
          o.color = color;
          return o;
      }

      fixed4 frag (v2f i) : SV_Target { return i.color; }
      ENDCG
    }
  } 
}