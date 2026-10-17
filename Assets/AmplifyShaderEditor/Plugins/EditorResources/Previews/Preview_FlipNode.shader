Shader "Hidden/FlipNode"
{
	Properties
	{
		_A ("_A", 2DArray) = "white" {}
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
			float4 _Flip;
			int _Count;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 output = 0;
				output = ( _Flip.x * -2 + 1 ) * ASE_SAMPLE_PORT( _A, i.uv ).x;
				output.y = ( _Count > 1 ) ? ( ( _Flip.y * -2 + 1 ) * ASE_SAMPLE_PORT( _A, i.uv ).y ) : output.y;
				output.z = ( _Count > 2 ) ? ( ( _Flip.z * -2 + 1 ) * ASE_SAMPLE_PORT( _A, i.uv ).z ) : output.z;
				output.w = ( _Count > 3 ) ? ( ( _Flip.w * -2 + 1 ) * ASE_SAMPLE_PORT( _A, i.uv ).w ) : output.w;
				return output;
			}
			ENDCG
		}
	}
}
