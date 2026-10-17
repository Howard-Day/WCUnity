Shader "Hidden/HueNode"
{
	Properties
	{
		_A ("_In", 2DArray) = "white" {}
		_B ("_Offset", 2DArray) = "white" {}
	}
	CGINCLUDE
		#include "UnityCG.cginc"
		#include "Preview.cginc"
		#pragma vertex vert_img
		#pragma fragment frag
		#pragma target 3.5

		ASE_PORT( _A );
		ASE_PORT( _B );

		float4 Hue( float2 uv, float scale )
		{
			float3 hsv = RGBToHSV( ASE_SAMPLE_PORT( _A, uv ).rgb );
			float offset = ASE_SAMPLE_PORT( _B, uv ).r * scale;
			return float4( HSVToRGB( float3( hsv.x + offset, hsv.yz ) ), 1 );
		}
	ENDCG
	SubShader
	{
		Pass //Degrees
		{
			CGPROGRAM
			float4 frag( v2f_img i ) : SV_Target
			{
				return Hue( i.uv, 1.0 / 360.0 );
			}
			ENDCG
		}

		Pass //Normalized
		{
			CGPROGRAM
			float4 frag( v2f_img i ) : SV_Target
			{
				return Hue( i.uv, 1.0 );
			}
			ENDCG
		}
	}
}
