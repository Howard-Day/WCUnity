Shader "Hidden/SimpleMaxOp"
{
	Properties
	{
		_A( "_A", 2DArray ) = "white" {}
		_B( "_B", 2DArray ) = "white" {}
		_C( "_C", 2DArray ) = "white" {}
		_D( "_D", 2DArray ) = "white" {}
		_E( "_E", 2DArray ) = "white" {}
		_F( "_F", 2DArray ) = "white" {}
		_G( "_G", 2DArray ) = "white" {}
		_H( "_H", 2DArray ) = "white" {}
		_I( "_I", 2DArray ) = "white" {}
		_J( "_J", 2DArray ) = "white" {}
		_Count( "_Count", Int ) = 0
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
			ASE_PORT( _J );
			int _Count;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 final = max( a , b );

				if ( _Count > 2 )
					final = max( final, ASE_SAMPLE_PORT( _C, i.uv ) );
				if ( _Count > 3 )
					final = max( final, ASE_SAMPLE_PORT( _D, i.uv ) );
				if ( _Count > 4 )
					final = max( final, ASE_SAMPLE_PORT( _E, i.uv ) );
				if ( _Count > 5 )
					final = max( final, ASE_SAMPLE_PORT( _F, i.uv ) );
				if ( _Count > 6 )
					final = max( final, ASE_SAMPLE_PORT( _G, i.uv ) );
				if ( _Count > 7 )
					final = max( final, ASE_SAMPLE_PORT( _H, i.uv ) );
				if ( _Count > 8 )
					final = max( final, ASE_SAMPLE_PORT( _I, i.uv ) );
				if ( _Count > 9 )
					final = max( final, ASE_SAMPLE_PORT( _J, i.uv ) );

				return final;
			}
			ENDCG
		}
	}
}
