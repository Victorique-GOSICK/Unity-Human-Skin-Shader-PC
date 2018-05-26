using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class SSSSSGBuffer : MonoBehaviour {
    public Texture rampTexture;
    private void OnEnable()
    {
        Shader.SetGlobalTexture("_RampTex", rampTexture);
    }
    private void OnDisable()
    {
        Shader.SetGlobalTexture("_RampTex", null);
    }
}
