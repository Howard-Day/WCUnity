Shader "Hidden/BlendNormalsNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
	}
	SubShader
	{
		CGINCLUDE
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "UnityStandardUtils.cginc"
		
			float3 BlendNormalWorldspaceRNM(float3 n1, float3 n2, float3 vtxNormal)
			{
				float4 q = float4(cross(vtxNormal, n2), dot(vtxNormal, n2) + 1.0) / sqrt(2.0 * (dot(vtxNormal, n2) + 1));
				return n1 * (q.w * q.w - dot(q.xyz, q.xyz)) + 2 * q.xyz * dot(q.xyz, n1) + 2 * q.w * cross(q.xyz, n1);
			}

			float3 BlendNormalRNM(float3 n1, float3 n2)
			{
				float3 t = n1.xyz + float3(0.0, 0.0, 1.0);
				float3 u = n2.xyz * float3(-1.0, -1.0, 1.0);
				float3 r = (t / t.z) * dot(t, u) - u;
				return r;
			}
		ENDCG
		Pass
		{
			CGPROGRAM

			ASE_PORT( _A );
			ASE_PORT( _B );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 a = ASE_SAMPLE_PORT( _A, i.uv ).rgb;
				float3 b = ASE_SAMPLE_PORT( _B, i.uv ).rgb;
				return float4(BlendNormals(a, b), 0);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			ASE_PORT( _A );
			ASE_PORT( _B );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 a = ASE_SAMPLE_PORT( _A, i.uv).rgb;
				float3 b = ASE_SAMPLE_PORT( _B, i.uv).rgb;
				return float4(BlendNormalRNM(a, b), 0);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag(v2f_img i) : SV_Target
			{
				float3 a = ASE_SAMPLE_PORT( _A, i.uv).rgb;
				float3 b = ASE_SAMPLE_PORT( _B, i.uv).rgb;
				float3 c = ASE_SAMPLE_PORT( _C, i.uv).rgb;
				return float4(BlendNormalWorldspaceRNM(a,b,c), 0);
			}
			ENDCG
		}
	}
}
