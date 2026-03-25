using Meta.XR;
using Meta.XR.MRUtilityKit;
using System;
using System.Collections;
using UnityEngine;

public class PassThroughCameraGrabber : MonoBehaviour
{
    private PassthroughCameraAccess cameraAccess;
    private float lastDetectionTime;
    private Texture2D outputTexture;
    private bool isReady = false;

    public float detectionInterval = 0.5f;
    public AIVisionManager visionManager;

    void Start()
    {
        cameraAccess = GetComponent<PassthroughCameraAccess>();
        if (cameraAccess == null)
        {
            Debug.LogError("[Grabber] Missing PassthroughCameraAccess component on this GameObject!");
            return;
        }

        StartCoroutine(InitializeCameraRoutine());
    }

    private IEnumerator InitializeCameraRoutine()
    {
        while (OVRManager.instance == null || !OVRManager.IsInsightPassthroughInitialized())
        {
            yield return new WaitForSeconds(0.5f);
        }

        while (!cameraAccess.IsPlaying)
        {
            yield return new WaitForSeconds(0.2f);
        }

        isReady = true;
    }

    void Update()
    {
        if (!isReady || cameraAccess == null) return;

        if (Time.time > lastDetectionTime + detectionInterval)
        {
            Texture metaTexture = cameraAccess.GetTexture();

            if (metaTexture != null)
            {
                Texture2D readableTexture = ConvertToTexture2D(metaTexture);

                if (readableTexture != null)
                {
                    visionManager.DetectObjects(readableTexture);
                    lastDetectionTime = Time.time;
                }
            }
        }
    }

    private Texture2D ConvertToTexture2D(Texture source)
    {
        if (outputTexture == null || outputTexture.width != source.width || outputTexture.height != source.height)
        {
            outputTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        }

        RenderTexture rt = source as RenderTexture;
        if (rt == null) return null;

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        outputTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        outputTexture.Apply();

        RenderTexture.active = previous;

        return outputTexture;
    }
}
