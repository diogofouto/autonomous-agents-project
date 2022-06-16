using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CatcherCentralizedML : Unity.MLAgents.Agent
{
    private List<Catcher> catchers;  // References all catchers

    // Start is called before the first frame update
    void Start()
    {
        Catcher parent_c = transform.parent.gameObject.GetComponent<Catcher>();
        catchers = parent_c.observations.GetCatcherList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void CollectObservations(VectorSensor sensor) {
        // We send the position of each catcher, the position of each fleer, and the status of each fleer 
        List<Vector3> catcherList = catchers[0].observations.GetCatcherPositions();
        foreach(Vector3 v in catcherList){
            sensor.AddObservation(v);
        }
        List<Vector3> fleerList = catchers[0].observations.GetFleerPositions();
        foreach(Vector3 v in fleerList){
            sensor.AddObservation(v);
        }
        List<Fleer> fleerStatusList = catchers[0].observations.GetFleerList();
        foreach(Fleer f in fleerStatusList){
            sensor.AddObservation(f.isCaught);
        }
        sensor.AddObservation(catchers[0].observations.GetEncirclement());
    }   


    public override void OnActionReceived(ActionBuffers actions) {
        int i = 0;
        foreach(Catcher c in catchers){
            c.alt_direction = new Vector3(actions.ContinuousActions[i], actions.ContinuousActions[i+1], 0);
            i += 2;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Fleer") && !collision.gameObject.GetComponent<Fleer>().isCaught)
            catchers[0].gm.addAllAgentsReward(0.01f);

        if (collision.transform.CompareTag("blackwall"))
            catchers[0].gm.addAllAgentsReward(-0.01f);
    }
}
