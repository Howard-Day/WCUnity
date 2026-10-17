Shader "Hidden/WorldPosInputsNode"
{
	CGINCLUDE
		#pragma vertex vert_img
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "Preview.cginc"
	ENDCG

	SubShader
	{
		// Object
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float3 positionOS = PreviewFragmentPositionOS( i.uv );
					return float4( positionOS, 1 );
				}
			ENDCG
		}

		// World
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float3 positionOS = PreviewFragmentPositionOS( i.uv );
					float3 positionWS = mul( unity_ObjectToWorld, positionOS );
					return float4( positionWS, 1 );
				}
			ENDCG
		}

		// Relative World
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float3 positionOS = PreviewFragmentPositionOS( i.uv );
					float3 positionWS = mul( unity_ObjectToWorld, positionOS );
					float3 positionRWS = positionWS - preview_WorldSpaceCameraPos.xyz;
					return float4( positionRWS, 1 );
				}
			ENDCG
		}

		// View
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float3 positionOS = PreviewFragmentPositionOS( i.uv );
					float3 positionVS = UnityObjectToViewPos( positionOS );
					return float4( positionVS, 1 );
				}
			ENDCG
		}
	}
}
