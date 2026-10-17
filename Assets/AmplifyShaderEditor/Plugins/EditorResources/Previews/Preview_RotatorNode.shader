Shader "Hidden/RotatorNode"
{
	Properties
	{
		_A ("_UVs", 2DArray) = "white" {}
		_B ("_Anchor", 2DArray) = "white" {}
		_C ("_RotTimeTex", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			float _UsingEditor;
			float _EditorTime;

			float4 frag(v2f_img i) : SV_Target
			{
				float multiplier = ASE_SAMPLE_PORT( _C, i.uv ).r;
				float time = _EditorTime*multiplier;
				
				if ( _UsingEditor == 0 ) 
				{
					time = multiplier;
				}

				float cosT = cos( time );
				float sinT = sin( time );

				float2 a = ASE_SAMPLE_PORT( _B, i.uv ).rg;
				return float4( mul( ASE_SAMPLE_PORT( _A, i.uv ).xy - a, float2x2( cosT, -sinT, sinT, cosT ) ) + a, 0, 1 );
			}
			ENDCG
		}
	}
}
