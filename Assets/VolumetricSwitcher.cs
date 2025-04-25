using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Depthkit;
using UnityEngine.Video;
using System;
using JetBrains.Annotations;
using System.Linq;
using UnityEngine.Rendering.HighDefinition;

public enum guideStates { Align, Walk, Transition, Talk, None }

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

    //[UnityEngine.SerializeReference]
    //public AudioClip voiceover;
    //private AudioSource Audio;
    [UnityEngine.SerializeReference]
    public AnimationClip Marionette;

    private bool isUsingRiggedModel = false;
    private float currentTime = 0f;

    private GameObject riggedModel;

    public void Init(GameObject riggedModel)
    {
        this.riggedModel = riggedModel;
    }

    public void Activated()
    {
       
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

            depthkitObject.GetComponent<AudioSource>().enabled = false;

            //Animator animator = riggedModel.GetComponent<Animator>();
            //var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            //overrideController["Default"] = Marionette;
            //riggedModel.transform.position = switcher.GetAlignments()[0].riggedModelTransforms.location;
            //riggedModel.transform.rotation = switcher.GetAlignments()[0].riggedModelTransforms.rotation;
            //riggedModel.GetComponent<Animator>().CrossFade("museum marionette cropped 4", 0f, 0, normalizedTime);
        }
        else
        {
            AnimatorStateInfo state = riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            currentTime = state.normalizedTime;

            //AllignObjects();
            setObjectVisibility(depthkitObject, true);

            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            playOnTimeSet(depthkitVideoPlayer, (currentTime * state.length));
            Debug.Log($"Depthkit Time: {(currentTime * state.length)}");
            depthkitVideoPlayer.Play();
            depthkitObject.GetComponent<AudioSource>().enabled = true;

            setObjectVisibility(riggedModel, false);
        }

        isUsingRiggedModel = !isUsingRiggedModel;
    }

    IEnumerator playOnTimeSet(VideoPlayer depthkitClip, float newTime)
    {
        depthkitClip.Pause();
        depthkitClip.time = newTime;

        // Wait until time is close enough (float comparisons)
        while (Mathf.Abs((float) depthkitClip.time - newTime) > 0.01f)
            yield return null;

        depthkitClip.Play();
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

    public List<Action> exhibitTalks = new List<Action>()
    {
        new Action() // Start with an Action
    };

    private bool isUsingRiggedModel;
    private bool finishPlaying = false;

    private int CurrentStep = 0;

    private List<Alignments> alignments = new List<Alignments>();
    public bool AssignAlignmentsEveryTime = false;

    public Material decalMaterial;
    private List<GameObject> decals = new List<GameObject>();

    public GameObject player;
    public Camera playerCam;
    //public Camera MainCam;

    private guideStates currentState = guideStates.None;
    public float proximityToSwitch = 1.0f;

    private void Start()
    {
        //Audio = gameObject.AddComponent<AudioSource>();
        
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
                List<String> movementNames = exhibitTalks.Select(item => item.Marionette.name).ToList();
                mover.initialise(volumetricModels, movementNames, riggedModel, alignments, AssignAlignmentsEveryTime);
                break;
            case guideStates.Walk:
                if (alignments.Count == 0 || alignments == null) {
                    throw new Exception("The alignments have not yet been assigned!");
                }

                //riggedModel.transform.position = player.transform.position;

                // Disable the hitbox for each volumetric video
                foreach (GameObject model in exhibitTalks.Select(item => item.depthkitObject).ToList())
                {
                    model.GetComponent<BoxCollider>().enabled = false;
                }
        
                var animator = riggedModel.GetComponent<Animator>();
                animator.enabled = true;
                animator.speed = 1.0f;
                ResetAllTriggers();
                animator.SetTrigger("Switch To Idle");
                animator.CrossFade("idle", 0.01f, 0, 0.0f);
                animator.Update(0f);

                EnableCircles();
                
                break;
            case guideStates.Transition:
                break;
            case guideStates.Talk:
                foreach (var talk in exhibitTalks) {
                    talk.Init(riggedModel);
                }

                DisableCircles();

                break;
        }
    }

    private void ResetAllTriggers ()
    {
        for (int i = 1; i <= 5; i++) // ToDo: Make it able to tell number of talk states
        {
            riggedModel.GetComponent<Animator>().ResetTrigger($"Play Talk {i}");
        }
    }

    private void EnableCircles()
    {
        if (decals.Count > 0)
        {
            foreach (var decal in decals)
            {
                var renderer = decal.GetComponent<Renderer>();
                renderer.enabled = true;
            }

            return;
        }

        foreach (var alignment in alignments)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Vector3 quadLocation = new Vector3(alignment.riggedModelTransforms.location.x, 0.1f, alignment.riggedModelTransforms.location.z);
            quad.transform.position = quadLocation;
            quad.transform.rotation = Quaternion.Euler(90, 0, 0); // Face upwards
            quad.transform.localScale = new Vector3(proximityToSwitch * 2f, proximityToSwitch * 2f, 1.0f);
            
            quad.GetComponent<MeshCollider>().enabled = false;

            if (decalMaterial != null)
            {
                quad.GetComponent<MeshRenderer>().material = decalMaterial;
            }
            else
            {
                Debug.LogWarning("Red circle material not assigned!");
            }

            decals.Add(quad);
        }
    }

    private void DisableCircles()
    {
        foreach (var decal in decals)
        {
            var renderer = decal.GetComponent<Renderer>();
            renderer.enabled = false;
        }
    }

    public void SwitchToPlayerMode(bool enable)
    {
        player.SetActive(enable);  // Enables/disables player movement
        playerCam.gameObject.SetActive(enable);  // Enables/disables the camera
        //MainCam.gameObject.SetActive(!enable);

        for (int i = 0; i < alignments.Count; i++)
        {
            exhibitTalks[i].depthkitObject.transform.position = alignments[i].volumetricModelTransforms.location;
        } 

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

    public void SwitchMode(int index)
    {
        if (currentState == guideStates.Talk)
        {
            exhibitTalks[index].SwitchMode();
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

    internal float GetProximityToSwitch()
    {
        return proximityToSwitch;
    }

    internal PlayerMovement getPlayer()
    {
        return player.GetComponent<PlayerMovement>();
    }
}

public class DepthKitObject
{
    public GameObject depthkitObject;
    public double start_time = 0.0;
}
