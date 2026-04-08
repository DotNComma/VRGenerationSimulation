using UnityEngine;
using Meta.XR.MRUtilityKit;

public class PresentationManager : MonoBehaviour
{
    public DetectionWorldBridge bridge;
    public GameObject occlusionMeshObject;
    public GameObject blueBoxMeshObject;

    private bool isOcclusionMode = true;

    void Update()
    {
        // 1. Reset Cat (Left Controller - X Button)
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            bridge.ResetCat();
        }

        // 2. Come to User (Right Controller - A Button)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            Wander cat = FindFirstObjectByType<Wander>();
            if (cat != null) cat.ComeToUser();
        }

        // 3. Toggle Mesh Material (Right Controller - B Button)
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            ToggleMeshVisibility();
        }
    }

    public void ToggleMeshVisibility()
    {
        isOcclusionMode = !isOcclusionMode;
        // Keep occlusionMeshObject always TRUE in the inspector
        // Only toggle the blue boxes
        if (blueBoxMeshObject != null) blueBoxMeshObject.SetActive(!isOcclusionMode);
    }
}