using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Depthkit;
using UnityEngine.Video;
using System;
using JetBrains.Annotations;
using System.Linq;

public enum guideStates { Align, Walk, Transition, Talk }

//[Serializable]
//public abstract class BaseActionTransition
//{
//    //protected GameObject riggedModel;

//    [NonSerialized]
//    public GameObject riggedModel;
//    protected VolumetricSwitcher switcher;

//    public abstract string GetNames();

//    public virtual void Init(GameObject riggedModel, VolumetricSwitcher switcher)
//    {
//        this.riggedModel = riggedModel;
//        //riggedModel.AddComponent<Animator>();
//        this.riggedModel = riggedModel;
//    }

//    public abstract GameObject GetVolumetricObject();

//    public abstract void SwitchMode();
//}

[Serializable]
public class Action
{
    [UnityEngine.SerializeReference]
    public GameObject depthkitObject;

    [UnityEngine.SerializeReference]
    public AudioClip voiceover;
    private AudioSource Audio;
    [UnityEngine.SerializeReference]
    public AnimationClip Marionette;

    private bool isUsingRiggedModel = false;
    private float currentTime = 0f;

    private VolumetricSwitcher switcher;
    private GameObject riggedModel;

    public void Init(GameObject riggedModel, VolumetricSwitcher switcher)
    {
        this.riggedModel = riggedModel;
        this.switcher = switcher;
    }

    public void Activated()
    {
        if (voiceover != null)
        {
            switcher.playAudio(voiceover);
        }
        else
        {
            Debug.LogError("No audio clip assigned!");
        }
        
        var animator = riggedModel.GetComponent<Animator>();
        animator.enabled = true;
        animator.speed = 1.0f;
        animator.Update(0f);
    }
    
    public void SwitchMode()
    {
        if (isUsingRiggedModel)
        {
            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            //Debug.Log($"Length: {depthkitVideoPlayer.length}");
            Debug.Log($" Depthkit Time: {depthkitVideoPlayer.time}");
            float normalizedTime = (float)((depthkitVideoPlayer.time) / depthkitVideoPlayer.length); // TODO: Make this actually find the right time
            Debug.Log(normalizedTime);

            setObjectVisibility(depthkitObject, false);
            setObjectVisibility(riggedModel, true);
            //riggedMovements.enabled = true;

            Animator animator = riggedModel.GetComponent<Animator>();
            var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            overrideController["Default"] = Marionette;
            riggedModel.GetComponent<Animator>().Play("Default", 0, normalizedTime);
        }
        else
        {
            AnimatorStateInfo state = riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            currentTime = state.normalizedTime;

            //AllignObjects();
            setObjectVisibility(depthkitObject, true);

            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            depthkitVideoPlayer.time = (currentTime * state.length); 
            Debug.Log($"Depthkit Time: {(currentTime * state.length)}");
            depthkitVideoPlayer.Play();

            setObjectVisibility(riggedModel, false);
        }

        isUsingRiggedModel = !isUsingRiggedModel;
    }

    private void setObjectVisibility(GameObject model, bool visible)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = visible;
    }

    public GameObject GetVolumetricObject()
    {
        return depthkitObject;
    }
}

//public class Transition : BaseActionTransition
//{
//    public string transitionName;
//    public AnimationClip walkingMovement;

//    public override string GetNames()
//    {
//        return riggedModel != null ? riggedModel.name : "No model";
//    }

//    public override void Init(GameObject riggedModel, VolumetricSwitcher switcher)
//    {
//        base.Init(riggedModel, switcher);
//    }

//    public override GameObject GetVolumetricObject()
//    {
//        return null;
//    }
//    public override void SwitchMode() {}
//}

public class VolumetricSwitcher : MonoBehaviour
{

    public GameObject riggedModel;
    public double riggedModelOffset = 0.0;
    private AudioSource Audio;

    [SerializeReference]
    public List<Action> exhibitTalks = new List<Action>()
    {
        new Action() // Start with an Action
    };

    private bool isUsingRiggedModel;
    private bool finishPlaying = false;

    private int CurrentStep = 0;

    private List<Alignments> alignments = new List<Alignments>();
    public bool AssignAlignmentsEveryTime = false;

    public GameObject player;
    public Camera playerCam;

    private guideStates currentState;

    private void Start()
    {
        SwitchToPlayerMode(false);
        SwitchState(guideStates.Align);
    }

    public void SwitchState(guideStates newState) {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case guideStates.Align:
                MoveModels mover = this.GetComponentInChildren<MoveModels>();
                if (mover.getInitialised()) { return; }

                List<GameObject> volumetricModels = exhibitTalks.Select(item => item.depthkitObject).ToList();
                mover.initialise(volumetricModels, riggedModel, alignments, AssignAlignmentsEveryTime);
                break;
            case guideStates.Walk:
                if (alignments.Count == 0 || alignments == null) {
                    throw new Exception("The alignments have not yet been assigned!");
                }

                SwitchToPlayerMode(true);
                
                Audio = gameObject.AddComponent<AudioSource>();
                break;
            case guideStates.Transition:
                break;
            case guideStates.Talk:
                break;
        }
    }

    public void SwitchToPlayerMode(bool enable)
    {
        player.SetActive(enable);  // Enables/disables player movement
        playerCam.gameObject.SetActive(enable);  // Enables/disables the camera

        if (enable)
        {
            Cursor.lockState = CursorLockMode.Locked; // Lock cursor for FPS movement
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;  // Unlock cursor when not in player mode
            Cursor.visible = true;
        }
    }

    public void SetAlignments(List<Alignments> alignments)
    {
        this.alignments = alignments;
    }

    public List<Alignments> GetAlignments()
    {
        return alignments;
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds();

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    public void SwitchMode()
    {
        if (currentState == guideStates.Talk)
        {
            exhibitTalks[CurrentStep].SwitchMode();
        }
    }

    public void triggerTransition()
    {
        CurrentStep = CurrentStep + 1;
    }

    public bool clipEnded()
    {
        return false; // steps[CurrentStep].riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime == 1;
    }

    //public GameObject GetRiggedModel()
    //{
    //    return exhibitTalks[CurrentStep].riggedModel;
    //}

    public bool checkIfUsingRiggedModel()
    {
        return isUsingRiggedModel;
    }

    public guideStates GetState()
    {
        return currentState;
    }

    private void setDepthkitTime()
    {

    }

    //public double getDepthkitOffset()
    //{
    //    return depthkitOffset;
    //}

    //public double getRiggedOffset()
    //{
    //    return riggedModelOffset;
    //}

    public void playAudio(AudioClip voiceover)
    {
        Audio.clip = voiceover;
        Audio.loop = false;
        StartCoroutine(loopAudio());
    }

    IEnumerator loopAudio()
    {
        while (!finishPlaying)
        {
            //Audio.time = AudioOffset;
            Audio.Play();

            yield return new WaitForSeconds(Audio.clip.length); // add - AudioOffset
        }
    }
}

public class DepthKitObject
{
    public GameObject depthkitObject;
    public double start_time = 0.0;
}
