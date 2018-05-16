Shader "Hidden/PerObjSSS" {
    SubShader {
        Pass {
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
#define Sample 32
            uniform sampler2D _CameraDepthTexture;
            uniform sampler2D _RenderTargetTex; uniform float4 _RenderTargetTex_TexelSize;
            uniform float _SSSScale;
            uniform float4 kernel[Sample];

            float4 SSS( float DepthColor, float4 colorM , float2 UV , float2 dir , float sssWidth ){        
              float depthM = LinearEyeDepth(tex2D(_CameraDepthTexture, UV).r) - _ProjectionParams.g;
                                    
			  float distanceToProjectionWindow = 5.671281819617709;
              float scale = distanceToProjectionWindow / depthM;
                                    
              float2 finalStep = sssWidth * scale * dir;
			  finalStep *= 0.33333333333;
                                    
              float4 colorBlurred = colorM;
              colorBlurred.rgb *= kernel[0].rgb;
                                    
              float2 offset = 0;
              float4 color = 0;
              float depth = 0;
              float s = 0;
              for (int i = 1; i < Sample; i++){
                 offset = UV + kernel[i].a * finalStep;
                 color = tex2D(_RenderTargetTex, offset);
                 depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, offset)) - _ProjectionParams.g;
                        					
                 s = saturate(1701.3845458853127 * sssWidth * abs(depthM - depth));
                 color.rgb = lerp(color.rgb, colorM.rgb, s);
                 colorBlurred.rgb += kernel[i].rgb * color.rgb;
                }
            return colorBlurred * 0.5;
            }
            
            struct VertexInput {
                float4 vertex : POSITION;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o;
                o.pos = UnityObjectToClipPos( v.vertex );
                o.uv0 = ComputeScreenPos(o.pos);
                return o;
            }
			sampler2D _CameraGBufferTexture0;
            float4 frag(VertexOutput i) : COLOR {
                float2 uv = i.uv0.xy / i.uv0.w;
                float SceneDepth_Sorce = max(0, LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv)) - _ProjectionParams.g);
				float4 colorM = tex2D(_RenderTargetTex, uv);
				float tp = tex2D(_CameraGBufferTexture0, uv).a;
                clip(tp - 0.1);
                float2 sssScale = _SSSScale * _RenderTargetTex_TexelSize.xy;
                float3 XBlur = SSS( SceneDepth_Sorce , colorM, uv , float2(1,0) , sssScale.x).rgb;
                float3 YBlur = SSS( SceneDepth_Sorce , colorM, uv , float2(0,1) , sssScale.y).rgb;
                float3 emissive = XBlur + YBlur;
                return float4(emissive,1);
            }
            ENDCG
        }
    }
}
