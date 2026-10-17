Shader "Hidden/TextureArrayNode"
{
	Properties
	{
		_A ("_UVs", 2DArray) = "white" {}
		_B ("_Index", 2DArray) = "white" {}
		_C ("_Lod", 2DArray) = "white" {}
		_D ("_NormalScale", 2DArray) = "white" {}
		_G ("_Tex", 2DArray) = "white" {}
		_TexConnected ("_TexConnected", Int) = 0
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "UnityStandardUtils.cginc"

			uniform UNITY_DECLARE_TEX2DARRAY( _Sampler );
			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _G );
			float _CustomUVs;
			float _LodType;
			float _Unpack;
			int _TexConnected;

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 uvs = i.uv;
				if ( _CustomUVs == 1 )
					uvs = ASE_SAMPLE_PORT( _A, i.uv ).xy;

				float n = ASE_SAMPLE_PORT( _D, i.uv ).r;
				float4 c = 0;
				if ( _LodType == 1 ) {
					float lod = ASE_SAMPLE_PORT( _C, i.uv ).r;
					c = UNITY_SAMPLE_TEX2DARRAY_LOD( _Sampler, float3( uvs, ASE_SAMPLE_PORT( _B, i.uv ).r ), lod );
				}
				else if( _TexConnected == 0) {
					c = UNITY_SAMPLE_TEX2DARRAY( _Sampler, float3( uvs, ASE_SAMPLE_PORT( _B, i.uv ).r ) );
				} 
				else {
					c = ASE_SAMPLE_PORT( _G, uvs );
				}

				if ( _Unpack == 1 ) 
				{
					c.rgb = UnpackScaleNormal(c, n);
				} 

				return c;
			}
			ENDCG
		}
	}
}
