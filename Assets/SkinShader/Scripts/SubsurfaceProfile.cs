using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubsurfaceProfile : MonoBehaviour {
    [Range(0f, 100f)]
	public float SSSScaler = 1;
	public Color SSSColor = new Vector4 (122f, 104f, 71.4f, 0.0f);
	public Color SSSFalloff = new Vector4 (255f, 94.5f, 76.58f, 0.0f);
	private List<Vector4> KernelArray = new List<Vector4>();
    static int kernel = Shader.PropertyToID("kernel");
    static int _SSSScale = Shader.PropertyToID("_SSSScale");
	void Update () {
		Vector3 SSSC = new Vector3 (SSSColor.r, SSSColor.g, SSSColor.b);
		Vector3 SSSFC = new Vector3 (SSSFalloff.r, SSSFalloff.g, SSSFalloff.b);
        SeparableSSS.calculateKernel(KernelArray, 32, SSSC, SSSFC);
        effect.SetVectorArray(kernel, KernelArray);
        effect.SetFloat(_SSSScale, -SSSScaler);
    }

    public Material effect;

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, effect);
    }
}
