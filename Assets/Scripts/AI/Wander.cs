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

    private IEnumerator ResumeWanderAfterArrival()
    {
        yield return new WaitUntil(() => !agent.pathPending);
        while (agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }
        PlayRandomMeow();
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(InitializeAndLoop());
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
        isBeingPetted = false;

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

    public void ComeToUser()
    {
        if (agent == null || !agent.isActiveAndEnabled)
        {
            return;
        }

        StopAllCoroutines();

        Vector3 playerPos = Camera.main.transform.position;

        // We want the floor position below the player
        if (NavMesh.SamplePosition(playerPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            Debug.Log("<color=green>[Cat] Coming to player!</color>");

            StartCoroutine(ResumeWanderAfterArrival());
        }
    }
}
