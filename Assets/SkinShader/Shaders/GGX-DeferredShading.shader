// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Hidden/GGX-DeferredShading" {
Properties {
    _LightTexture0 ("", any) = "" {}
    _LightTextureB0 ("", 2D) = "" {}
    _ShadowMapTexture ("", any) = "" {}
    _SrcBlend ("", Float) = 1
    _DstBlend ("", Float) = 1
}
SubShader {

// Pass 1: Lighting pass
//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {
    ZWrite Off
    Blend [_SrcBlend] [_DstBlend]

CGPROGRAM
#pragma target 3.0
#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt
#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
sampler2D _RampTex;

#define BLOODCOLOR(NdotL, bloodIntensity, diffuse)\
	float3 diffuse = tex2D(_RampTex, float2(NdotL * 0.5 + 0.5, bloodIntensity));

inline float3 SubTransparentColor(float3 lightDir, float3 viewDir, float3 finalColor) {
	float VdotH = saturate(dot(viewDir, -normalize(lightDir)));
	return finalColor * VdotH;
}

float4 PBR_BRDF (float3 diffColor, float3 specColor, float smoothness,
    float3 normal, float3 viewDir,
    UnityLight light, bool preIntDiff)
{
    float perceptualRoughness = SmoothnessToPerceptualRoughness (smoothness);
    float3 floatDir = Unity_SafeNormalize (float3(light.dir) + viewDir);

    float nv = dot(normal, viewDir);    // This abs allow to limit artifact

    float nl = saturate(dot(normal, light.dir));
    float nh = saturate(dot(normal, floatDir));

    float lh = saturate(dot(light.dir, floatDir));

    // Diffuse term
	
	float3 diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
	float bloodValue = 0;
	float subTrans = 0;
	if (preIntDiff) {
		bloodValue = specColor.g;
		subTrans = specColor.b;
		specColor.rgb = specColor.rrr;
		diffuseTerm = tex2D(_RampTex, float2(nl * 0.5 + 0.5, bloodValue));
	}
    //Diffuse = DisneyDiffuse(NoV, NoL, LoH, SmoothnessToPerceptualRoughness (smoothness)) * NoL;
    // Specular term
    // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
    // BUT 1) that will make shader look significantly darker than Legacy ones
    // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if UNITY_BRDF_GGX
    // GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
    roughness = max(roughness, 0.002);
    float V = SmithJointGGXVisibilityTerm (nl, nv, roughness);
    float D = GGXTerm (nh, roughness);
#else
    // Legacy
    float V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
    float D = NDFBlinnPhongNormalizedTerm (nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

    float specularTerm = V*D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#   ifdef UNITY_COLORSPACE_GAMMA
        specularTerm = sqrt(max(1e-4h, specularTerm));
#   endif
    specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
    specularTerm = 0.0;
#endif
    float3 color =  (diffColor * diffuseTerm + specularTerm * FresnelTerm (specColor, lh)) * light.color;
	color += SubTransparentColor(light.dir, viewDir, light.color * diffColor * subTrans);
    return float4(color, 1);
}
#pragma multi_compile SSS_OFF SSS_ON


float4 CalculateLight (unity_v2f_deferred i)
{
    float3 wpos;
    float2 uv;
    float atten, fadeDist;
    UnityLight light;
    UNITY_INITIALIZE_OUTPUT(UnityLight, light);
    UnityDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist);

    light.color = _LightColor.rgb * atten;

    // unpack Gbuffer
    float4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
    float4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
    float4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
	gbuffer2.xyz  = gbuffer2.xyz * 2 - 1;
	float3 eyeVec = normalize(wpos - _WorldSpaceCameraPos);
	return PBR_BRDF(gbuffer0.rgb, gbuffer1.rgb, gbuffer1.a, gbuffer2.xyz, -eyeVec, light, gbuffer2.a > 0.1);
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i) : SV_Target
{
    float4 c = CalculateLight(i);
    #ifdef UNITY_HDR_ON
    return c;
    #else
    return exp2(-c);
    #endif
}

ENDCG
}


// Pass 2: Final decode pass.
// Used only with HDR off, to decode the logarithmic buffer into the main RT
Pass {
    ZTest Always Cull Off ZWrite Off
    Stencil {
        ref [_StencilNonBackground]
        readmask [_StencilNonBackground]
        // Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
        compback equal
        compfront equal
    }

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

#include "UnityCG.cginc"

sampler2D _LightBuffer;
struct v2f {
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(vertex);
    o.texcoord = texcoord.xy;
#ifdef UNITY_SINGLE_PASS_STEREO
    o.texcoord = TransformStereoScreenSpaceTex(o.texcoord, 1.0f);
#endif
    return o;
}

fixed4 frag (v2f i) : SV_Target
{
    return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG
}

}
Fallback Off
}
