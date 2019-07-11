Shader "Pixel Art/SpaceSkybox"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
		_DitherBlendAmt("Dither Blend", Float) = 1
        
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 screenPos : TEXCOORD0;
            };

            struct v2f
            {
                float4 screenPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
            //fixed4 _ScreenParams;
            sampler2D _MainTex;
            float4 _Color;
            fixed _DitherBlendAmt;

            fixed4 frag (v2f i) : SV_Target
            { 
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                fixed4 col = tex2D(_MainTex, screenUV);
                // just invert the colors
                col *=_Color;
                col.rgb = lerp(col.rgb,_Color.rgb,_DitherBlendAmt);
                return col;
            }
            ENDCG
        }
    }
}
