using UnityEngine;
using UnityEngine.AI;
using Meta.XR.MRUtilityKit;
using Unity.AI.Navigation;
using System.Collections;
public class Wander : MonoBehaviour
{
    private MRUKRoom currentRoom;
    public NavMeshAgent agent;
    public NavMeshSurface navSurface;
    public Animator animator;
    public float minIdleTime = 3f;
    public float maxIdleTime = 10f;
    void Start()
    {
        OVRPlugin.systemDisplayFrequency = 72.0f;
        agent.enabled = false;
        if (navSurface == null)
        {
            navSurface = FindFirstObjectByType<NavMeshSurface>();
        }
        MRUK.Instance.RegisterSceneLoadedCallback(() => {
            StartCoroutine(InitializeAndLoop());
        });
    }
    void Update()
    {
        if (currentRoom == null || !agent.isActiveAndEnabled)
        {
            return;
        }
        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }
    private IEnumerator InitializeAndLoop()
    {
        yield return new WaitForSeconds(2.0f);
        if (navSurface != null)
        {
            navSurface.BuildNavMesh();
            agent.agentTypeID = navSurface.agentTypeID;
        }
        yield return new WaitForEndOfFrame();
        currentRoom = MRUK.Instance.GetCurrentRoom();
        agent.enabled = true;
        while (true)
        {
            yield return WanderToNewSpot();
            float idleTime = Random.Range(minIdleTime, maxIdleTime);
            Debug.Log($"[Cat] Chilling for {idleTime:F1} seconds...");
            yield return new WaitForSeconds(idleTime);
        }
    }
    private IEnumerator WanderToNewSpot()
    {
        // 1. 70% chance to do exactly what your "Floor Script" does
        bool isFloorTarget = Random.value > 0.3f;
        LabelFilter filter = isFloorTarget
            ? new LabelFilter(MRUKAnchor.SceneLabels.FLOOR)
            : new LabelFilter(MRUKAnchor.SceneLabels.TABLE | MRUKAnchor.SceneLabels.BED | MRUKAnchor.SceneLabels.COUCH);

        bool found = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            0.3f,
            filter,
            out Vector3 randomPos,
            out _
        );

        if (found)
        {
            // 2. USE YOUR WORKING FLOOR LOGIC FOR FLOOR SPOTS
            if (isFloorTarget)
            {
                // Use your 2.0f radius that you know works!
                if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                    yield return new WaitUntil(() => !agent.pathPending);
                    while (agent.remainingDistance > agent.stoppingDistance)
                    {
                        yield return null;
                    }
                }
            }
            // 3. USE STRICT LOGIC FOR FURNITURE (Jumping)
            else
            {
                int roomLayerMask = LayerMask.GetMask("Default", "RoomMesh");

                // Check if there is a "ceiling" (desk/bed top) above the spot
                bool isBlocked = Physics.Raycast(randomPos + Vector3.up * 0.01f, Vector3.up, 0.6f, roomLayerMask);

                if (!isBlocked)
                {
                    // Use a TINY radius (0.1f). 
                    // If there is no NavMesh EXACTLY on top of the bed, the cat won't go there.
                    if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 0.15f, NavMesh.AllAreas))
                    {
                        Debug.Log("<color=yellow>[Cat] Jumping up to furniture!</color>");
                        agent.Warp(hit.position);
                        // Wait a moment so it doesn't immediately try to walk off
                        yield return new WaitForSeconds(1.0f);
                    }
                }
            }
        }
    }
}
