Shader "Hidden/ParallaxMappingNode"
{
	Properties
	{
		_A ("_UV", 2DArray) = "white" {}
		_B ("_Height", 2DArray) = "white" {}
		_C ("_Scale", 2DArray) = "white" {}
		_D ("_ViewDirTan", 2DArray) = "white" {}
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
			float _ParallaxType;

			float4 frag(v2f_img i) : SV_Target
			{
				float2 uv = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				float h = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float s = ASE_SAMPLE_PORT( _C, i.uv ).r;
				float3 vt = ASE_SAMPLE_PORT( _D, i.uv ).xyz;
				float2 parallaxed = uv;
				if ( _ParallaxType == 1 ) {
					parallaxed = ( ( h - 1 )*( vt.xy / vt.z ) * s ) + uv;
				}
				else {
					parallaxed = ( ( h - 1 )*( vt.xy ) * s ) + uv;
				}

				return float4(parallaxed, 0 , 0);
			}
			ENDCG
		}
	}
}
