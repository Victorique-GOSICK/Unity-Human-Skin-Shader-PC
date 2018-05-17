using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SSSSSGBuffer : MonoBehaviour {
    public Texture rampTexture;
    private void Awake()
    {
        Shader.SetGlobalTexture("_RampTex", rampTexture);
    }
}
