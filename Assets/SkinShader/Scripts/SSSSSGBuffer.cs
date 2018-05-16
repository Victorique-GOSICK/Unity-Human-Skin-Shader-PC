using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SSSSSGBuffer : MonoBehaviour {
    CommandBuffer buffer;
    Material mat;
    Camera cam;
    public Texture rampTexture;
    [Range(0, 2f)]
    public float blurIntensity = 0.5f;
    int width, height;
    // Use this for initialization
    private void Awake()
    {
        cam = GetComponent<Camera>();
        mat = new Material(Shader.Find("Hidden/GBufferBlur"));
        buffer = new CommandBuffer();
        width = cam.pixelWidth;
        height = cam.pixelHeight;
        Shader.SetGlobalTexture("_RampTex", rampTexture);
    }

    private void OnEnable()
    {
        cam.AddCommandBuffer(CameraEvent.BeforeLighting, buffer);
    }

    private void OnDisable()
    {
        cam.RemoveCommandBuffer(CameraEvent.BeforeLighting, buffer);
    }

    private void OnPreRender()
    {
        if (width != cam.pixelWidth || height != cam.pixelHeight) {
            width = cam.pixelWidth;
            height = cam.pixelHeight;
            SetBuffer();
        }
        SetBuffer();
    }

    void SetBuffer() {
        buffer.Clear();
        buffer.GetTemporaryRT(ShaderIDs.blur1ID, cam.pixelWidth, cam.pixelHeight,0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
        buffer.GetTemporaryRT(ShaderIDs.blur2ID, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
        buffer.GetTemporaryRT(ShaderIDs.blendTexID, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
        buffer.GetTemporaryRT(ShaderIDs.blendTex1ID, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
        buffer.SetGlobalFloat(ShaderIDs.blurIntensity, blurIntensity);
        buffer.Blit(BuiltinRenderTextureType.GBuffer0, ShaderIDs.blendTex1ID, mat, 3);
        buffer.SetGlobalVector(ShaderIDs.blendWeightID, new Vector4(0.33f, 0.45f, 0.36f, 0));  //Magic number
        buffer.Blit(ShaderIDs.blendTex1ID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blur2ID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blendTex1ID, ShaderIDs.blendTexID, mat, 0);
        buffer.SetGlobalVector(ShaderIDs.blendWeightID, new Vector4(0.34f, 0.19f, 0, 0));
        buffer.Blit(ShaderIDs.blendTexID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blur2ID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blendTexID, ShaderIDs.blendTex1ID, mat, 0);
        buffer.SetGlobalVector(ShaderIDs.blendWeightID, new Vector4(0.46f, 0f, 0.04f, 0));
        buffer.Blit(ShaderIDs.blendTex1ID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blur2ID, ShaderIDs.blur1ID, mat, 1);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 2);
        buffer.Blit(ShaderIDs.blendTex1ID, BuiltinRenderTextureType.GBuffer0, mat, 0);
    }
}
