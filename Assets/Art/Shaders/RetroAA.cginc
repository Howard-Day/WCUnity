#include "UnityCG.cginc"

#pragma target 3.0

fixed4 RetroAA(sampler2D tex, float2 uv, float4 texelSize){
	float2 texelCoord = uv*texelSize.zw;
	float2 hfw = 0.5*fwidth(texelCoord);
	float2 fl = floor(texelCoord - 0.5) + 0.5;
	float2 uvaa = (fl + smoothstep(0.5 - hfw, 0.5 + hfw, texelCoord - fl))*texelSize.xy;

	return tex2D(tex, uvaa);
}
