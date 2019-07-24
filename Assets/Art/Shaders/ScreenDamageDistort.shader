// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/ScreenDamageDistort"
{
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _DmgAmt ("Damage Amount", Range(0, 1)) = 0
        _SpeedU ("Speed U", Range(0, 1)) = 0
        _SpeedV ("Speed V", Range(0, 1)) = 0
        _OffsetU ("Offset U", Range(0, 1)) = 0
        _OffsetV ("Offset V", Range(0, 1)) = 0

    }
    SubShader {
        GrabPass { "_GrabTexture" }
        ZWrite off
        ZTest Always
        Blend oneminussrcalpha srcalpha
        Pass {
            Tags { "Queue"="Transparent+2000" }
       
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct v2f {
                half4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;  
                half4 grabPos : TEXCOORD1;
            };
            uniform sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            sampler2D _GrabTexture;
            half _DmgAmt;
            half _SpeedU;
            half _SpeedV;
            half _OffsetU;
            half _OffsetV;
 
            v2f vert(appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
                return o;
            }
 
            half4 frag(v2f i) : COLOR {
                _OffsetU *=.02*saturate(_DmgAmt*3);
                _OffsetV *=.02*saturate(_DmgAmt*3);
                _SpeedU *= 50;
                _SpeedV *= 50;
                half uv2X = i.uv.x/20+_Time.x*20;
                half uv2Y = i.uv.y/20+_Time.x*2;
                i.uv.x += _Time.x*_SpeedU;
                i.uv.y += _Time.x*_SpeedV;
                
                fixed4 col2 = tex2D(_MainTex, fixed2(uv2X,uv2Y) );
                col2.b = saturate(col2.b-.25)*1.25;
                fixed4 col = tex2D(_MainTex, i.uv*(1-col2.g));
                i.grabPos.x += ((col.r*_OffsetU)-_OffsetU/2)*col2.b;//sin((_Time.y + i.grabPos.y) * _Intensity)/500;
                i.grabPos.y += ((col.g*_OffsetV)-_OffsetV/2)*col2.b;
                fixed4 color = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.grabPos));
                color.rgb += (col.r-.75)*_DmgAmt+(col2.r-.5)*_DmgAmt;
                col.b = saturate( ((col.b)*200)-(_DmgAmt)*200 );
                color.a = col.b;
                //color.rgb = col2.b;
                return color;
            }
            ENDCG
        }
    }

        FallBack "Diffuse"

}

