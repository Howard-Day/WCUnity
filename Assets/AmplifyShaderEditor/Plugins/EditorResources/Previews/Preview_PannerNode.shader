Shader "Hidden/PannerNode"
{
	Properties
	{
		_A ("_UVs", 2DArray) = "white" {}
		_B ("_PanTime", 2DArray) = "white" {}
		_C ("_PanSpeed", 2DArray ) = "white" {}
	}
	SubShader
	{
		Pass
		{
			Name "Panner" // 14 - UV panner node
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
				float multiplier = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float time = _EditorTime*multiplier;
				if ( _UsingEditor == 0 ) 
				{
					time = multiplier;
				}
				float2 speed = ASE_SAMPLE_PORT( _C, i.uv ).rg;
				return ASE_SAMPLE_PORT( _A, i.uv) + time * float4( speed, 0, 0 );
			}
			ENDCG
		}
	}
}
