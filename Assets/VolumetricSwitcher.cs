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

}

[Serializable]
//[UnityEngine.SerializeReference]
public class Action : BaseActionTransition
{
    public GameObject depthkitObject;
    public double depthkitOffset = 0.0; // Obj.start_time;
    public GameObject riggedModel;
    public double riggedModelOffset = 0.0;
    public AudioClip voiceover;
    private AudioSource Audio;
    public float AudioOffset = 0f;
}

public class Transition : BaseActionTransition
{
    public string transitionName;
}

public class VolumetricSwitcher : MonoBehaviour
{
    //private static DepthKitObject Obj = new DepthKitObject();
    //public GameObject depthkitObject = Obj.depthkitObject;
    public GameObject depthkitObject;
    public double depthkitOffset = 0.0; // Obj.start_time;
    public GameObject riggedModel;
    public double riggedModelOffset = 0.0;
    public AudioClip voiceover;
    private AudioSource Audio;
    public float AudioOffset = 0f;

    [SerializeReference]
    public List<BaseActionTransition> steps = new List<BaseActionTransition>()
    {
        new Action() // Start with an Action
    };

    public bool showing;

    public bool StartWithRiggedModel = false;
    private bool isUsingRiggedModel;
    private bool finishPlaying = false;

    private float currentTime = 0f;
    private float clipDuration;
    private bool initialised = false;

    private int CurrentAction = 0;

    // Start is called before the first frame update
    void Start()
    {
        //depthkitObject.GetComponent<VideoPlayer>().time = depthkitOffset;
        //depthkitObject.GetComponent<VideoPlayer>().Play();

        //AllignObjects();
        //MatchScale();


    }

    void AllignObjects()
    {
        depthkitObject.transform.position = riggedModel.transform.position;
        //depthkitObject.transform.rotation = riggedModel.transform.rotation;

        Debug.Log($"Rigged Model rotation: {riggedModel.transform.rotation.eulerAngles}, Depthkit object rotation: {depthkitObject.transform.rotation.eulerAngles}");
        Debug.Log($"Rigged Model scale: {riggedModel.transform.localScale}, Depthkit Object scale: {depthkitObject.transform.localScale}");
        Debug.Log($"Rigged Model location: {riggedModel.transform.position}, Depthkit Object location: {depthkitObject.transform.position}");
    }

    public void Initialise()
    {
        depthkitObject.GetComponent<VideoPlayer>().playOnAwake = false;

        isUsingRiggedModel = StartWithRiggedModel;

        Audio = gameObject.AddComponent<AudioSource>();

        if (voiceover != null)
        {
            Audio.clip = voiceover;
            Audio.loop = false;
            StartCoroutine(loopAudio());
        }
        else
        {
            Debug.LogError("No audio clip assigned!");
        }

        var animator = riggedModel.GetComponent<Animator>();
        animator.enabled = true;
        animator.speed = 1.0f;
        animator.Update(0f);

        initialised = true;
        SwitchMode();
    }

    // Update is called once per frame
    void Update() {

    }

    void MatchScale()
    {
        // Get bounding box sizes
        Bounds depthkitBounds = GetObjectBounds(depthkitObject);
        Bounds riggedBounds = GetObjectBounds(riggedModel);

        float scaleFactor = depthkitBounds.size.y / riggedBounds.size.y; // Match height
        riggedModel.transform.localScale *= scaleFactor;
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

            riggedModel.GetComponent<Animator>().Play("museum talk 1", 0, normalizedTime);
        }
        else
        {
            AnimatorStateInfo state = riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            currentTime = state.normalizedTime;

            //AllignObjects();
            setObjectVisibility(depthkitObject, true);

            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            depthkitVideoPlayer.time = (currentTime * state.length) - riggedModelOffset + 0.01; // Fix the inaccuracy from floating point numbers
            Debug.Log($"Depthkit Time: {(currentTime * state.length) - riggedModelOffset + 0.01}");
            depthkitVideoPlayer.Play();

            setObjectVisibility(riggedModel, false);
        }

        isUsingRiggedModel = !isUsingRiggedModel;
    }

    public void triggerTransition()
    {

    }

    private void setObjectVisibility(GameObject model, bool visible)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = visible;
    }

    public void SwitchMode(Quaternion degrees)
    {
        if (!isUsingRiggedModel)
        {
            depthkitObject.transform.rotation = degrees;
        }
        SwitchMode();
    }

    public bool clipEnded()
    {
        return riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime == 1;
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

    public double getDepthkitOffset()
    {
        return depthkitOffset;
    }

    public double getRiggedOffset()
    {
        return riggedModelOffset;
    }

    IEnumerator loopAudio()
    {
        while (!finishPlaying)
        {
            Audio.time = AudioOffset;
            Audio.Play();

            yield return new WaitForSeconds(voiceover.length - AudioOffset);
        }
    }
}

public class DepthKitObject
{
    public GameObject depthkitObject;
    public double start_time = 0.0;
}
