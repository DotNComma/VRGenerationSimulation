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

    // Start is called before the first frame update
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

    // Update is called once per frame
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
        bool found = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            0.3f,
            new LabelFilter(MRUKAnchor.SceneLabels.FLOOR),
            out Vector3 randomPos,
            out _
        );

        if (found && NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            yield return new WaitUntil(() => !agent.pathPending);
            while (agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }

            Debug.Log("[Cat] Arrived at spot.");
        }
    }
}
