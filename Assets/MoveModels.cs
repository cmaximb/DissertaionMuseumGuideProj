using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Video;
using System;

// Object for containing transform
[System.Serializable]
public class TransformModel
{
    public Vector3 location;
    public Quaternion rotation;
}

// Object containing transforms for both the models and the marionette capture name
[System.Serializable]
public class Alignments
{
    public TransformModel volumetricModelTransforms;
    public TransformModel riggedModelTransforms;
    public String name;
}

// alignment phase
public class MoveModels : MonoBehaviour
{
    private List<GameObject> volumetricModels;
    private GameObject riggedModel; 
    private List<Alignments> alignmentsList;

    private bool AssignAlignmentsEveryTime;
    private List<string> movementNames;
    public string saveFileName = "alignments.json";
    private string path => Path.Combine(Application.persistentDataPath, saveFileName);

    private bool showRiggedModel = true;
    private int currentModelNumber = 0;

    private bool initialised = false;
    private bool finished = false;

    public Camera alignmentCamera;
    private static Vector3 cameraOffset = new Vector3(0.0f, 2.0f, -5.0f);

    // Get information from inspector chosen by user
    public void initialise(List<GameObject> volumetricModels, List<String> movementNames, GameObject riggedModel, List<Alignments> alignmentsList, bool AssignAlignmentsEveryTime)
    {
        initialised = true;

        this.volumetricModels = volumetricModels;
        this.riggedModel = riggedModel;
        this.alignmentsList = alignmentsList;
        this.AssignAlignmentsEveryTime = AssignAlignmentsEveryTime;
        this.movementNames = movementNames;

        alignmentCamera.gameObject.SetActive(true);

        // Pause the object playback >> Hide unseen models >> load previous alignments
        PausePlayback();
        SetVisibility();
        LoadAlignment();
    }

    void Update()
    {
        // Don't trigger until alignments loaded
        if (alignmentsList == null || finished) { return; }

        // Trigger the volumetric switcher when there is an alignment corresponding to each volumetric model
        if (alignmentsList.Count >= volumetricModels.Count)
        {
            finished = true;
            VolumetricSwitcher switcher = FindObjectOfType<VolumetricSwitcher>();

            // Stop Update from calling this a second time or initialising twice
            if (!showRiggedModel)
            {
                showRiggedModel = true;
                SetVisibility();
            }

            // In case VolumetricSwitcher does not have an alignments list
            if (switcher.GetAlignments() == null)
                switcher.SetAlignments(alignmentsList);

            // Disable camera from being seen ouut of
            alignmentCamera.gameObject.SetActive(false);

            // Rigged model should start roughly where volumetric video is for user experience
            riggedModel.transform.position = switcher.getPlayer().transform.position;
            switcher.SwitchToPlayerMode(true);

            // Stop accidental further triggers of this method
            switcher.SwitchState(guideStates.Walk);
            this.enabled = false;
        }

        // Movement inputs must be measured in an update function
        HandleMovement();

        // Switch visible model from the current phase of talk alignments
        if (Input.GetKeyDown(KeyCode.Space))
        {
            showRiggedModel = !showRiggedModel;
            SetVisibility();
        }
        
        // Move camera to get constant view of both models
        LockCamera();

        // When user is satisfied with an alignment pairing
        if (Input.GetKeyDown(KeyCode.Return) && (alignmentsList.Count < volumetricModels.Count))
        {
            // Each time alignment is confirmed, a new jsondocument is created to save the list of alignments
            SaveAlignmentData();
            AlignmentsWrapper wrapper = new AlignmentsWrapper();
            wrapper.alignments = alignmentsList;
            File.WriteAllText(path, JsonUtility.ToJson(wrapper, true));
            Debug.Log("Alignment Saved!");

            // Check if all alignments set
            currentModelNumber += 1;
            if (alignmentsList.Count > currentModelNumber)
            {
                // Rigged model has to be paused on every increment in talk alignments otherwise moves
                PauseRiggedModel(alignmentsList[currentModelNumber].name);
            }
            
            // Always start following phase as rigged model
            showRiggedModel = false;
            SetVisibility();
        }
    }

    // Update camera to match the model
    private void LockCamera()
    {
        if (alignmentCamera == null) { return; }

        if (showRiggedModel)
        {
            alignmentCamera.transform.position = riggedModel.transform.position + cameraOffset ;
        }
        else
        {
            alignmentCamera.transform.position = volumetricModels[currentModelNumber].transform.position + cameraOffset;
        }
    }

    // Check if started
    public bool getInitialised()
    {
        return initialised;
    }

