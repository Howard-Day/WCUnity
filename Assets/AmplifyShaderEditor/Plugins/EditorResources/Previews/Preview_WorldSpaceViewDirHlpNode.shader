Shader "Hidden/WorldSpaceViewDirHlpNode"
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
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			
			ASE_PORT( _A );

			float4 frag( v2f_img i ) : SV_Target
			{
				return float4( float3( 0,0,-5 ) - ASE_SAMPLE_PORT( _A, i.uv ).rgb,1);
			}
			ENDCG
		}
	}
}
