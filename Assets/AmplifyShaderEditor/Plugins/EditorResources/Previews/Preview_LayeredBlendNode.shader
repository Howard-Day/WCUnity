Shader "Hidden/LayeredBlendNode"
{
	Properties
	{
		_A ( "_Weights", 2DArray) = "white" {}
		_B ( "_LayerBase", 2DArray) = "white" {}
		_C ( "_Layer1", 2DArray) = "white" {}
		_D ( "_Layer2", 2DArray ) = "white" {}
		_E ( "_Layer3", 2DArray ) = "white" {}
		_F ( "_Layer4", 2DArray ) = "white" {}
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
		ASE_PORT( _C );
		
		ENDCG

		Pass
		{
			CGPROGRAM
			float4 frag(v2f_img i) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 LayerBase = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _C, i.uv );
				return lerp ( LayerBase, Layer1, Weights.x );
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			ASE_PORT( _D );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 LayerBase = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _C, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _D, i.uv );
				return lerp ( lerp ( LayerBase, Layer1, Weights.x ), Layer2, Weights.y );
			}
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			ASE_PORT( _D );
			ASE_PORT( _E );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 LayerBase = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _C, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _D, i.uv );
				float4 Layer3 = ASE_SAMPLE_PORT( _E, i.uv );
				return lerp ( lerp ( lerp ( LayerBase, Layer1, Weights.x ), Layer2, Weights.y ), Layer3, Weights.z );
			}
			ENDCG
		}
		
		Pass
		{
			CGPROGRAM
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			float4 frag ( v2f_img i ) : SV_Target
			{
				float4 Weights = ASE_SAMPLE_PORT( _A, i.uv );
				float4 LayerBase = ASE_SAMPLE_PORT( _B, i.uv );
				float4 Layer1 = ASE_SAMPLE_PORT( _C, i.uv );
				float4 Layer2 = ASE_SAMPLE_PORT( _D, i.uv );
				float4 Layer3 = ASE_SAMPLE_PORT( _E, i.uv );
				float4 Layer4 = ASE_SAMPLE_PORT( _F, i.uv );
				return lerp ( lerp ( lerp ( lerp ( LayerBase, Layer1, Weights.x ), Layer2, Weights.y ), Layer3, Weights.z ), Layer4, Weights.w );
			}
			ENDCG
		}
	}
}
