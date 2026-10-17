Shader "Hidden/DotProductOpNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
		_B ("_B", 2DArray) = "white" {}
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
			#include "Preview.cginc"
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.5

			ASE_PORT( _A );
			ASE_PORT( _B );
			int _Type;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = ASE_SAMPLE_PORT( _A, i.uv);
				float4 b = ASE_SAMPLE_PORT( _B, i.uv);

				float result;
				switch ( _Type )
				{
					case 1: result = dot( a.x, b.x ); break;
					case 2: result = dot( a.xy, b.xy ); break;
					case 3: result = dot( a.xyz, b.xyz ); break;
					default: result = dot( a.xyzw, b.xyzw ); break;
				}
				return result;
			}
			ENDCG
		}
	}
}
