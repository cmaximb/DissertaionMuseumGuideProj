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

[Serializable]
public class Action
{
    [UnityEngine.SerializeReference]
    public GameObject depthkitObject;

    [UnityEngine.SerializeReference]
    public AnimationClip Marionette;

    private bool isUsingRiggedModel = false;
    private float currentTime = 0f;

    private GameObject riggedModel;

    // Passes the Talk a reference to the rigged model
    public void Init(GameObject riggedModel)
    {
        this.riggedModel = riggedModel;
    }
    
    public void SwitchMode()
    {
        if (isUsingRiggedModel)
        {
            // Find and log times
            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            Debug.Log($"Switched at Depthkit Time: {depthkitVideoPlayer.time}");
            float normalizedTime = (float)((depthkitVideoPlayer.time) / depthkitVideoPlayer.length);

            // Toggle visibility
            setObjectVisibility(depthkitObject, false);
            setObjectVisibility(riggedModel, true);

            // Stop audio from auto-playing
            depthkitObject.GetComponent<AudioSource>().enabled = false;

            // The following code can be used to actually play the objects at the necessary time rather than making them invisible:
            //
            //Animator animator = riggedModel.GetComponent<Animator>();
            //var overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            //overrideController["Default"] = Marionette;
            //riggedModel.transform.position = switcher.GetAlignments()[0].riggedModelTransforms.location;
            //riggedModel.transform.rotation = switcher.GetAlignments()[0].riggedModelTransforms.rotation;
            //riggedModel.GetComponent<Animator>().CrossFade("museum marionette cropped 4", 0f, 0, normalizedTime);
        }
        else
        {
            // Convert rigged model time to DepthKit time
            AnimatorStateInfo state = riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            currentTime = state.normalizedTime;

            // Toggle depthkit object visibility
            setObjectVisibility(depthkitObject, true);

            // Play depthKit video at equivalent time
            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            playOnTimeSet(depthkitVideoPlayer, (currentTime * state.length));
            Debug.Log($"Depthkit Time: {(currentTime * state.length)}");
            depthkitVideoPlayer.Play();
            depthkitObject.GetComponent<AudioSource>().enabled = true;

            // Toggle rigged model visibility
            setObjectVisibility(riggedModel, false);
        }

        // Alternate boolean to store which model is showing; for systems where switchmode is called blindly
        isUsingRiggedModel = !isUsingRiggedModel;
    }

    // Enumerable play method prevents first few frames of DepthKit object playing while the rest of the video loads
    IEnumerator playOnTimeSet(VideoPlayer depthkitClip, float newTime)
    {
        depthkitClip.Pause();
        depthkitClip.time = newTime;

        // Wait until time is close enough with margin of error for float conversion
        while (Mathf.Abs((float) depthkitClip.time - newTime) > 0.01f)
            yield return null;

        depthkitClip.Play();
    }

    // Toggles visibility of a game object
    private void setObjectVisibility(GameObject model, bool visible)
    {
        // Disable renderers
        var renderers = model.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = visible;
    }
}

// Volumetric Switcher class contains central logic for the system & state machine for users
public class VolumetricSwitcher : MonoBehaviour
{

    public GameObject riggedModel;
    private AudioSource Audio;

    public List<Action> exhibitTalks = new List<Action>()
    {
        new Action() // Always start with one template
    };

    private bool isUsingRiggedModel;
    private bool finishPlaying = false;

    private List<Alignments> alignments = new List<Alignments>();
    public bool AssignAlignmentsEveryTime = false;

    public Material decalMaterial;
    private List<GameObject> decals = new List<GameObject>();

    public GameObject player;
    public Camera playerCam;

    private guideStates currentState = guideStates.None;
    public float proximityToSwitch = 1.0f;

    private void Start()
    {      
        SwitchToPlayerMode(false);
        SwitchState(guideStates.Align);
        
        // Pass reference to rigged model to each group of exhibit talks containing volumetric videos
        foreach (var talk in exhibitTalks) {
            talk.Init(riggedModel);
        }
    }

