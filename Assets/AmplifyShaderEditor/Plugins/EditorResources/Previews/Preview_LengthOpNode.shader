Shader "Hidden/LengthOpNode"
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

			float4 frag (v2f_img i) : SV_Target
			{
				return length( ASE_SAMPLE_PORT( _A, i.uv).x );
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
				return length( ASE_SAMPLE_PORT( _A, i.uv).xy );
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
				return length( ASE_SAMPLE_PORT( _A, i.uv).xyz );
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
				return length( ASE_SAMPLE_PORT( _A, i.uv));
			}
			ENDCG
		}
	}
}
