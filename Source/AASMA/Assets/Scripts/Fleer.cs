using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleer : Agent
{
    public static float totalNrCaught = 0.0f; // total number of fleers caught
    public static float totalNrRevived = 0.0f; // total number of fleers revived
    public bool isCaught = false;

    public bool enableSmart;        // If true, agent doesnt just move randomly
    public bool isMixedSmart;       // If true, agent will be randomly smart or not
    
    private Rigidbody2D rb;
    public bool isTouchingWall;

    public SpriteRenderer render;
    public Sprite free;
    public Sprite caught;


    private GameManager gm;

    public Fleer(float acceleration = 40f) : base(acceleration) { }

    void Start()
    {
        render = GetComponent<SpriteRenderer>();
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();

        rb = GetComponent<Rigidbody2D>();

        if(isMixedSmart)
            enableSmart = Random.Range(0,2) == 0;

    }

    void Update()
    {
        Debug.DrawRay(this.transform.position, this.transform.up, Color.green);
    }

    protected override Vector3 SelectMove() 
    {
        if(isCaught)
            return Vector3.zero;
        else if(enableSmart)
            return SelectSmartMove();
        else
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
    }

    private Vector3 SelectSmartMove(){

        List<Fleer> fleers = Observations.GetFleerList();
        List<Vector3> catcher_positions = Observations.GetCatcherPositions();

        // First move in the direction to save the closest caught fleer
        Vector3 save_move = Vector3.zero;

        // for each caught agent, we raycast to his position to see if there are any catchers in the way.
        // The fleer moves in the direction of the closest caught agent whose raycast doesnt touch catchers 
        foreach(Fleer f in fleers){
            
            if(f.isCaught){
                Vector3 f_pos = f.gameObject.transform.position;

                Vector3 dir = f_pos - transform.position;   // Used to make raycast not hit this fleer's body
                RaycastHit2D hit = Physics2D.Raycast(transform.position+dir.normalized*0.264f, dir);
                Debug.DrawRay(transform.position+dir.normalized*0.264f, dir, Color.green);

                // If tag == "fleer", then we didnt hit any catchers or ropes
                if(hit.collider.tag == "Fleer"){
                    if(save_move != null || Vector3.Distance(f_pos, transform.position) < save_move.magnitude){
                        save_move = f_pos - transform.position;
                    }
                }
            }
        }
        if(save_move != Vector3.zero){
            return save_move.normalized;
        }

        // If no move is possible, move in the oposite direction of every fleer
        Vector3 catchers_move = new Vector3(0,0,0); 
        
        // We sum every vector from this fleer to the catchers, and normalize it
        foreach(Vector3 c in catcher_positions){
            Vector3 diff = this.transform.position - c;
            catchers_move += diff;
        }

        return getUsefulMove(catchers_move, catcher_positions);
        //return catchers_move.normalized;
    }

    // Given a moviment vector, if the moviment isnt contributing to the velocity
    // Increases the moviment in the other axis to maximize velocity
    // If nothing is contributing, move to direction furthest away from catchers 
    // center of mass
    private Vector3 getUsefulMove(Vector3 move, List<Vector3> catcher_positions){

        // If everything is contributting
        if(rb.velocity.x != 0 && rb.velocity.y != 0){
            return move.normalized;
        }
        // If nothing is contributing 
        else if(isTouchingWall && rb.velocity.x == 0 && rb.velocity.y == 0 && move != Vector3.zero){
            Vector3 center_of_mass = Vector3.zero;
            foreach(Vector3 cp in catcher_positions){
                center_of_mass += cp;
            }
            center_of_mass = center_of_mass / catcher_positions.Count;

            Vector3 dist = transform.position - center_of_mass;
            
            // it moves in the direction where the distance is smaller, to maximize it
            // It moves to the opposite side of the center in that direction
            if(dist.x > dist.y){
                Vector3 new_move = new Vector3(0,-transform.position.y,0);
                return new_move.normalized;
            }
            else{
                Vector3 new_move = new Vector3(-transform.position.x,0,0);
                return new_move.normalized;
            }
        }
        // If stuck in a wall
        else if(isTouchingWall && rb.velocity.y == 0 && move.y != 0){
            Vector3 new_move = new Vector3(move.x,0,0);
            return new_move.normalized;
        }
        else if(isTouchingWall && rb.velocity.x == 0 && move.x != 0){
            Vector3 new_move = new Vector3(0,move.y,0);
            return new_move.normalized;
        }
        return move.normalized;
    }

    public void setCaught()
    {
        if (!isCaught) 
        {
            isCaught = true;
            render.sprite = caught;
            gm.incCaught(1);
            totalNrCaught++;
        }
    }

    public void setFree(){
        if (isCaught) 
        {
            isCaught = false;
            render.sprite = free;
            gm.incCaught(-1);
            totalNrRevived++;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("Fleer") && isCaught && !collision.gameObject.GetComponent<Fleer>().isCaught)
            setFree();
        if (collision.transform.CompareTag("wall"))
            isTouchingWall = true;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.transform.CompareTag("wall"))
            isTouchingWall = false;
    }
}
