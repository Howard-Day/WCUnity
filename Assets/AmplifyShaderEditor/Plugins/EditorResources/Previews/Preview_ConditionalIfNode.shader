Shader "Hidden/Preview_ConditionalIfNode"
{
	Properties
	{
		_A ( "_A", 2DArray) = "white" {}
		_B ( "_B", 2DArray ) = "white" {}
		_C ( "_AGreaterThanB", 2DArray ) = "white" {}
		_D ( "_AEqualToB", 2DArray ) = "white" {}
		_E ( "_ALessThanB", 2DArray ) = "white" {}
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

			float4 frag ( v2f_img i ) : SV_Target
			{
				float aVal = ASE_SAMPLE_PORT( _A, i.uv ).r;
				float bVal = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float4 aGreaterbVal = ASE_SAMPLE_PORT( _C, i.uv );
				float4 aEqualbVal = ASE_SAMPLE_PORT( _D, i.uv );
				float4 aLessbVal = ASE_SAMPLE_PORT( _E, i.uv );
				
				if ( aVal > bVal )
					return aGreaterbVal;

				if ( aVal == bVal )
					return aEqualbVal;

				return aLessbVal;
				
			}
			ENDCG
		}
	}
}
