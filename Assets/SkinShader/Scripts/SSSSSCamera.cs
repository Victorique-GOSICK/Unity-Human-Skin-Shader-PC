using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderIDs{
    public static int blendTexID = Shader.PropertyToID("_BlendTex");
    public static int blendTex1ID = Shader.PropertyToID("_BlendTex1");
    public static int blendWeightID = Shader.PropertyToID("_BlendWeight");
    public static int blendWeight1ID = Shader.PropertyToID("_BlendWeight1");
    public static int blendWeight2ID = Shader.PropertyToID("_BlendWeight2");
    public static int blur1ID = Shader.PropertyToID("_Blur1Tex");
    public static int blur2ID = Shader.PropertyToID("_Blur2Tex");
    public static int blurIntensity = Shader.PropertyToID("_BlurIntensity");
}

[RequireComponent(typeof(Camera))]
public class SSSSSCamera : MonoBehaviour
{
    #region MASK_SPACE
    Camera cam;
    Material mat;
    CommandBuffer buffer;

    static int width;
    static int height;
    void Awake()
    {
        width = Screen.width;
        height = Screen.height;
        cam = GetComponent<Camera>();
        cam.depthTextureMode |= DepthTextureMode.Depth;
        mat = new Material(Shader.Find("Hidden/SSSSS"));
        Shader.SetGlobalVector(ShaderIDs.blendWeightID, new Vector4(0.33f, 0.45f, 0.36f));
        Shader.SetGlobalVector(ShaderIDs.blendWeight1ID, new Vector4(0.34f, 0.19f));
        Shader.SetGlobalVector(ShaderIDs.blendWeight2ID, new Vector4(0.46f, 0f, 0.04f));
        buffer = new CommandBuffer();
        buffer.name = "SSSSS";
        BufferBlit();
    }

    void BufferBlit()
    {
        buffer.GetTemporaryRT(ShaderIDs.blendTexID, Screen.width, Screen.height, 24);
        buffer.GetTemporaryRT(ShaderIDs.blendTex1ID, Screen.width, Screen.height, 24);
        buffer.GetTemporaryRT(ShaderIDs.blur1ID, Screen.width, Screen.height, 24);
        buffer.GetTemporaryRT(ShaderIDs.blur2ID, Screen.width, Screen.height, 24);
        buffer.Blit(BuiltinRenderTextureType.CameraTarget, ShaderIDs.blendTexID);
        buffer.Blit(ShaderIDs.blendTexID, ShaderIDs.blur1ID, mat, 0);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 1);
        buffer.Blit(ShaderIDs.blur2ID, ShaderIDs.blendTex1ID, mat, 2);
        buffer.Blit(ShaderIDs.blendTex1ID, ShaderIDs.blur1ID, mat, 0);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 1);
        buffer.Blit(ShaderIDs.blur2ID, ShaderIDs.blendTexID, mat, 3);
        buffer.Blit(ShaderIDs.blendTexID, ShaderIDs.blur1ID, mat, 0);
        buffer.Blit(ShaderIDs.blur1ID, ShaderIDs.blur2ID, mat, 1);
        buffer.Blit(ShaderIDs.blur2ID, BuiltinRenderTextureType.CameraTarget, mat, 4);
    }

    void OnEnable()
    {
        cam.AddCommandBuffer(CameraEvent.AfterForwardOpaque, buffer);
    }

    void OnDisable()
    {
        cam.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, buffer);
    }

    void OnPreRender()
    {
        if (width != Screen.width || height != Screen.height)
        {
            width = Screen.width;
            height = Screen.height;
            buffer.Clear();
            BufferBlit();
        }
    }

    #endregion


}
