Shader "Hidden/SwitchBySRPVersion"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
		_D ("_D", 2DArray) = "white" {}
		_E ("_E", 2DArray) = "white" {}
		_F ("_F", 2DArray) = "white" {}
		_G ("_G", 2DArray) = "white" {}
		_H( "_H", 2DArray ) = "white" {}
		_Condition( "_Condition", Int ) = 0
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
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );
			ASE_PORT( _H );
			int _Condition;

			float4 frag(v2f_img i) : SV_Target
			{
				switch ( _Condition )
				{
					case 1: return ASE_SAMPLE_PORT( _B, i.uv );
					case 2: return ASE_SAMPLE_PORT( _C, i.uv );
					case 3: return ASE_SAMPLE_PORT( _D, i.uv );
					case 4: return ASE_SAMPLE_PORT( _E, i.uv );
					case 5: return ASE_SAMPLE_PORT( _F, i.uv );
					case 6: return ASE_SAMPLE_PORT( _G, i.uv );
					case 7: return ASE_SAMPLE_PORT( _H, i.uv );
				}
				return ASE_SAMPLE_PORT( _A, i.uv );
			}
			ENDCG
		}
	}
}
