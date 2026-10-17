Shader "Hidden/DecodeDepthNormalNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
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
		

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float depthValue = 0;
				float3 normalValue = 0;
				DecodeDepthNormal( a , depthValue, normalValue );
				return float4( depthValue,normalValue );
			}
			ENDCG
		}
	}
}
