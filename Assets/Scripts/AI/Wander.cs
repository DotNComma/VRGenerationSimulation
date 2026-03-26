using UnityEngine;
using UnityEngine.AI;
using Meta.XR.MRUtilityKit;
using Unity.AI.Navigation;
using System.Collections;
public class Wander : MonoBehaviour
{
    private MRUKRoom currentRoom;
    private float nextMeowTime = 0f;
    private bool isBeingPetted = false;

    public NavMeshAgent agent;
    public NavMeshSurface navSurface;
    public Animator animator;

    public float minIdleTime = 3f;
    public float maxIdleTime = 10f;

    public AudioSource catAudio;
    public AudioClip meowClip;
    public AudioClip purrClip;

    public float minMeowInterval = 5f;
    public float maxMeowInterval = 15f;

    private void Start()
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

    private void Update()
    {
        if (currentRoom == null || !agent.isActiveAndEnabled)
        {
            return;
        }

        float currentSpeed = agent.velocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);

        if (currentSpeed > 0.1f && !isBeingPetted && Time.time >= nextMeowTime)
        {
            PlayRandomMeow();
        }
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
            yield return new WaitForSeconds(idleTime);
        }
    }

    private IEnumerator WanderToNewSpot()
    {
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
            if (isFloorTarget)
            {
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
            else
            {
                int roomLayerMask = LayerMask.GetMask("Default", "RoomMesh");

                bool isBlocked = Physics.Raycast(randomPos + Vector3.up * 0.01f, Vector3.up, 0.6f, roomLayerMask);

                if (!isBlocked)
                {
                    if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, 0.15f, NavMesh.AllAreas))
                    {
                        Debug.Log("<color=yellow>[Cat] Jumping up to furniture!</color>");
                        agent.Warp(hit.position);
                        yield return new WaitForSeconds(1.0f);
                    }
                }
            }
        }
    }

    private void PlayRandomMeow()
    {
        if (catAudio != null && meowClip != null)
        {
            catAudio.PlayOneShot(meowClip);
            nextMeowTime = Time.time + Random.Range(minMeowInterval, maxMeowInterval);
        }
    }

    public void StartPetting()
    {
        if (!agent.enabled)
        {
            return;
        }

        Debug.Log("<color=magenta>[Cat] Interaction Started: Petting!</color>");
        isBeingPetted = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        animator.SetBool("IsPet", true);
        animator.SetFloat("Speed", 0);

        if (catAudio != null && purrClip != null)
        {
            catAudio.Stop();
            catAudio.clip = purrClip;
            catAudio.loop = true;
            catAudio.Play();
        }
    }

    public void StopPetting()
    {
        if (!agent.enabled)
        {
            return;
        }

        Debug.Log("<color=magenta>[Cat] Interaction Ended.</color>");
        isBeingPetted = true;

        agent.isStopped = false;

        animator.SetBool("IsPet", false);

        if (catAudio != null)
        {
            catAudio.Stop();
            catAudio.loop = false;
            nextMeowTime = Time.time + Random.Range(minMeowInterval, maxMeowInterval);

            if (meowClip != null)
            {
                catAudio.PlayOneShot(meowClip);
            }
        }
    }
}
