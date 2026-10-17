Shader "Hidden/IsNaN"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );

			bool4 IsNaN( float4 x )
			{
				const float zero = float4( 0.0f, 0.0f, 0.0f, 0.0f );
				return !( x < zero || x > zero || x == zero );
			}

			float4 frag(v2f_img i) : SV_Target
			{
				// for some reason, isnan() wasn't working in preview shaders
				return IsNaN( ASE_SAMPLE_PORT( _A, i.uv) ) ? 1 : 0;
			}
			ENDCG
		}
	}
}
