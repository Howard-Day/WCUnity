Shader "Hidden/MainLight"
{
	SubShader
	{
		CGINCLUDE
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Preview.cginc"
		ENDCG

		// DIRECTION
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return float4( preview_EditorLightDirection, 0 );
				}
			ENDCG
		}

		// COLOR HDR
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return float4( preview_EditorLightColor * preview_EditorLightIntensity, 0 );
				}
			ENDCG
		}

		// COLOR LDR
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return float4( preview_EditorLightColor, 0 );
				}
			ENDCG
		}

		// INTENSITY
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					return preview_EditorLightIntensity;
				}
			ENDCG
		}

		// SHADOW ATTENUATION
		Pass
		{
			CGPROGRAM
				float4 frag( v2f_img i ) : SV_Target
				{
					float3 vertexPos = PreviewFragmentPositionOS( i.uv );
					float3 normal = PreviewFragmentNormalOS( i.uv );
					float3 worldNormal = UnityObjectToWorldNormal( normal );
					return saturate( dot( worldNormal , preview_EditorLightDirection ) * 10 + 0.1 );
				}
			ENDCG
		}
	}
}
