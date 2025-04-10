using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class SwitchingCondition : MonoBehaviour
{
    public GameObject riggedmodel;
    private VolumetricSwitcher switcher;
    private List<Alignments> alignments;
    private Vector3 currentTalkLocation;
    RiggedGuide guide;
    NavMeshAgent agent;
    private PlayerMovement player;
    private bool hasTransitioned = false;
    private bool startTransition = false;
    private float stoppingDistanceReset = 0;

    void Start()
    {
        switcher = GetComponent<VolumetricSwitcher>();
        player = switcher.getPlayer();
        guide = riggedmodel.GetComponent<RiggedGuide>();
        agent = guide.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (switcher.GetState() == guideStates.Align || player == null) { return; }

        if (alignments == null)
        {
            if (switcher.GetAlignments() == null)
            {
                return;
            }

            alignments = switcher.GetAlignments();
        }
        

        //if (switcher.GetState() == guideStates.Talk) { return; }

       
        if (switcher.GetState() == guideStates.Walk)
        {
            guide.followPlayer();
        }

        foreach (Alignments alignment in alignments)
        {
            Vector3 co_ords = new Vector3(alignment.riggedModelTransforms.location.x, riggedmodel.transform.position.y, alignment.riggedModelTransforms.location.z);
            //co_ords += new Vector3(0, 0, 7);

            if ((player.distanceToObject(alignment.riggedModelTransforms.location) < 0.5f) && switcher.GetState() == guideStates.Walk)
            {
                switcher.SwitchState(guideStates.Transition);
                stoppingDistanceReset = agent.stoppingDistance;
                agent.stoppingDistance = 0;
                guide.walkToLocation(co_ords);
                currentTalkLocation = co_ords;
                //switcher.SwitchState(guideStates.Transition);
                
                break;
            }
        }

        // Activates when player is transitioning and in the right place
        // Starts the transition to necessary pose
        if ((!agent.pathPending && !agent.hasPath && agent.velocity.sqrMagnitude == 0f) && switcher.GetState() == guideStates.Transition && startTransition == false)
        {
            startTransition = true;
            riggedmodel.GetComponent<Animator>().CrossFade("museum marionette cropped 5", 0.2f, 0, 0.0f);
        }

        AnimatorStateInfo stateInfo = riggedmodel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);

        // Activates when player is transitioning and in the right state
        // Switches to the rigged model
        if (switcher.GetState() == guideStates.Transition && stateInfo.IsName("museum marionette cropped 5") && stateInfo.normalizedTime >= 0f && !hasTransitioned)
        {
            switcher.SwitchState(guideStates.Talk);
            hasTransitioned = true;
            agent.isStopped = true;
            switcher.SwitchMode();
        }

        // Activates when the player leaves the proximity of the talk
        // Guide turns back to rigged model and continues following the player
        if ((player.distanceToObject(currentTalkLocation) + agent.stoppingDistance) > switcher.GetProximityToSwitch() && switcher.GetState() == guideStates.Talk)
        {
            agent.stoppingDistance = stoppingDistanceReset;
            agent.isStopped = false;
            switcher.SwitchMode();
            guide.followPlayer();
            hasTransitioned = false;
            startTransition = false;
            switcher.SwitchState(guideStates.Walk);
        }

        if (switcher.clipEnded() && switcher.checkIfUsingRiggedModel())
        {
            switcher.triggerTransition();
        }
    }
}
