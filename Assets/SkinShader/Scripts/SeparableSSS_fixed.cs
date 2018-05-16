using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class SeparableSSS
{
    //public Vector3 strength = new Vector3(0.48f, 0.41f, 0.28f);
    //public Vector3 falloff = new Vector3(1.0f, 0.37f, 0.3f);
	public static void calculateKernel(List<Vector4> kernel, int nSamples, Vector3 strength, Vector3 falloff)
    {
        float RANGE = nSamples > 20 ? 3.0f : 2.0f;
        float EXPONENT = 2.0f;
        kernel.Clear();

        // Calculate the offsets:
        float step = 2.0f * RANGE / (nSamples - 1);
        for (int i = 0; i < nSamples; i++)
        {
            float o = -RANGE + i * step;
            float sign = o < 0.0f ? -1.0f : 1.0f;
            float w = RANGE * sign *Mathf.Abs(Mathf.Pow(o, EXPONENT)) / Mathf.Pow(RANGE, EXPONENT);
            kernel.Add(new Vector4(0, 0, 0, w));
        }
        // Calculate the weights:
        for (int i = 0; i < nSamples; i++)
        {
            float w0 = i > 0 ? Mathf.Abs(kernel[i].w - kernel[i - 1].w) : 0.0f;
            float w1 = i < nSamples - 1 ? Mathf.Abs(kernel[i].w - kernel[i + 1].w) : 0.0f;
            float area = (w0 + w1) / 2.0f;
            Vector3 temp =  profile(kernel[i].w, falloff);
            Vector4 tt = new Vector4(area * temp.x, area * temp.y, area * temp.z, kernel[i].w);
            kernel[i] = tt;
        }
        // We want the offset 0.0 to come first:
        Vector4 t = kernel[nSamples / 2];
        for (int i = nSamples / 2; i > 0; i--)
            kernel[i] = kernel[i - 1];
        kernel[0] = t;
        // Calculate the sum of the weights, we will need to normalize them below:
        Vector4 sum = Vector4.zero;
        for (int i = 0; i < nSamples; i++)
        {
            sum.x += kernel[i].x;
            sum.y += kernel[i].y;
            sum.z += kernel[i].z;
            //sum += D3DXVECTOR3(kernel[i]);
        }
        // Normalize the weights:
        for (int i = 0; i < nSamples; i++)
        {
            Vector4 vecx = kernel[i];
            vecx.x /= sum.x;
            vecx.y /= sum.y;
            vecx.z /= sum.z;
            kernel[i] = vecx;
        }

        // Tweak them using the desired strength. The first one is:
        //     lerp(1.0, kernel[0].rgb, strength)
        Vector4 vec = kernel[0];
        vec.x = (1.0f - strength.x) * 1.0f + strength.x * vec.x;
        vec.y = (1.0f - strength.y) * 1.0f + strength.y * vec.y;
        vec.z = (1.0f - strength.z) * 1.0f + strength.z * vec.z;
        kernel[0] = vec;

        // The others:
        //     lerp(0.0, kernel[0].rgb, strength)
        for (int i = 1; i < nSamples; i++)
        {
            var vect = kernel[i];
            vect.x *= strength.x;
            vect.y *= strength.y;
            vect.z *= strength.z;
            kernel[i] = vect;
        }
    }


	private static Vector3 gaussian(float variance, float r, Vector3 falloff)
    {
        /**
         * We use a falloff to modulate the shape of the profile. Big falloffs
         * spreads the shape making it wider, while small falloffs make it
         * narrower.
         */
        Vector3 g;

        float rr1 = r / (0.001f + falloff.x);
        g.x = Mathf.Exp((-(rr1 * rr1)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        float rr2 = r / (0.001f + falloff.y);
        g.y = Mathf.Exp((-(rr2 * rr2)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        float rr3 = r / (0.001f + falloff.z);
        g.z = Mathf.Exp((-(rr3 * rr3)) / (2.0f * variance)) / (2.0f * 3.14f * variance);

        return g;
    }
	private static Vector3 profile(float r, Vector3 falloff)
    {
        /**
         * We used the red channel of the original skin profile defined in
         * [d'Eon07] for all three channels. We noticed it can be used for green
         * and blue channels (scaled using the falloff parameter) without
         * introducing noticeable differences and allowing for total control over
         * the profile. For example, it allows to create blue SSS gradients, which
         * could be useful in case of rendering blue creatures.
         */
        return  // 0.233f * gaussian(0.0064f, r, falloff) + /* We consider this one to be directly bounced light, accounted by the strength parameter (see @STRENGTH) */
                   0.100f * gaussian(0.0484f, r, falloff) +
                   0.118f * gaussian(0.187f, r, falloff) +
                   0.113f * gaussian(0.567f, r, falloff) +
                   0.358f * gaussian(1.99f, r, falloff) +
                   0.078f * gaussian(7.41f, r, falloff);
    }
}