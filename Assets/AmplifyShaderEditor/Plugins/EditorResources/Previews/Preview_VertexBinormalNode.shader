Shader "Hidden/VertexBinormalNode"
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
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal(normal);
					
				float3 tangent = PreviewFragmentTangentOS( i.uv );
				float3 worldPos = mul(unity_ObjectToWorld, vertexPos).xyz;
				float3 worldTangent = UnityObjectToWorldDir(tangent);
				float tangentSign = -1;
				float3 worldBinormal = normalize( cross(worldNormal, worldTangent) * tangentSign);
					
				return float4(worldBinormal, 1);
			}
			ENDCG
		}
	}
}
