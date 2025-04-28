using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class SwitchingCondition : MonoBehaviour
{
    private GameObject riggedmodel;
    private VolumetricSwitcher switcher;
    private List<Alignments> alignments;
    private Vector3 currentTalkLocation;
    private string currentTalkName;
    RiggedGuide guide;
    NavMeshAgent agent;
    private PlayerMovement player;
    private float stoppingDistanceReset = 0;
    private static float TURN_SPEED = 100.0f;
    private bool coRoutineStarted = false;
    private int alignmentReference = -1;

    void Start()
    {
        switcher = GetComponent<VolumetricSwitcher>();
        player = switcher.getPlayer();
        riggedmodel = switcher.getRiggedModel();
        guide = riggedmodel.GetComponent<RiggedGuide>();
        agent = guide.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (switcher.GetState() == guideStates.Align || player == null || switcher.GetState() == guideStates.Transition) { return; }

        if (alignments == null)
        {
            if (switcher.GetAlignments() == null)
            {
                return;
            }

            alignments = switcher.GetAlignments();
        }
       
        if (switcher.GetState() == guideStates.Walk)
        {
            guide.followPlayer();
        }

        int i = 0;

        foreach (Alignments alignment in alignments)
        {
            Vector3 co_ords = new Vector3(alignment.riggedModelTransforms.location.x, riggedmodel.transform.position.y, alignment.riggedModelTransforms.location.z);
            //co_ords += new Vector3(0, 0, 7);

            if ((player.distanceToObject(alignment.riggedModelTransforms.location) < 0.5f) && switcher.GetState() == guideStates.Walk && !coRoutineStarted)
            {
                // Ensure Co-routine can't be set twice by consecutive calls to Update()
                coRoutineStarted = true;
                // Update guideStates state machine
                switcher.SwitchState(guideStates.Transition);
                // So that the stopping distance can be reverted when moved out of talk phase
                stoppingDistanceReset = agent.stoppingDistance;
                // Resetting Stopping distance prevents model from getting "stuck" colliding with player after singular call to WalkToLocation()
                agent.stoppingDistance = 0;
                // Lets agent know where to walk to and keeps track of current talk
                currentTalkLocation = co_ords;
                // Update buffer containing current talk name, so the right motion is triggered
                currentTalkName = alignment.name;
                // Change the reference to know which element in the list of volumetric videos needs to be switched
                alignmentReference = i;

                // Stop all coroutines then start sequence for guide to transition to talk
                StopAllCoroutines();
                Coroutine transitionCoroutine = StartCoroutine(TransitionCoRoutine());
                
                break;
            }

            i++;
        }

        // Activates when the player leaves the proximity of the talk
        // Guide turns back to rigged model and continues following the player
        if ((player.distanceToObject(currentTalkLocation) + agent.stoppingDistance) > switcher.GetProximityToSwitch() && switcher.GetState() == guideStates.Talk)
        {
            agent.stoppingDistance = stoppingDistanceReset;
            agent.isStopped = false;
            switcher.SwitchMode(alignmentReference);
            alignmentReference = -1;
            guide.followPlayer();
            switcher.SwitchState(guideStates.Walk);
        }
    }

    IEnumerator TransitionCoRoutine()
    {
        guide.walkToLocation(currentTalkLocation);

        while (agent.pathPending || agent.remainingDistance > 0.01)
        {
            yield return null;
        }

        // Activates when player is transitioning and in the right place
        // Starts the transition to necessary pose
        Quaternion targetRotation = alignments[0].riggedModelTransforms.rotation;
        agent.isStopped = true;

        float currentY = agent.transform.eulerAngles.y;
        float targetY = targetRotation.eulerAngles.y;

        float deltaY = Mathf.DeltaAngle(currentY, targetY);

        Animator animator = riggedmodel.GetComponent<Animator>();
        //animator.SetFloat("Speed", 0);
        agent.ResetPath();

        // Get state info
        AnimatorStateInfo stateInfo = riggedmodel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
        
        if (Mathf.Abs(deltaY) < 0.5f)
        {
            animator.SetBool("TurningLeft", false);
            animator.SetBool("TurningRight", false);
        }
        else
        {
            bool turnRight = (deltaY > 0);

            animator.SetBool("TurningRight", turnRight);
            animator.SetBool("TurningLeft", !turnRight);
        }

        animator.Update(0);

        while (stateInfo.IsName("left") || stateInfo.IsName("right"))
        {
            yield return null;
        }

        animator.SetBool("TurningLeft", false);
        animator.SetBool("TurningRight", false);

        while (Quaternion.Angle(riggedmodel.transform.rotation, targetRotation) > 0.5f) 
        {
            guide.rotateStationary(transform.rotation, targetRotation, TURN_SPEED);

            yield return null;
        }

        // Rotates model any final distance to exact rotation
        riggedmodel.transform.rotation = targetRotation;

        // Start transition to talk
        riggedmodel.GetComponent<Animator>().CrossFade(currentTalkName, 0.2f, 0, 0.0f);

        // Stops activating when player is in the right state
        while (!(stateInfo.IsName(currentTalkName) && stateInfo.normalizedTime >= 0f)) 
        {
            stateInfo = riggedmodel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            yield return null;
        }

        // Then switch to the rigged model
        switcher.SwitchState(guideStates.Talk);
        switcher.SwitchMode(alignmentReference);
        coRoutineStarted = false;
    }
}
