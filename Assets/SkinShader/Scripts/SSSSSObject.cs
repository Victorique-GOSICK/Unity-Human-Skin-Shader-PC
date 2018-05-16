using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SSSSSObject : MonoBehaviour {
    public Mesh currentMesh;
    public static CommandBuffer buffer;
    public static Material renderMat;
    public static bool isSSSSSCamera = false;
    public Color SSSColor;
    public Color FalloffColor;
    [Range(0, 100)]
    public float SSSScale = 10;
    private Color lastColor;
    private Color lastFalloffColor;
    private List<Vector4> kernels = new List<Vector4>(32);
    static int kernel = Shader.PropertyToID("kernel");
    static int _SSSScale = Shader.PropertyToID("_SSSScale");
    private void Awake()
    {
        SetKernel();
    }

    private void OnWillRenderObject()
    {
        if (!isSSSSSCamera) return;
        if (SSSColor != lastColor || FalloffColor != lastFalloffColor) {
            lastColor = SSSColor;
            lastFalloffColor = FalloffColor;
            SetKernel();
        }
        buffer.SetGlobalVectorArray(kernel, kernels);
        buffer.SetGlobalFloat(_SSSScale, -SSSScale);
        buffer.DrawMesh(currentMesh, transform.localToWorldMatrix, renderMat);
    }

    private void SetKernel() {
        Vector3 sssC = new Vector3(SSSColor.r, SSSColor.g, SSSColor.b);
        Vector3 sssFC = new Vector3(FalloffColor.r, FalloffColor.g, FalloffColor.b);
        SeparableSSS.calculateKernel(kernels, 32, sssC, sssFC);
    }
}
