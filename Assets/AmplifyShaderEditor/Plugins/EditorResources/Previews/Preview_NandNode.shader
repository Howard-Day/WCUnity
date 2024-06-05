Shader "Hidden/AllNode"
{
	Properties
	{
		_A ("_A", 2D) = "white" {}
		_B ("_B", 2D) = "white" {}
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
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				return ( !tex2D(_A, i.uv) && !tex2D(_B, i.uv) ) ? ( 1 ).xxxx : ( 0 ).xxxx;
			}
			ENDCG
		}
	}
}
