using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[RequireComponent(typeof(Camera))]
public class SubsurfaceProfile : MonoBehaviour {
    [Range(0, 100f)]
	public float SSSScaler = 1;
    public Color SSSColor;
    public Color SSSFalloff;
    private Camera cam;
    CommandBuffer buffer;
	private List<Vector4> KernelArray = new List<Vector4>();
    static int kernel = Shader.PropertyToID("kernel");
    static int _SSSScale = Shader.PropertyToID("_SSSScale");
    private void Awake()
    {
        cam = GetComponent<Camera>();
        buffer = new CommandBuffer();
    }

    private void OnEnable()
    {
        cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buffer);
    }

    private void OnDisable()
    {
        cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buffer);
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
    static int tempTex = Shader.PropertyToID("_TempTex_SSS");

    private void OnPreRender()
    {
        Vector3 SSSC = new Vector3(SSSColor.r, SSSColor.g, SSSColor.b);
        Vector3 SSSFC = new Vector3(SSSFalloff.r, SSSFalloff.g, SSSFalloff.b);
        SeparableSSS.calculateKernel(KernelArray, 16, SSSC, SSSFC);
        effect.SetFloat(_SSSScale, SSSScaler);
        effect.SetVectorArray(kernel, KernelArray);
        buffer.Clear();
        buffer.GetTemporaryRT(tempTex, cam.pixelWidth, cam.pixelHeight, 0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR);
        buffer.Blit(BuiltinRenderTextureType.CameraTarget, tempTex, effect, 0);
        buffer.Blit(tempTex, BuiltinRenderTextureType.CameraTarget, effect, 1);
        buffer.Blit(BuiltinRenderTextureType.CameraTarget, tempTex, effect, 0);
        buffer.Blit(tempTex, BuiltinRenderTextureType.CameraTarget, effect, 1);
    }

    public Material effect;
}
