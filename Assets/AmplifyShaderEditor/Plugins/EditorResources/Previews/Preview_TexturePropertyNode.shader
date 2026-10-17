Shader "Hidden/TexturePropertyNode"
{
	Properties
	{
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
					case 4: result = bump; break;
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

			UNITY_DECLARE_TEX2DARRAY (_Array);
			samplerCUBE _Cube;
			sampler2D _Sampler;
			sampler3D _Sampler3D;
			int _Type;

			float4 frag (v2f_img i) : SV_Target
			{
				if (_Type == 4)
				{
					return UNITY_SAMPLE_TEX2DARRAY (_Array, float3(i.uv, 0));
				}
				else if (_Type == 3)
				{
					return texCUBE (_Cube, float3(i.uv,0));
				}
				else if (_Type == 2)
				{
					return tex3D (_Sampler3D, float3(i.uv,0));
				}
				else
				{
					return tex2D (_Sampler, i.uv);
				}
			}
			ENDCG
		}
	}
}
