Shader "Unlit/MFDTintable"
{
    Properties
    {
       [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_RedColor ("Red Color", Color) = (1,0,0,1)
        _BlueColor ("Blue Color", Color) = (1,0,0,1)
        _GreenColorA ("Green Color", Color) = (1,0,0,1)
        _GreenColorB ("Green Color 2", Color) = (1,0,0,1)
    }
    SubShader
    {
        Tags {
        "Queue"="Transparent" 
        "IgnoreProjector"="True" 
        "RenderType"="Transparent" 
        "PreviewType"="Plane"
        "CanUseSpriteAtlas"="True"
        }
        LOD 100

        Cull Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f{
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex;
			float4 _MainTex_ST;
            fixed4 _RedColor;
            fixed4 _BlueColor;
            fixed4 _GreenColorA;
            fixed4 _GreenColorB;

            v2f vert(appdata v){
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET{
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 redCol = lerp(0,_RedColor.rgb,col.r);
                fixed3 bluCol = lerp(0,_BlueColor.rgb,col.b);
                fixed3 grnCol = lerp(0,_GreenColorA.rgb,saturate(col.g*2 *(1-saturate((col.g*2)-1)*10)  ));
                fixed3 gr2Col = lerp(0,_GreenColorB.rgb,saturate((col.g*2)-1));

                col.rgb = redCol+bluCol+grnCol+gr2Col;                
                //col *= _Color;
                //col *= i.color;
                return col;
            }


            ENDCG
        }
    }
}
