using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.UIElements;

public class DetectionWorldBridge : MonoBehaviour
{
    private bool hasSpawned = false;

    public GameObject catPrefab;

    public void OnObjectDetected(Vector2 screenPosition)
    {
        try
        {
            if (hasSpawned)
            {
                return;
            }
                
            if (catPrefab == null)
            {
                Debug.LogError("[Bridge] Cat Prefab is MISSING in the Inspector!");
                return;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("[Bridge] Camera.main is null. Ensure your OVRCameraRig has the 'MainCamera' tag!");
                return;
            }

            Ray ray = mainCam.ViewportPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0));

            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null)
            {
                Debug.LogWarning("[Bridge] No MRUK Room found. Waiting for Scene Scan...");
                return;
            }

            if (room.Raycast(ray, 10f, out RaycastHit hit))
            {
                Instantiate(catPrefab, hit.point, Quaternion.identity);
                hasSpawned = true;
            }
            else
            {
                Debug.LogWarning("[Bridge] Ray fired but hit no mesh. Is your room mesh loaded?");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bridge] CRASH: {e.Message} \n {e.StackTrace}");
        }
    }

    public void ResetCat()
    {
        // Find the cat in the scene and destroy it
        Wander existingCat = FindFirstObjectByType<Wander>();
        if (existingCat != null)
        {
            Destroy(existingCat.gameObject);
        }

        // Allow the bridge to spawn a new one next time it detects the bottle
        hasSpawned = false;
        Debug.Log("<color=red>[Bridge] Cat Reset. Ready for new detection.</color>");
    }
}
