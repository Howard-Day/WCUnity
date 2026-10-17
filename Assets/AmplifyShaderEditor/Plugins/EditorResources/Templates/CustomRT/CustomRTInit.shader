Shader /*ase_name*/"Hidden/Templates/CustomRTInit"/*end*/
{
    Properties
    {
        /*ase_props*/
    }

    SubShader
    {
        Tags { }

		/*ase_all_modules*/

		/*ase_pass*/
        Pass
        {
			Name "Custom RT Init"
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"

            #pragma vertex ASEInitCustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.5

			/*ase_pragma*/

			struct ase_appdata_init_customrendertexture
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
				/*ase_vdata:p=p;uv0=tc0*/
			};

			// User facing vertex to fragment structure for initialization materials
			struct ase_v2f_init_customrendertexture
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 position : TEXCOORD1;
				/*ase_interp(2,):sp=sp.xyzw;uv0=tc0;uv1=tc1*/
			};

			/*ase_globals*/

			float3 CustomRenderTextureComputeCubePosition( float2 globalTexcoord )
			{
				float2 xy = globalTexcoord * 2.0 - 1.0;
				float3 position;
				if ( _CustomRenderTextureCubeFace == 0.0 )
				{
					position = float3( 1.0, -xy.y, -xy.x );
				}
				else if ( _CustomRenderTextureCubeFace == 1.0 )
				{
					position = float3( -1.0, -xy.y, xy.x );
				}
				else if ( _CustomRenderTextureCubeFace == 2.0 )
				{
					position = float3( xy.x, 1.0, xy.y );
				}
				else if ( _CustomRenderTextureCubeFace == 3.0 )
				{
					position = float3( xy.x, -1.0, -xy.y );
				}
				else if ( _CustomRenderTextureCubeFace == 4.0 )
				{
					position = float3( xy.x, -xy.y, 1.0 );
				}
				else if ( _CustomRenderTextureCubeFace == 5.0 )
				{
					position = float3( -xy.x, -xy.y, -1.0 );
				}
				return position;
			}

			ase_v2f_init_customrendertexture ASEInitCustomRenderTextureVertexShader( ase_appdata_init_customrendertexture v /*ase_vert_input*/ )
			{
				ase_v2f_init_customrendertexture o;

				/*ase_vert_code:v=ase_appdata_init_customrendertexture;o=ase_v2f_init_customrendertexture*/

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = float3(v.texcoord.xy, CustomRenderTexture3DTexcoordW);
				o.position = CustomRenderTextureComputeCubePosition(v.texcoord.xy);
				return o;
			}

            float4 frag( ase_v2f_init_customrendertexture IN /*ase_frag_input*/ ) : SV_Target
            {
				/*ase_local_var:wp*/half3 PositionWS = IN.position;
				/*ase_local_var:wn*/half3 NormalWS = normalize( IN.position );

				/*ase_frag_code:IN=ase_v2f_init_customrendertexture*/

                float4 finalColor = /*ase_frag_out:Frag Color;Float4*/float4(1,1,1,1)/*end*/;

				return finalColor;
            }
            ENDCG
        }
    }
}
