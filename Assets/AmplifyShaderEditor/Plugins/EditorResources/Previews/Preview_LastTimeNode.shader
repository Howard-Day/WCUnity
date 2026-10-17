Shader "Hidden/LastTimeNode"
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
				float4 t = 0;
				t.x = preview_EditorLastTime;
				t.y = sin( preview_EditorLastTime );
				t.z = cos( preview_EditorLastTime );
				return t;
			}
			ENDCG
		}
	}
}
