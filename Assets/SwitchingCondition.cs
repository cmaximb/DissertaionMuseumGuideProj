using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class SwitchingCondition : MonoBehaviour
{
    public GameObject riggedmodel;
    private VolumetricSwitcher switcher;
    private List<Alignments> alignments;
    private Vector3 currentTalkLocation;

    void Start()
    {
        switcher = GetComponent<VolumetricSwitcher>();
    }

    void Update()
    {
        if (switcher.GetState() != guideStates.Align && alignments == null)
        {
            alignments = switcher.GetAlignments();
        }
        if (alignments == null)
        {
            return;
        }

        if (switcher.GetState() == guideStates.Talk) { return; }

        RiggedGuide guide = riggedmodel.GetComponent<RiggedGuide>();
        if (switcher.GetState() == guideStates.Walk)
        {
            guide.followPlayer();
        }
        

        foreach (Alignments alignment in alignments)
        {
            if (guide.distanceToObject(alignment.riggedModelTransforms.location) < 0.5f)
            {
                guide.walkToLocation(alignment.riggedModelTransforms.location);
                currentTalkLocation = alignment.riggedModelTransforms.location;
                switcher.SwitchState(guideStates.Transition);
                break;
            }
        }

        if (guide.distanceToObject(currentTalkLocation) > 1.0f && switcher.GetState() == guideStates.Talk)
        {
            guide.followPlayer();
            switcher.SwitchState(guideStates.Walk);
            switcher.SwitchMode();
        }
        if (switcher.clipEnded() && switcher.checkIfUsingRiggedModel())
        {
            switcher.triggerTransition();
        }
    }
}
