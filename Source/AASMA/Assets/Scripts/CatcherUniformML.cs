using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CatcherUniformML : Unity.MLAgents.Agent
{
    private Catcher c;  // References this catcher's catcher script

    // Start is called before the first frame update
    void Start()
    {
        c = GetComponent<Catcher>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor){
        // We send the position of each catcher, the position of each fleer, and the status of each fleer 
        List<Vector3> catcherList = c.observations.GetCatcherPositions();
        foreach(Vector3 v in catcherList){
            sensor.AddObservation(v);
        }
        List<Vector3> fleerList = c.observations.GetFleerPositions();
        foreach(Vector3 v in fleerList){
            sensor.AddObservation(v);
        }
        List<Fleer> fleerStatusList = c.observations.GetFleerList();
        foreach(Fleer f in fleerStatusList){
            sensor.AddObservation(f.isCaught);
        }
        sensor.AddObservation(c.observations.GetEncirclement());
    }   


    public override void OnActionReceived(Unity.MLAgents.Actuators.ActionBuffers actions){
        c.alt_direction = new Vector3(actions.ContinuousActions[0], actions.ContinuousActions[1], 0);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Fleer") && !collision.gameObject.GetComponent<Fleer>().isCaught)
            c.gm.addAllAgentsReward(0.01f);

        if (collision.transform.CompareTag("blackwall"))
            c.gm.addAllAgentsReward(-0.01f);
    }
}
