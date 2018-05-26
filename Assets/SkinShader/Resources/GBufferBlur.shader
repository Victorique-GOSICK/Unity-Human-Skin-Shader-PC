Shader "Hidden/GBufferBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
CGINCLUDE
float4 _BlendWeight;
sampler2D _Blur2Tex;
sampler2D _MainTex;
float4 _MainTex_TexelSize;
sampler2D _CameraGBufferTexture2;
float _BlurIntensity;
#include "UnityCG.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
#define BLUR0 0.12516840610182164
#define BLUR1 0.11975714566876787
#define BLUR2 0.10488697964330942
#define BLUR3 0.08409209097592142
#define BLUR4 0.061716622693291805
#define BLUR5 0.04146317758515726
#define BLUR6 0.025499780382641484
#define Gaus(offset, blur)\
		col = tex2D(_MainTex, uv + offset);\
		c += lerp(originColor, col, col.a) * blur;\
		col = tex2D(_MainTex, uv - offset);\
		c += lerp(originColor, col, col.a) * blur;

	inline float4 getWeightedColor(float2 uv, float2 offset) {
		float4 originColor = tex2D(_MainTex, uv);
		float4 c = originColor * BLUR0;
		float2 offsetM2 = offset * 2;
		float2 offsetM3 = offset * 3;
		float2 offsetM4 = offset * 4;
		float2 offsetM5 = offset * 5;
		float2 offsetM6 = offset * 6;
		float4 col;
		Gaus(offset,BLUR1)
		Gaus(offsetM2, BLUR2)
		Gaus(offsetM3, BLUR3)
		Gaus(offsetM4, BLUR4)
		Gaus(offsetM5, BLUR5)
		Gaus(offsetM6, BLUR6)
		return c;
	}
ENDCG

	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag (v2f i) : SV_Target
			{
				return lerp(tex2D(_MainTex, i.uv), tex2D(_Blur2Tex, i.uv), _BlendWeight);
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag (v2f i) : SV_Target
			{
				return getWeightedColor(i.uv, float2(_MainTex_TexelSize.x * _BlurIntensity, 0));
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag (v2f i) : SV_Target
			{
				return getWeightedColor(i.uv, float2(0, _MainTex_TexelSize.y * _BlurIntensity));
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				float v = tex2D(_CameraGBufferTexture2, i.uv).a;
				float offset = v > 0.1;
				float4 value = tex2D(_MainTex, i.uv);
				value.a = offset;
				return value;
			}
			ENDCG
		}
	}
}
