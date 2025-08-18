using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBlurEffect : MonoBehaviour
{
    public Material blurMaterial;  // assign in inspector
    [Range(0, 10)] public int blurIterations = 3;
    [Range(0.2f, 3.0f)] public float blurSpread = 1.2f;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (blurMaterial != null)
        {
            // downsampling for a cheaper blur
            int rtW = src.width / 4;
            int rtH = src.height / 4;
            RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);

            // copies camera render into buffer
            Graphics.Blit(src, buffer);

            // blur iterations
            for (int i = 0; i < blurIterations; i++)
            {
                RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);

                // horizontal blur
                blurMaterial.SetVector("_Offset", new Vector4(blurSpread / src.width, 0, 0, 0));
                Graphics.Blit(buffer, buffer2, blurMaterial);
                RenderTexture.ReleaseTemporary(buffer); // frees GPU memory
                buffer = buffer2;

                buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);

                // vertical blur
                blurMaterial.SetVector("_Offset", new Vector4(0, blurSpread / src.height, 0, 0));
                Graphics.Blit(buffer, buffer2, blurMaterial);
                RenderTexture.ReleaseTemporary(buffer);
                buffer = buffer2;
            }

            Graphics.Blit(buffer, dest);
            RenderTexture.ReleaseTemporary(buffer);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
