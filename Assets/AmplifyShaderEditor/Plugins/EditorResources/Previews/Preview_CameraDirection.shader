Shader "Hidden/CameraDirection"
{
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				return float4( -UNITY_MATRIX_V[ 2 ].xyz, 0 );
			}
			ENDCG
		}
	}
}