    // Save alignments to a JSON file
    private void SaveAlignmentData()
    {
        Alignments data = new Alignments();

        // Assign volumetric model
        if (volumetricModels[currentModelNumber] != null)
        {
            data.volumetricModelTransforms = new TransformModel
            {
                location = volumetricModels[currentModelNumber].transform.position,
                rotation = volumetricModels[currentModelNumber].transform.rotation
            };
        }
        else throw new Exception("No volumetric model currently assigned!");

        // Assign rigged model
        if (riggedModel != null)
        {
            data.riggedModelTransforms = new TransformModel
            {
                location = riggedModel.transform.position,
                rotation = riggedModel.transform.rotation
            };
        }
        else throw new Exception("No rigged model currently assigned!");

        // Assign name
        data.name = movementNames[currentModelNumber];

        // All contained in one object, added to list of alignments
        alignmentsList.Add(data);
    }
    
    // Load any preexisting alignments for the currently allocated talks
    private void LoadAlignment()
    {
        // Don't load alignments if they don't exist or user wants to assign new ones
        if (!File.Exists(path) || AssignAlignmentsEveryTime) return;

        // Parse JSON file
        string json = File.ReadAllText(path);
        AlignmentsWrapper wrapper = JsonUtility.FromJson<AlignmentsWrapper>(json);
        List<Alignments> alignments = wrapper.alignments;

        // Transfer all alignments saved in JSON document to alignments and volumetric models
        int i = 0;
        foreach (Alignments alignment in alignments) {
            alignmentsList.Add(alignment);

            volumetricModels[i].transform.position = alignment.volumetricModelTransforms.location;
            volumetricModels[i].transform.rotation = alignment.volumetricModelTransforms.rotation;
            
            currentModelNumber = i;
            i++;
        }

        // Set initial configuration for rigged model
        riggedModel.transform.position = alignments[currentModelNumber].riggedModelTransforms.location;
        riggedModel.transform.rotation = alignments[currentModelNumber].riggedModelTransforms.rotation;
    }

    // Toggles both game objects' renderers
    private void SetVisibility()
    {
        var riggedRenderers = riggedModel.GetComponentsInChildren<Renderer>();
        foreach (var r in riggedRenderers)
            r.enabled = showRiggedModel;

        // Find the appropriate volumetric video
        for (int i = 0; i < volumetricModels.Count; i++)
        {
            var volumetricRenderers = volumetricModels[i].GetComponentsInChildren<Renderer>();
            foreach (var r in volumetricRenderers)
            {
                // Active only the current volumetric model if in volumetric mode
                if (i != currentModelNumber)
                    r.enabled = false;
                else
                    r.enabled = !showRiggedModel;
            }
        }    
    }

    // Pause both types of model
    private void PausePlayback()
    {
        foreach (var v in volumetricModels)
        {
            try
            {
                VideoPlayer player = v.GetComponent<VideoPlayer>();
                player.playOnAwake = false;
                player.Pause();
                v.GetComponent<AudioSource>().enabled = false;
            }
            catch
            {
                Debug.LogError("Clip must have video player attached");
            }
        }

        // Pause the specific rigged model currently used
        PauseRiggedModel(movementNames[0]);
    }

    // Pause specific rigged model
    private void PauseRiggedModel(string movementName)
    {
        var animator = riggedModel.GetComponent<Animator>();

        if (animator != null)
        {
            animator.CrossFade(movementName, 0f);
            animator.speed = 0f;
            animator.Update(0f);
            Debug.Log(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        else
            Debug.LogError("Model must have animator attached");
        
        // Move rigged model to volumetric model for easier use
        riggedModel.transform.position = volumetricModels[0].transform.position;
    }

    // Called on each frame update; listening for any inputs by user
    private void HandleMovement()
    {
        // Finds which model to move (rigged or volumetric, current visible model moved)
        GameObject currentModel = (showRiggedModel) ? riggedModel : volumetricModels[currentModelNumber];
        if (currentModel == null) { return; }

        // Set speeds
        float moveSpeed = 1f * Time.deltaTime;
        float rotateSpeed = 50f * Time.deltaTime;

        // Listen for movement
        if (Input.GetKey(KeyCode.W))
            currentModel.transform.position += Vector3.forward * moveSpeed;
        if (Input.GetKey(KeyCode.A))
            currentModel.transform.position += Vector3.left * moveSpeed;
        if (Input.GetKey(KeyCode.D))
            currentModel.transform.position += Vector3.right * moveSpeed;
        if (Input.GetKey(KeyCode.S))
            currentModel.transform.position += Vector3.back * moveSpeed;

        // Change the rotation
        if (Input.GetKey(KeyCode.LeftArrow))
            currentModel.transform.Rotate(Vector3.up, -rotateSpeed);
        if (Input.GetKey(KeyCode.RightArrow))
            currentModel.transform.Rotate(Vector3.up, rotateSpeed);
        if (Input.GetKey(KeyCode.UpArrow))
            currentModel.transform.Rotate(Vector3.right, -rotateSpeed);
        if (Input.GetKey(KeyCode.DownArrow))
            currentModel.transform.Rotate(Vector3.right, rotateSpeed);
    }
}

// Needed for parsing list to JSON file
[System.Serializable] // Serialisable since assigned initially in inspector not during runtime
public class AlignmentsWrapper
{
    public List<Alignments> alignments;
}
