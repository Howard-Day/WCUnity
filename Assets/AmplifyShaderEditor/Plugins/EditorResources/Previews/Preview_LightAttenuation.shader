Shader "Hidden/LightAttenuation"
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
			#include "Lighting.cginc"

			float4 _EditorWorldLightPos;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 vertexPos = PreviewFragmentPositionOS( i.uv );
				float3 normal = PreviewFragmentNormalOS( i.uv );
				float3 worldNormal = UnityObjectToWorldNormal( normal );
				float3 lightDir = normalize( _EditorWorldLightPos.xyz );
				return saturate(dot(worldNormal ,lightDir) * 10 + 0.1);
			}
			ENDCG
		}
	}
}
