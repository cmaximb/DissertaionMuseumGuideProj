using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Video;
using System;

[System.Serializable]
public class TransformModel
{
    public Vector3 location;
    public Quaternion rotation;
}

[System.Serializable]
public class Alignments
{
    public TransformModel volumetricModelTransforms;
    public TransformModel riggedModelTransforms;
}

public class MoveModels : MonoBehaviour
{
    public List<GameObject> volumetricModels = new List<GameObject>();
    public GameObject riggedModel; // TODO: Access models in volumetric switcher
    private List<Alignments> alignmentsList = new List<Alignments>();

    public bool AssignAlignmentsEveryTime = false;

    public string saveFileName = "alignments.json";
    private string path => Path.Combine(Application.persistentDataPath, saveFileName);

    private bool showRiggedModel = true;
    private int currentModelNumber = 0;

    void Start()
    {
        PausePlayback();
        SetVisibility();
        LoadAlignment();
    }

    void Update()
    {
        // Trigger the volumetric switcher when there is an alignment corresponding to each volumetric model
        if (alignmentsList.Count >= volumetricModels.Count)
        {
            VolumetricSwitcher switcher = FindObjectOfType<VolumetricSwitcher>(); // TODO: Only work with one volumetric switcher
            // Stop Update from calling this a second time or initialising twice
            StartCoroutine(DisableSelfNextFrame());
            if (!showRiggedModel)
            {
                showRiggedModel = true;
                SetVisibility();
            }
            switcher.Initialise(alignmentsList);
        }

        HandleMovement();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            showRiggedModel = !showRiggedModel;
            SetVisibility();
        }

        if (Input.GetKeyDown(KeyCode.Return) && (alignmentsList.Count < volumetricModels.Count))
        {
            // Each time alignment is confirmed, a new jsondocument is created to save the list of alignments
            SaveAlignmentData();
            AlignmentsWrapper wrapper = new AlignmentsWrapper();
            wrapper.alignments = alignmentsList;
            File.WriteAllText(path, JsonUtility.ToJson(wrapper, true));
            Debug.Log("Alignment Saved!");
        }
        
    }

    private IEnumerator DisableSelfNextFrame()
    {
        yield return new WaitForEndOfFrame(); // or yield return null; for just 1 frame delay
        this.gameObject.SetActive(false);
    }

    private void SaveAlignmentData()
    {
        Alignments data = new Alignments();

        if (volumetricModels[currentModelNumber] != null)
        {
            data.volumetricModelTransforms = new TransformModel
            {
                location = volumetricModels[currentModelNumber].transform.position,
                rotation = volumetricModels[currentModelNumber].transform.rotation
            };
        }
        else throw new Exception("No volumetric model currently assigned!");

        if (riggedModel != null)
        {
            data.riggedModelTransforms = new TransformModel
            {
                location = riggedModel.transform.position,
                rotation = riggedModel.transform.rotation
            };
        }
        else throw new Exception("No rigged model currently assigned!");

        alignmentsList.Add(data);
    }
    
    private void LoadAlignment()
    {
        if (!File.Exists(path) || AssignAlignmentsEveryTime) return;

        string json = File.ReadAllText(path);
        AlignmentsWrapper wrapper = JsonUtility.FromJson<AlignmentsWrapper>(json);
        List<Alignments> alignments = wrapper.alignments;

        int i = 0;
        foreach (Alignments alignment in alignments) {
            alignmentsList.Add(alignment);

            volumetricModels[i].transform.position = alignment.volumetricModelTransforms.location;
            volumetricModels[i].transform.rotation = alignment.volumetricModelTransforms.rotation;
            
            currentModelNumber = i;
            i++;
        }

        Debug.Log(alignments.Count);
        riggedModel.transform.position = alignments[currentModelNumber].riggedModelTransforms.location;
        riggedModel.transform.rotation = alignments[currentModelNumber].riggedModelTransforms.rotation;
    }

    private void SetVisibility()
    {
        var riggedRenderers = riggedModel.GetComponentsInChildren<Renderer>();
        foreach (var r in riggedRenderers)
            r.enabled = showRiggedModel;

        //riggedModel.SetActive(showRiggedModel);?

        for (int i = 0; i < volumetricModels.Count; i++)
        {
            var volumetricRenderers = volumetricModels[i].GetComponentsInChildren<Renderer>();
            foreach (var r in volumetricRenderers)
            {
                // Active only the current volumetric model if in volumetric mode
                if (i != currentModelNumber)
                r.enabled = false;
                //volumetricModels[i].SetActive(false);
                else
                    r.enabled = !showRiggedModel;
                //volumetricModels[i].SetActive(!showRiggedModel);
            }
        }    
    }

    private void PausePlayback()
    {
        foreach (var v in volumetricModels)
        {
            try
            {
                VideoPlayer player = v.GetComponent<VideoPlayer>();
                player.playOnAwake = false;
                //player.time = FindObjectOfType<VolumetricSwitcher>().getDepthkitOffset(); 
                player.Pause();
            }
            catch
            {
                Debug.LogError("Clip must have video player attached");
            }
        }

        var animator = riggedModel.GetComponent<Animator>();
        if (animator != null)
        {
            //animator.enabled = false;
            animator.speed = 0f;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            //float startingTime = (float)FindObjectOfType<VolumetricSwitcher>().getRiggedOffset();
            animator.Play("museum talk 1", 0, 0); // startingTime / state.length);
            animator.Update(0f);
            Debug.Log(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }
        else
            Debug.LogError("Model must have animator attached");
    }

    private void HandleMovement()
    {
        GameObject currentModel = (showRiggedModel) ? riggedModel : volumetricModels[currentModelNumber];
        if (currentModel == null) { return; }

        float moveSpeed = 1f * Time.deltaTime;
        float rotateSpeed = 50f * Time.deltaTime;


        if (Input.GetKey(KeyCode.W))
            currentModel.transform.position += Vector3.forward * moveSpeed;
        if (Input.GetKey(KeyCode.A))
            currentModel.transform.position += Vector3.left * moveSpeed;
        if (Input.GetKey(KeyCode.D))
            currentModel.transform.position += Vector3.right * moveSpeed;
        if (Input.GetKey(KeyCode.S))
            currentModel.transform.position += Vector3.down * moveSpeed;

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

[System.Serializable]
public class AlignmentsWrapper
{
    public List<Alignments> alignments;
}
