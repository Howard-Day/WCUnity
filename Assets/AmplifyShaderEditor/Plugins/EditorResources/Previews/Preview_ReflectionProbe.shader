Shader "Hidden/IndirectSpecularLight"
{
	Properties
	{
		_Skybox("_Skybox", CUBE) = "white" {}
		_A ("View Dir OS", 2DArray) = "white" {}
		_B ("Normal OS", 2DArray) = "white" {}
		_C ("LOD", 2DArray) = "white" {}
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

			uint viewDirInput;
			uint normalInput;
			uint lodInput;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );
				float3 worldViewDir = normalize( preview_WorldSpaceCameraPos - vertexPos );
				float lod = 0;

				float2 sphereUV = PreviewFragmentSphericalUV( i.uv );

				if ( viewDirInput != 0 )
				{
					float3 viewDirOS = ASE_SAMPLE_PORT( _A, sphereUV );
					worldViewDir = UnityObjectToWorldDir( viewDirOS );
				}

				if ( normalInput != 0 )
				{
					float3 normalOS = ASE_SAMPLE_PORT( _B, i.uv );
					worldNormal = UnityObjectToWorldNormal( normalOS );
				}

				if ( lodInput != 0 )
				{
					lod = ASE_SAMPLE_PORT( _C, i.uv );
				}

				float3 worldRefl = normalize( reflect( -worldViewDir, worldNormal ) );

				float3 sky = texCUBElod( _Skybox, float4( worldRefl, lod ) ).rgb;

				return float4( sky, 1 );
}
			ENDCG
		}
	}
}
