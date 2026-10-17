Shader "Hidden/ViewDir"
{
	CGINCLUDE
		#include "Preview.cginc"
	ENDCG

	SubShader
	{

		Pass // 0 => Tangent Space
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 worldViewDir = normalize(preview_WorldSpaceCameraPos - vertexPos);

				float3 normal = normalize(vertexPos);
				float3 worldNormal = UnityObjectToWorldNormal(normal);

				float3 tangent = PreviewFragmentTangentOS( i.uv );
				float3 worldPos = mul(unity_ObjectToWorld, float4(vertexPos,1)).xyz;
				float3 worldTangent = UnityObjectToWorldDir(tangent);
				float tangentSign = -1;
				float3 worldBinormal = normalize( cross(worldNormal, worldTangent) * tangentSign);
				float4 tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				float4 tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				float4 tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);

				fixed3 viewDirTan = tSpace0.xyz * worldViewDir.x + tSpace1.xyz * worldViewDir.y + tSpace2.xyz * worldViewDir.z;

				return float4(viewDirTan, 1);
			}
			ENDCG
		}

		Pass // 1 => World Space
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 worldViewDir = PreviewWorldSpaceViewDir( vertexPos, true );

				return float4(worldViewDir, 1);
			}
			ENDCG
		}

		Pass // 2 => Object Space
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 worldViewDir = PreviewWorldSpaceViewDir( vertexPos, false );
				float3 objViewDir = PreviewWorldToObjectDir( worldViewDir, true );

				return float4(objViewDir, 1);
			}
			ENDCG
		}

		Pass // 3 => View Space
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldViewDir = PreviewWorldSpaceViewDir( vertexPos, false );
				float3 viewViewDir = PreviewWorldToViewDir( worldViewDir, true );
	
				return float4(viewViewDir, 1);
			}
			ENDCG
		}

		
	}
}
