using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Depthkit;
using UnityEngine.Video;

public class VolumetricSwitcher : MonoBehaviour
{
    //private static DepthKitObject Obj = new DepthKitObject();
    //public GameObject depthkitObject = Obj.depthkitObject;
    public double depthkitOffset = 0.0; // Obj.start_time;
    public GameObject depthkitObject;
    public GameObject riggedModel;

    public bool manualOverride = false; // toggles next three vectors

    [HideInInspector] public Vector3 manualPosition;
    [HideInInspector] public Vector3 manualRotation;
    [HideInInspector] public Vector3 manualScale = Vector3.one;

    public bool StartWithRiggedModel = false;
    private bool isUsingRiggedModel;

    private float currentTime = 0f;
    private float clipDuration;

    // Start is called before the first frame update
    void Start()
    {

        depthkitObject.GetComponent<VideoPlayer>().playOnAwake = false;
        //depthkitObject.GetComponent<VideoPlayer>().time = depthkitOffset;
        //depthkitObject.GetComponent<VideoPlayer>().Play();

        //AllignObjects();
        //MatchScale();

        isUsingRiggedModel = StartWithRiggedModel;
        SwitchMode();
    }

    void AllignObjects()
    {
        depthkitObject.transform.position = riggedModel.transform.position;
        //depthkitObject.transform.rotation = riggedModel.transform.rotation;

        Debug.Log($"Rigged Model rotation: {riggedModel.transform.rotation.eulerAngles}, Depthkit object rotation: {depthkitObject.transform.rotation.eulerAngles}");
        Debug.Log($"Rigged Model scale: {riggedModel.transform.localScale}, Depthkit Object scale: {depthkitObject.transform.localScale}");
        Debug.Log($"Rigged Model location: {riggedModel.transform.position}, Depthkit Object location: {depthkitObject.transform.position}");
    }

    // Update is called once per frame
    void Update() {}

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
            Debug.Log($"Length: {depthkitVideoPlayer.length}");
            //Debug.Log($"Time: {depthkitVideoPlayer.time}");
            float normalizedTime = (float)(depthkitVideoPlayer.time / depthkitVideoPlayer.length);

            depthkitObject.SetActive(false);
            riggedModel.SetActive(true);
            //riggedMovements.enabled = true;
            
            riggedModel.GetComponent<Animator>().Play("museum talk 1", 0, normalizedTime);
        }
        else
        {
            AnimatorStateInfo state = riggedModel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            currentTime = state.normalizedTime;

            //AllignObjects();
            depthkitObject.SetActive(true);

            VideoPlayer depthkitVideoPlayer = depthkitObject.GetComponent<VideoPlayer>();
            depthkitVideoPlayer.time = (currentTime * state.length);
            Debug.Log($"Time: {currentTime * state.length}");
            depthkitVideoPlayer.Play();
            

            riggedModel.SetActive(false);
        }

        isUsingRiggedModel = !isUsingRiggedModel;
    }

    public void SwitchMode(Quaternion degrees)
    {
        if (!isUsingRiggedModel)
        {
            depthkitObject.transform.rotation = degrees;
        }
        SwitchMode();
    }
}

public class DepthKitObject
{
    public GameObject depthkitObject;
    public double start_time = 0.0;
}
