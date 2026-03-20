using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR;
using System.Collections;

public class PassThroughCameraGrabber : MonoBehaviour
{
    private PassthroughCameraAccess cameraAccess;
    private WebCamTexture questCam;
    private bool isReady = false;
    private float lastDetectionTime;

    public float detectionInterval = 0.5f;
    public AIVisionManager visionManager;

    // Start is called before the first frame update
    void Start()
    {
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
        }

        StartCoroutine(InitializeCamera());
    }

    private IEnumerator InitializeCamera()
    {
        Debug.Log("<color=white>[Grabber] Waiting for OVRManager to initialize...</color>");

        while (!OVRManager.IsInsightPassthroughInitialized())
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (WebCamTexture.devices.Length > 0)
        {
            // Usually "0" is the Left RGB camera on Quest 3S
            questCam = new WebCamTexture(WebCamTexture.devices[0].name, 1280, 960, 30);
            questCam.Play();
        }
        else
        {
            Debug.LogError("[Grabber] No Camera Hardware detected by Unity!");
            yield break;
        }

        // 3. Wait for the texture to actually start producing pixels
        float timer = 0;
        while (questCam.width < 100 && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (questCam.isPlaying)
        {
            isReady = true;
            Debug.Log("<color=green>[Grabber] SUCCESS! WebCamTexture is Playing.</color>");
        }
        else
        {
            Debug.LogError("[Grabber] WebCamTexture failed to start after 5 seconds.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isReady || questCam == null) return;

        if (Time.time > lastDetectionTime + detectionInterval)
        {
            if (questCam.didUpdateThisFrame)
            {
                visionManager.DetectObjects(questCam);
                lastDetectionTime = Time.time;
            }
        }
    }
}
