using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class SSSSSRenderer : MonoBehaviour {
    // Use this for initialization
    private CommandBuffer buffer;
    private Camera cam;
    static int _RenderTargetTex = Shader.PropertyToID("_RenderTargetTex");
    static int _TempTex = Shader.PropertyToID("_UnUsedTempTex");

    void Awake ()
    {
        cam = GetComponent<Camera>();
        buffer = new CommandBuffer();
        if (SSSSSObject.renderMat == null)
        {
            SSSSSObject.renderMat = new Material(Shader.Find("Hidden/PerObjSSS"));
        }
	}

    private void OnPreCull()
    {
        SSSSSObject.isSSSSSCamera = true;
        SSSSSObject.buffer = buffer;
        buffer.Clear();
        buffer.GetTemporaryRT(_TempTex, cam.pixelWidth, cam.pixelHeight, 24, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR);
        buffer.Blit(BuiltinRenderTextureType.CameraTarget, _TempTex);
        buffer.SetRenderTarget(_TempTex);
        buffer.SetGlobalTexture(_RenderTargetTex, BuiltinRenderTextureType.CameraTarget);
    }

    private void OnPreRender()
    {
        buffer.Blit(_TempTex, BuiltinRenderTextureType.CameraTarget);
    }

    private void OnPostRender()
    {
        SSSSSObject.isSSSSSCamera = false;
    }

    private void OnEnable()
    {
        cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buffer);
    }

    private void OnDisable()
    {
        cam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, buffer);
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
}
