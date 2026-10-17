Shader "Hidden/TFHCFlipBookUVAnimation"
{
	Properties
	{
		_A ("_UV", 2DArray) = "white" {}
		_B ("_Columns", 2DArray) = "white" {}
		_C ("_Rows", 2DArray) = "white" {}
		_D ("_Speed", 2DArray) = "white" {}
		_E ("_StartFrame", 2DArray) = "white" {}
		_F ("_Speed", 2DArray) = "white" {}
		_G( "_MaxFrame", 2DArray ) = "white" {}
	}
	SubShader
	{
		CGINCLUDE
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
			float _EditorTime;
		ENDCG

		//Time port disconnected
		Pass
		{
			CGPROGRAM
			float4 frag(v2f_img i) : SV_Target
			{
				float2 uv = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				float col = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float row = ASE_SAMPLE_PORT( _C, i.uv ).r;
				float spd = ASE_SAMPLE_PORT( _D, i.uv ).r;
				float str = ASE_SAMPLE_PORT( _E, i.uv ).r;
				float maxframe = ASE_SAMPLE_PORT( _G, i.uv ).r;
				float numframes = col * row;
				float fbtotaltiles = ( maxframe < 0 ) ? numframes : min( numframes, maxframe + 1 );
				float fbcolsoffset = 1.0f / col;
				float fbrowsoffset = 1.0f / row;
				float fbspeed = _EditorTime * spd;
				float2 fbtiling = float2(fbcolsoffset, fbrowsoffset);
				float fbcurrenttileindex = floor( fmod( fbspeed + str, fbtotaltiles) );
				fbcurrenttileindex += ( fbcurrenttileindex < 0) ? fbtotaltiles : 0;
				float fblinearindextox = round ( fmod ( fbcurrenttileindex, col ) );
				float fboffsetx = fblinearindextox * fbcolsoffset;
				float fblinearindextoy = round( fmod( ( fbcurrenttileindex - fblinearindextox ) / col, row ) );
				fblinearindextoy = (int)(row-1) - fblinearindextoy;
				float fboffsety = fblinearindextoy * fbrowsoffset;
				float2 fboffset = float2(fboffsetx, fboffsety);
				float2 fbuv = float4( uv, 0.0 , 0.0 ) * fbtiling + fboffset;
				return float4(fbuv, 0 , 0);
			}
			ENDCG
		}
		
		//Time port connected
		Pass
		{
			CGPROGRAM
			float4 frag(v2f_img i) : SV_Target
			{
				float2 uv = ASE_SAMPLE_PORT( _A, i.uv ).rg;
				float col = ASE_SAMPLE_PORT( _B, i.uv ).r;
				float row = ASE_SAMPLE_PORT( _C, i.uv ).r;
				float spd = ASE_SAMPLE_PORT( _D, i.uv ).r;
				float str = ASE_SAMPLE_PORT( _E, i.uv ).r;
				float time = ASE_SAMPLE_PORT( _F, i.uv ).r;
				float maxframe = ASE_SAMPLE_PORT( _G, i.uv ).r;
				float numframes = col * row;
				float fbtotaltiles = ( maxframe < 0 ) ? numframes : min( numframes, maxframe + 1 );
				float fbcolsoffset = 1.0f / col;
				float fbrowsoffset = 1.0f / row;
				float fbspeed = time * spd;
				float2 fbtiling = float2(fbcolsoffset, fbrowsoffset);
				float fbcurrenttileindex = floor( fmod( fbspeed + str, fbtotaltiles) );
				fbcurrenttileindex += ( fbcurrenttileindex < 0) ? fbtotaltiles : 0;
				float fblinearindextox = round ( fmod ( fbcurrenttileindex, col ) );
				float fboffsetx = fblinearindextox * fbcolsoffset;
				float fblinearindextoy = round( fmod( ( fbcurrenttileindex - fblinearindextox ) / col, row ) );
				fblinearindextoy = (int)(row-1) - fblinearindextoy;
				float fboffsety = fblinearindextoy * fbrowsoffset;
				float2 fboffset = float2(fboffsetx, fboffsety);
				float2 fbuv = float4( uv, 0.0 , 0.0 ) * fbtiling + fboffset;
				return float4(fbuv, 0 , 0);
			}
			ENDCG
		}
	}
}
