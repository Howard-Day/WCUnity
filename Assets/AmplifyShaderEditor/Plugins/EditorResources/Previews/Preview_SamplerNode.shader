Shader "Hidden/SamplerNode"
{
	Properties
	{
		_B ("_UVs", 2DArray) = "white" {}
		_C ("_Level", 2DArray) = "white" {}
		_F ("_NormalScale", 2DArray) = "white" {}
		_G ("Index", 2DArray) = "white" {}
		_CustomUVs ("_CustomUVs", Int) = 0
		_Unpack ("_Unpack", Int) = 0
		_LodType ("_LodType", Int) = 0

		_Sampler ("_Sampler", 2D) = "white" {}
		_Sampler3D ("_Sampler3D", 3D) = "white" {}
		_Array ("_Array", 2DArray) = "white" {}
		_Cube( "_Cube", CUBE) = "white" {}
		_Default ("_Default", Int) = 0
		_Type ("_Type", Int) = 0
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

			ASE_PORT( _F );
			int _CustomUVs;
			int _Unpack;
			int _Default;

			float4 frag( v2f_img i ) : SV_Target
			{
				const float4 white = float4( 1, 1, 1, 1 );
				const float4 black = float4( 0, 0, 0, 0 );
				const float4 grey = float4( 0.214, 0.214, 0.214, 0.5 ); // sRGB gray
				const float4 bump = float4( 0.5, 0.5, 1, 1 );
				const float4 linearGrey = float4( 0.5, 0.5, 0.5, 0.5 );
				const float4 red = float4( 1, 0, 0, 0 );

				float4 result = 1;
				switch ( _Default )
				{
					case 1: result = white; break;
					case 2: result = black; break;
					case 3: result = grey; break;
					case 4: result = ( _Unpack == 1 ) ? float4( UnpackScaleNormal( bump, ASE_SAMPLE_PORT( _F, i.uv ).r ), bump.a ) : bump; break;
					case 5: result = linearGrey; break;
					case 6: result = red; break;
				}
				return result;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#include "UnityStandardUtils.cginc"

			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _F );
			ASE_PORT( _G );
			int _CustomUVs;
			int _Unpack;
			int _LodType;

			UNITY_DECLARE_TEX2DARRAY (_Array);
			samplerCUBE _Cube;
			sampler2D _Sampler;
			sampler3D _Sampler3D;
			int _Type;

			float4 frag (v2f_img i) : SV_Target
			{
				if (_Type == 4)
				{
					return UNITY_SAMPLE_TEX2DARRAY ( _Array, float3(i.uv, ASE_SAMPLE_PORT( _G, i.uv ).r ) );
				}
				else if (_Type == 3)
				{
					float3 uvs = float3(i.uv,0);

					if (_CustomUVs == 1)
						uvs = ASE_SAMPLE_PORT( _B, i.uv).xyz;

					return texCUBE (_Cube, uvs);
				}
				else if (_Type == 2)
				{
					return tex3D (_Sampler3D, float3(i.uv,0));
				}
				else
				{
					float2 uvs = i.uv;
					float4 c = 0;

					if (_CustomUVs == 1)
						uvs = ASE_SAMPLE_PORT( _B, i.uv).xy;

					if (_LodType == 1)
					{
						float lod = ASE_SAMPLE_PORT( _C, i.uv).r;
						c = tex2Dlod (_Sampler, float4(uvs,0,lod));
					}
					else if (_LodType == 2)
					{
						float bias = ASE_SAMPLE_PORT( _C, i.uv).r;
						c = tex2Dbias (_Sampler, float4(uvs,0,bias));
					}
					else
					{
						c = tex2D (_Sampler, uvs);
					}

					if (_Unpack == 1)
					{
						float nscale = ASE_SAMPLE_PORT( _F, i.uv).r;
						c.rgb = UnpackScaleNormal (c, nscale);
					}

					return c;
				}
			}
			ENDCG
		}
	}
}
