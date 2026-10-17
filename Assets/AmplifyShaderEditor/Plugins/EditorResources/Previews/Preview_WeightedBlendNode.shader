Shader "Hidden/WeightedBlendNode"
{
	Properties
	{
		_A ( "_Weights", 2DArray) = "white" {}
		_B ( "_Layer1", 2DArray) = "white" {}
		_C ( "_Layer2", 2DArray ) = "white" {}
		_D ( "_Layer3", 2DArray ) = "white" {}
		_E ( "_Layer4", 2DArray ) = "white" {}
	}

	SubShader
	{

		CGINCLUDE
		
		#include "UnityCG.cginc"
			#include "Preview.cginc"
		#pragma vertex vert_img
		#pragma fragment frag
			#pragma target 3.5
		ASE_PORT( _A );
		ASE_PORT( _B );
		
		ENDCG

		Pass
		{
			CGPROGRAM
			float4 frag(v2f_img i) : SV_Target
			{
				float4 Layer1 = ASE_SAMPLE_PORT( _B, i.uv );
				return Layer1;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			ASE_PORT( _C );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _C, i.uv );
				return ( Weights.x*Layer1 + Weights.y*Layer2 ) / ( Weights.x + Weights.y );
			}
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			ASE_PORT( _C );
			ASE_PORT( _D );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _C, i.uv );
				float4 Layer3 = ASE_SAMPLE_PORT( _D, i.uv );
				return ( Weights.x*Layer1 + Weights.y*Layer2 + Weights.z*Layer3 ) / ( Weights.x + Weights.y + Weights.z );
			}
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _C, i.uv );
				float4 Layer3 = ASE_SAMPLE_PORT( _D, i.uv );
				float4 Layer4 = ASE_SAMPLE_PORT( _E, i.uv );
				return ( Weights.x*Layer1 + Weights.y*Layer2 + Weights.z*Layer3 + Weights.w*Layer4 ) / ( Weights.x + Weights.y + Weights.z + Weights.w );
			}
			ENDCG
		}
	}
}
