Shader "Hidden/ShadeVertexLights"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_LightCount( "_LightCount", Int ) = 4
		_IsSpotlight ("_IsSpotlight", Int) = 0
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
			int _LightCount;
			int _IsSpotlight;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 vertexPosition = ASE_SAMPLE_PORT( _A, i.uv );
				float3 vertexNormal = ASE_SAMPLE_PORT( _B, i.uv ).xyz;
				float3 result = ShadeVertexLightsFull (vertexPosition, vertexNormal, _LightCount, (_IsSpotlight > 0));
				return float4(result, 1);
			}
			ENDCG
		}
	}
}
