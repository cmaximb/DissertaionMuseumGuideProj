using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchingCondition : MonoBehaviour
{
    public GameObject volumetricObject;
    public GameObject riggedmodel;
    private VolumetricSwitcher switcher;
    private List<Alignments> alignments;

    void Start()
    {
        switcher = GetComponent<VolumetricSwitcher>();
    }

    void Update()
    {
        if (switcher.CheckIfInitialised() && alignments == null)
        {
            alignments = switcher.GetAlignments();
        }

        RiggedGuide guide = riggedmodel.GetComponent<RiggedGuide>();
        guide.followPlayer();
        //foreach (Alignments alignment in alignments)
        //{
        //    if (guide.distanceToObject(alignment.riggedModelTransforms.location) < 0.5f)
        //    {

        //    }
        //}


        if (Input.GetKeyDown(KeyCode.Space) && switcher.CheckIfInitialised())
        {
            switcher.SwitchMode();
        }
        if (switcher.clipEnded() && switcher.checkIfUsingRiggedModel())
        {
            switcher.triggerTransition();
        }
    }
}
