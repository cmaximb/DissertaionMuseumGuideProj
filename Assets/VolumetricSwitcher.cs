using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Depthkit;
using UnityEngine.Video;
using System;
using JetBrains.Annotations;

[Serializable]
public abstract class BaseActionTransition
{
    //protected GameObject riggedModel;

    [NonSerialized]
    public GameObject riggedModel;
    protected VolumetricSwitcher switcher;

    public abstract string GetNames();

    public virtual void Init(GameObject riggedModel, VolumetricSwitcher switcher)
    {
        this.riggedModel = riggedModel;
        //riggedModel.AddComponent<Animator>();
        this.switcher = switcher;
    }

    public abstract GameObject GetVolumetricObject();

    public abstract void SwitchMode();
}

[Serializable]
//[UnityEngine.SerializeReference]
public class Action : BaseActionTransition
{
    public GameObject depthkitObject;
    public double depthkitOffset = 0.0; // Obj.start_time;
   
    public AudioClip voiceover;
    private AudioSource Audio;
    public float AudioOffset = 0f;
    public AnimationClip Marionette;

    private bool isUsingRiggedModel = false;

    private float currentTime = 0f;

    public override string GetNames()
    {
        return riggedModel != null ? riggedModel.name : "No model";
    }

    public override void Init(GameObject riggedModel, VolumetricSwitcher switcher)
    {
        base.Init(riggedModel, switcher);
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
    
    public override void SwitchMode()
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
            depthkitVideoPlayer.time = (currentTime * state.length) + 0.01; // Fix the inaccuracy from floating point numbers
            Debug.Log($"Depthkit Time: {(currentTime * state.length) + 0.01}");
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

    public override GameObject GetVolumetricObject()
    {
        return depthkitObject;
    }
}

public class Transition : BaseActionTransition
{
    public string transitionName;
    public AnimationClip walkingMovement;

    public override string GetNames()
    {
        return riggedModel != null ? riggedModel.name : "No model";
    }

    public override void Init(GameObject riggedModel, VolumetricSwitcher switcher)
    {
        base.Init(riggedModel, switcher);
    }

    public override GameObject GetVolumetricObject()
    {
        return null;
    }
    public override void SwitchMode() {}
}

public class VolumetricSwitcher : MonoBehaviour
{
    //private static DepthKitObject Obj = new DepthKitObject();
    //public GameObject depthkitObject = Obj.depthkitObject;
    //public GameObject depthkitObject;
    //public double depthkitOffset = 0.0; // Obj.start_time;
    public GameObject riggedModel;
    public double riggedModelOffset = 0.0;
    //public AudioClip voiceover;
    private AudioSource Audio;
    //public float AudioOffset = 0f;

    [SerializeReference]
    public List<BaseActionTransition> steps = new List<BaseActionTransition>()
    {
        new Action() // Start with an Action
    };

    private bool isUsingRiggedModel;
    private bool finishPlaying = false;

    private bool initialised = false;

    private int CurrentStep = 0;
    private List<Alignments> alignments;

    // Start is called before the first frame update
    void Start()
    {
        //depthkitObject.GetComponent<VideoPlayer>().time = depthkitOffset;
        //depthkitObject.GetComponent<VideoPlayer>().Play();

        //AllignObjects();
        //MatchScale();


    }

    private void OnEnable()
    {
        // Ensure the first item is always an ActionType
        if (steps == null || steps.Count == 0 || !(steps[0] is Action))
        {
            steps = new List<BaseActionTransition> {};
            BaseActionTransition newStep = new Transition();
            newStep.Init(riggedModel, this);
            steps.Add(newStep);
        }
    }

    //void AllignObjects()
    //{
    //    depthkitObject.transform.position = riggedModel.transform.position;
    //    //depthkitObject.transform.rotation = riggedModel.transform.rotation;

    //    Debug.Log($"Rigged Model rotation: {riggedModel.transform.rotation.eulerAngles}, Depthkit object rotation: {depthkitObject.transform.rotation.eulerAngles}");
    //    Debug.Log($"Rigged Model scale: {riggedModel.transform.localScale}, Depthkit Object scale: {depthkitObject.transform.localScale}");
    //    Debug.Log($"Rigged Model location: {riggedModel.transform.position}, Depthkit Object location: {depthkitObject.transform.position}");
    //}

    public void Initialise(List<Alignments> alignments)
    {
        this.alignments = alignments;

        //if (steps[0].GetVolumetricObject() == null)
        //    throw new Exception("Initial step must be Action");

        //steps[0].GetVolumetricObject().GetComponent<VideoPlayer>().playOnAwake = false;
        
        Audio = gameObject.AddComponent<AudioSource>();

        initialised = true;
        //SwitchMode();
    }

    // Update is called once per frame
    void Update() {

    }

    public List<Alignments> GetAlignments()
    {
        return alignments;
    }

    //void MatchScale()
    //{
    //    // Get bounding box sizes
    //    Bounds depthkitBounds = GetObjectBounds(depthkitObject);
    //    Bounds riggedBounds = GetObjectBounds(riggedModel);

    //    float scaleFactor = depthkitBounds.size.y / riggedBounds.size.y; // Match height
    //    riggedModel.transform.localScale *= scaleFactor;
    //}

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
        steps[CurrentStep].SwitchMode();
    }

    public void triggerTransition()
    {
        CurrentStep = CurrentStep + 1;
    }

    //public void SwitchMode(Quaternion degrees)
    //{
    //    if (!isUsingRiggedModel)
    //    {
    //        depthkitObject.transform.rotation = degrees;
    //    }
    //    SwitchMode();
    //}

    public bool clipEnded()
    {
        return false; // steps[CurrentStep].riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime == 1;
    }

    public GameObject GetRiggedModel()
    {
        return steps[CurrentStep].riggedModel;
    }

    public bool checkIfUsingRiggedModel()
    {
        return isUsingRiggedModel;
    }

    public bool CheckIfInitialised()
    {
        return initialised;
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
