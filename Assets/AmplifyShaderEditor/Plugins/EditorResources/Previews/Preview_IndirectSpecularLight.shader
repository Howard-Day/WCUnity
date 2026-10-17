Shader "Hidden/IndirectSpecularLight"
{
	Properties
	{
		_Skybox("_Skybox", CUBE) = "white" {}
		_A ("Normal", 2DArray) = "white" {}
		_B ("Smoothness", 2DArray) = "white" {}
		_C ("Occlusion", 2DArray) = "white" {}
	}

	SubShader
	{
		Pass // not connected
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = normalize( reflect( -worldViewDir, worldNormal ) );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( ASE_SAMPLE_PORT( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * ASE_SAMPLE_PORT( _C, i.uv ).r, 1 );
			}
			ENDCG
		}

		Pass // connected tangent
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 tangentNormal = ASE_SAMPLE_PORT( _A, PreviewFragmentSphericalUV( i.uv ) ).xyz;
				float3 worldNormal = PreviewFragmentTangentToWorldDir( i.uv, tangentNormal, true );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = reflect( -worldViewDir, worldNormal );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( ASE_SAMPLE_PORT( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * ASE_SAMPLE_PORT( _C, i.uv ).r, 1 );
			}
			ENDCG
		}

		Pass // connected world
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			uniform samplerCUBE _Skybox;
			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = ASE_SAMPLE_PORT( _A, i.uv );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float3 worldRefl = reflect( -worldViewDir, worldNormal );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, ( 1 - saturate( ASE_SAMPLE_PORT( _B, i.uv ).r ) ) * 6 ) ).rgb;

				return float4( sky * ASE_SAMPLE_PORT( _C, i.uv ).r, 1 );
			}
			ENDCG
		}
	}
}