    // Changes state machine to be in state passed in as long as it is a different state to the current state
    public void SwitchState(guideStates newState) {
        // Prevents accidental consecutive calls
        if (currentState == newState) return;

        // Update current state to parameter state
        currentState = newState;

        switch (currentState)
        {
            case guideStates.Align:
                // Start alignment phase
                MoveModels mover = this.GetComponentInChildren<MoveModels>();
                if (mover.getInitialised()) { return; }

                // Pass in the inspector-specified values to alignment class (MoveModels)
                List<GameObject> volumetricModels = exhibitTalks.Select(item => item.depthkitObject).ToList();
                List<String> movementNames = exhibitTalks.Select(item => item.Marionette.name).ToList();
                mover.initialise(volumetricModels, movementNames, riggedModel, alignments, AssignAlignmentsEveryTime);
                break;
            case guideStates.Walk:
                // Doesn't activate if no alignments
                if (alignments.Count == 0 || alignments == null) {
                    throw new Exception("The alignments have not yet been assigned!");
                }

                // Disable the hitbox for each volumetric video
                foreach (GameObject model in exhibitTalks.Select(item => item.depthkitObject).ToList())
                {
                    model.GetComponent<BoxCollider>().enabled = false;
                }
        
                // Default states (rigged model was frozen in alignment phase, idle is default)
                var animator = riggedModel.GetComponent<Animator>();
                animator.enabled = true;
                animator.speed = 1.0f;
                ResetAllTriggers();
                animator.SetTrigger("Switch To Idle");
                animator.CrossFade("idle", 0.01f, 0, 0.0f);
                animator.Update(0f);

                // Place threshold circles guiding user
                EnableCircles();
                
                break;
            case guideStates.Transition:
                // No implicit action needed for transition state
                break;
            case guideStates.Talk:
                // Toggle off circles for better visiibility
                DisableCircles();

                break;
        }
    }

    private void ResetAllTriggers ()
    {
        for (int i = 1; i <= 5; i++) // Assumes default number of states used
        {
            riggedModel.GetComponent<Animator>().ResetTrigger($"Play Talk {i}");
        }
    }

    // Creates circles
    private void EnableCircles()
    {
        // Simply render existing circles if any exist
        if (decals.Count > 0)
        {
            foreach (var decal in decals)
            {
                var renderer = decal.GetComponent<Renderer>();
                renderer.enabled = true;
            }

            return;
        }

        // Create circles only on first toggle of circles
        foreach (var alignment in alignments)
        {
            // Move each circle to each talk
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Vector3 quadLocation = new Vector3(alignment.riggedModelTransforms.location.x, 0.1f, alignment.riggedModelTransforms.location.z);
            quad.transform.position = quadLocation;
            quad.transform.rotation = Quaternion.Euler(90, 0, 0); // Face upwards
            quad.transform.localScale = new Vector3(proximityToSwitch * 2f, proximityToSwitch * 2f, 1.0f);
            
            // Disable hitbox
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

    // Toggles off each circle
    private void DisableCircles()
    {
        foreach (var decal in decals)
        {
            var renderer = decal.GetComponent<Renderer>();
            renderer.enabled = false;
        }
    }

    // Used when switching from alignment to talk phase; initialises player character
    public void SwitchToPlayerMode(bool enable)
    {
        player.SetActive(enable);  // Enables/disables player movement
        playerCam.gameObject.SetActive(enable);  // Enables/disables the camera

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

    // Pass reference to completed list of alignments
    public void SetAlignments(List<Alignments> alignments)
    {
        this.alignments = alignments;
    }

    // Get list of alignments
    public List<Alignments> GetAlignments()
    {
        return alignments;
    }

    // Executes SwitchMode() method in relevant action object; needs index of depthkit video
    public void SwitchMode(int index)
    {
        if (currentState == guideStates.Talk)
        {
            exhibitTalks[index].SwitchMode();
        }
    }

    // Reference to rigged model for no inspector assigned
    public GameObject getRiggedModel()
    {
        return riggedModel; 
    }

    // Get current state in global state machine
    public guideStates GetState()
    {
        return currentState;
    }

    // Can be used instead of playing DepthKit video audio through assignment in the DepthKit Clip
    public void playAudio(AudioClip voiceover)
    {
        Audio.clip = voiceover;
        Audio.loop = false;
        StartCoroutine(loopAudio());
    }

    // Made for audio not synced with DepthKit object
    IEnumerator loopAudio()
    {
        while (!finishPlaying)
        {
            Audio.Play();

            yield return new WaitForSeconds(Audio.clip.length);
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
