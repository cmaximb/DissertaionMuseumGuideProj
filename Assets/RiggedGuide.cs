using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiggedGuide : MonoBehaviour
{
    public Transform player;
    public float followSpeed = 1.0f;
    public float rotationSpeed = 5.0f;
    public float stoppingDistance = 4.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > stoppingDistance )
        {
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * followSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
