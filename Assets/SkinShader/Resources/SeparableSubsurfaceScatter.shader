Shader "SeparableSubsurfaceScatter" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
    }
CGINCLUDE
#include "UnityCG.cginc"
#define Sample 20
#define DistanceToProjectionWindow 5.671281819617709             //1.0 / tan(0.5 * radians(20));
#define DPTimes300 1701.384545885313                             //DistanceToProjectionWindow * 300
            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _MainTex; uniform float4 _MainTex_TexelSize;
            uniform float _SSSScale;
            uniform float4 kernel[Sample];
            sampler2D _CameraGBufferTexture2;

            
            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.uv = v.texcoord0;
                o.pos = UnityObjectToClipPos( v.vertex );
                return o;
            }
float4 SSS(float4 SceneColor, float2 UV, float2 SSSIntencity) {
    float SceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, UV));                                   
    float BlurLength = DistanceToProjectionWindow / SceneDepth;                                   
    float2 UVOffset = SSSIntencity * BlurLength;                      
        float4 BlurSceneColor = SceneColor;
    BlurSceneColor.rgb *=  kernel[0].rgb;  

    [loop]
    for (int i = 1; i < Sample; i++) {
        float2 SSSUV = UV +  kernel[i].a * UVOffset;
        float4 SSSSceneColor = tex2D(_MainTex, SSSUV);
        float SSSDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, SSSUV)).r;         
        float SSSScale = saturate(DPTimes300 * SSSIntencity * abs(SceneDepth - SSSDepth));
        SSSSceneColor.rgb = lerp(SSSSceneColor.rgb, SceneColor.rgb, SSSScale);
        BlurSceneColor.rgb +=  kernel[i].rgb * SSSSceneColor.rgb;
    }
    return BlurSceneColor;
}
ENDCG
    SubShader {
        ZTest Always
        ZWrite Off
		Cull off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(VertexOutput i) : COLOR {
				bool tp = tex2D(_CameraGBufferTexture2, i.uv).a > 0.1;
                float4 SceneColor = tex2D(_MainTex, i.uv);
                float SSSIntencity = (_SSSScale * _MainTex_TexelSize.x);
                float3 XBlur = SSS(SceneColor, i.uv, float2(SSSIntencity, 0)).rgb;
				return lerp(SceneColor, float4(XBlur, 1), tp);
            }
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 frag(VertexOutput i) : COLOR {
				bool tp = tex2D(_CameraGBufferTexture2, i.uv).a > 0.1;
                float4 SceneColor = tex2D(_MainTex, i.uv);
                float SSSIntencity = (_SSSScale * _MainTex_TexelSize.y);
                float3 YBlur = SSS(SceneColor, i.uv, float2(0, SSSIntencity)).rgb;
				return lerp(SceneColor, float4(YBlur, 1), tp);
            }
            ENDCG
        }

        
    }
}
