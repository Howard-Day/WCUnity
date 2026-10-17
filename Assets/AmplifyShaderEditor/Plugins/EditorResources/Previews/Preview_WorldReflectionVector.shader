Shader "Hidden/WorldReflectionVector"
{
	Properties
	{
		_A ("_TangentNormal", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass //not connected
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );

				float3 worldViewDir = normalize(preview_WorldSpaceCameraPos - vertexPos);
				float3 worldRefl = -worldViewDir;
				worldRefl = reflect( worldRefl, worldNormal );
				
				return float4((worldRefl), 1);
			}
			ENDCG
		}

		Pass //connected
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"

			ASE_PORT( _A );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );

				float3 tangent = PreviewFragmentTangentOS( i.uv );
				float3 worldPos = mul(unity_ObjectToWorld, float4(vertexPos,1)).xyz;
				float3 worldViewDir = normalize(preview_WorldSpaceCameraPos - vertexPos);
				
				float3 worldTangent = UnityObjectToWorldDir(tangent);
				float tangentSign = -1;
				float3 worldBinormal = normalize( cross(worldNormal, worldTangent) * tangentSign);
				float4 tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				float4 tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				float4 tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

				float3 worldRefl = -worldViewDir;

				float2 sphereUVs = i.uv;
				sphereUVs.x = atan2(vertexPos.x, -vertexPos.z) / (UNITY_PI) + 0.5;
				
				// Needs further checking
				//float3 tangentNormal = tex2Dlod(_A, float4(sphereUVs,0,0)).xyz;
				float3 tangentNormal = ASE_SAMPLE_PORT( _A, sphereUVs).xyz;

				worldRefl = reflect( worldRefl, half3( dot( tSpace0.xyz, tangentNormal ), dot( tSpace1.xyz, tangentNormal ), dot( tSpace2.xyz, tangentNormal ) ) );
				
				return float4((worldRefl), 1);
			}
			ENDCG
		}
	}
}
