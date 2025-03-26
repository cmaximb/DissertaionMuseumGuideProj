using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RiggedGuide : MonoBehaviour
{
    public Transform player;
    public float followSpeed = 1.0f;
    public float rotationSpeed = 5.0f;
    //public float stoppingDistance = 2f;
    private NavMeshAgent agent;
    private Animator movement;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        movement = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > agent.stoppingDistance - 0.5)
        {
            //Vector3 direction = (player.position - transform.position).normalized;
            //transform.position += direction * followSpeed * Time.deltaTime;
            agent.SetDestination(player.position);
            movement.SetFloat("Speed", agent.velocity.magnitude);

            //Quaternion targetRotation = Quaternion.LookRotation(direction);
            //transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            //agent.SetDestination(transform.position);
            agent.ResetPath();
            movement.SetFloat("Speed", 0);
        }
    }
}
