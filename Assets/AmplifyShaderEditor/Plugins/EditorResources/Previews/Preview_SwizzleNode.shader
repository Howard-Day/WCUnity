Shader "Hidden/SwizzleNode"
{
	Properties
	{
		_A ("_A", 2D) = "white" {}
		_Mask("_Mask", Vector) = (0,0,0,0)
	}
	SubShader
	{
		Pass 
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			int4 _Mask;
			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D(_A, i.uv);
				return float4( a[ _Mask.x ], a[ _Mask.y ], a[ _Mask.z ], a[ _Mask.w ] );
			}
			ENDCG
		}
	}
}
