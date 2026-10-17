Shader "Hidden/SimpleMultiplyOpNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
		_C ("_C", 2DArray) = "white" {}
		_D ("_D", 2DArray) = "white" {}
		_E ("_E", 2DArray) = "white" {}
		_F ("_F", 2DArray) = "white" {}
		_G ("_G", 2DArray) = "white" {}
		_H ("_H", 2DArray) = "white" {}
		_I ("_I", 2DArray) = "white" {}
		_J ("_J", 2DArray) = "white" {}
		_Count ("_Count", Int) = 0
	}

	SubShader
	{
		Pass //2
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );

				return a * b;
			}
			ENDCG
		}

		Pass //3
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );

				return a * b * c;
			}
			ENDCG
		}

		Pass //4
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );

				return a * b * c * d;
			}
			ENDCG
		}

		Pass //5
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );

				return a * b * c * d * e;
			}
			ENDCG
		}

		Pass //6
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );
				float4 f = ASE_SAMPLE_PORT( _F, i.uv );

				return a * b * c * d * e * f;
			}
			ENDCG
		}

		Pass //7
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );
				float4 f = ASE_SAMPLE_PORT( _F, i.uv );
				float4 g = ASE_SAMPLE_PORT( _G, i.uv );

				return a * b * c * d * e * f * g;
			}
			ENDCG
		}

		Pass //8
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );
			ASE_PORT( _H );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );
				float4 f = ASE_SAMPLE_PORT( _F, i.uv );
				float4 g = ASE_SAMPLE_PORT( _G, i.uv );
				float4 h = ASE_SAMPLE_PORT( _H, i.uv );

				return a * b * c * d * e * f * g * h;
			}
			ENDCG
		}

		Pass //9
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );
			ASE_PORT( _H );
			ASE_PORT( _I );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );
				float4 f = ASE_SAMPLE_PORT( _F, i.uv );
				float4 g = ASE_SAMPLE_PORT( _G, i.uv );
				float4 h = ASE_SAMPLE_PORT( _H, i.uv );
				float4 is = ASE_SAMPLE_PORT( _I, i.uv );

				return a * b * c * d * e * f * g * h * is;
			}
			ENDCG
		}

		Pass //10
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			ASE_PORT( _C );
			ASE_PORT( _D );
			ASE_PORT( _E );
			ASE_PORT( _F );
			ASE_PORT( _G );
			ASE_PORT( _H );
			ASE_PORT( _I );
			ASE_PORT( _J );

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv );
				float4 b = ASE_SAMPLE_PORT( _B, i.uv );
				float4 c = ASE_SAMPLE_PORT( _C, i.uv );
				float4 d = ASE_SAMPLE_PORT( _D, i.uv );
				float4 e = ASE_SAMPLE_PORT( _E, i.uv );
				float4 f = ASE_SAMPLE_PORT( _F, i.uv );
				float4 g = ASE_SAMPLE_PORT( _G, i.uv );
				float4 h = ASE_SAMPLE_PORT( _H, i.uv );
				float4 is = ASE_SAMPLE_PORT( _I, i.uv );
				float4 j = ASE_SAMPLE_PORT( _J, i.uv );

				return a * b * c * d * e * f * g * h * is * j;
			}
			ENDCG
		}
	}
}
