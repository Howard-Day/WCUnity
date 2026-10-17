Shader "Hidden/StaticSwitchNode"
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
		_H ("_H", 2DArray) = "white" {}
		_I ("_I", 2DArray) = "white" {}
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
			ASE_PORT( _I );
			int _Condition;

			float4 frag( v2f_img i ) : SV_Target
			{
				if( _Condition == 0)
					return ASE_SAMPLE_PORT( _B, i.uv ); // A nd B are switched
				else if( _Condition == 1 )
					return ASE_SAMPLE_PORT( _A, i.uv );
				else if( _Condition == 2 )
					return ASE_SAMPLE_PORT( _C, i.uv );
				else if( _Condition == 3 )
					return ASE_SAMPLE_PORT( _D, i.uv );
				else if( _Condition == 4 )
					return ASE_SAMPLE_PORT( _E, i.uv );
				else if( _Condition == 5 )
					return ASE_SAMPLE_PORT( _F, i.uv );
				else if( _Condition == 6 )
					return ASE_SAMPLE_PORT( _G, i.uv );
				else if( _Condition == 7 )
					return ASE_SAMPLE_PORT( _H, i.uv );
				else
					return ASE_SAMPLE_PORT( _I, i.uv );
			}
			ENDCG
		}
	}
}
