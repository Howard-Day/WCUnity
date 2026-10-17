Shader "Hidden/ComputeScreenPos"
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
				float4 screenPos = ComputeScreenPos(a);
				return screenPos;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );

			float4 frag (v2f_img i) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv);
				float4 screenPos = ComputeScreenPos(a);
				screenPos = screenPos / screenPos.w;
				screenPos.z = (UNITY_NEAR_CLIP_VALUE >= 0) ? screenPos.z : screenPos.z* 0.5 + 0.5;
				return screenPos;
			}
			ENDCG
		}
	}
}
