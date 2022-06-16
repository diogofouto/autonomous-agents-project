using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Main agent class, used for testing
public class Agent : MonoBehaviour
{
    public float acceleration;
    public int index;

    // for Net-Force Quotient
    public Vector3 previousVelocity = new Vector3(0, 0, 0);
    public Vector3 currentVelocity;
    public Vector3 netForce;
    public Vector3 externalForce;
    public Vector3 ownForce;
    
    private Rigidbody2D rb2d;

    public Observations observations;

    public Observations Observations 
    { 
        get => observations;
        set => observations = value; 
    }

    public float Acceleration 
    {
        get => acceleration;
        set => acceleration = value; 
    }

    public Agent(float acceleration = 40f) => this.acceleration = acceleration;

    protected void OnEnable() 
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    protected virtual void FixedUpdate()
    {
        // get and add force
        Vector3 move = SelectMove();
        move.Normalize();

        ownForce = move * acceleration;
        rb2d.AddForce(ownForce);

        // update current velocity and get external force
        currentVelocity = rb2d.velocity;
        netForce = (currentVelocity - previousVelocity) / acceleration;
        externalForce = netForce - ownForce;

        // update previous velocity
        previousVelocity = rb2d.velocity;
    }

    protected virtual Vector3 SelectMove() 
    {
        // Use user input
        return new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
    }
}
