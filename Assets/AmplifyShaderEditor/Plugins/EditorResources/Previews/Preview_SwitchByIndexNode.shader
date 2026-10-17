Shader "Hidden/SwitchByIndexNode"
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
		_J ("_J", 2DArray) = "white" {}
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

			#define MAX_CASES 8

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );
			ASE_PORT( _H );
			ASE_PORT( _I );
			ASE_PORT( _J );
			int _Amount;
			float _Cases[MAX_CASES];

			float4 frag( v2f_img i ) : SV_Target
			{
				int index = ( int )ASE_SAMPLE_PORT( _A, i.uv ).r;

				// @diogo: walked backwards so a repeated case resolves to its first input, like the generated switch
				int slot = -1;
				for( int c = MAX_CASES - 1; c >= 0; c-- )
				{
					if( c < _Amount && index == ( int )_Cases[ c ] )
						slot = c;
				}

				if( slot == 0 )
					return  ASE_SAMPLE_PORT( _C, i.uv );
				else if( slot == 1 )
					return  ASE_SAMPLE_PORT( _D, i.uv );
				else if( slot == 2 )
					return  ASE_SAMPLE_PORT( _E, i.uv );
				else if( slot == 3 )
					return  ASE_SAMPLE_PORT( _F, i.uv );
				else if( slot == 4 )
					return  ASE_SAMPLE_PORT( _G, i.uv );
				else if( slot == 5 )
					return  ASE_SAMPLE_PORT( _H, i.uv );
				else if( slot == 6 )
					return  ASE_SAMPLE_PORT( _I, i.uv );
				else if( slot == 7 )
					return  ASE_SAMPLE_PORT( _J, i.uv );
				else
					return  ASE_SAMPLE_PORT( _B, i.uv );
			}
			ENDCG
		}
	}
}
