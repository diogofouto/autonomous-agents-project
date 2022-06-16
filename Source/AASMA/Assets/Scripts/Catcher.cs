using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Catcher : Agent
{
    private DistanceJoint2D sj2d;
    public CircleCollider2D triggerbox;         // The collider used as a trigger box to detect when ends meet
    public GameManager gm;            // Used to reference the Game Manager function to detect catchers

    public bool isEndpoint = false;
    public bool obstacleInPath = false;

    static public int nrCatchersInFlightPosition = 0;

    public bool inEncirclement = false;

    /*
    Alex's V formation
    */
    public float v_angle;
    private float max_distance = 1.085f;
    public Catcher pivot;
    public Fleer target;
    public int midpoint;
    public bool to_close = false;

    // In cases where we dont want to use the default formation, we can just
    // Edit alt_direction from another script
    public bool enableV;
    public Vector3 alt_direction = Vector3.zero;

    public Catcher(float acceleration = 40f) : base(acceleration) { 
    }

    void Start(){
        gm = GameObject.Find("GameManager").GetComponent<GameManager>();
        List<Catcher> catchers = Observations.GetCatcherList();
        this.midpoint = (catchers.Count / 2); // -1 for index
        this.pivot = catchers[midpoint];

        this.max_distance = 1.085f;

        // Set starting target
        if(this.index == midpoint){
            findTarget();
        }
    }

    
    public void setAsEndPoint(GameManager gm){
        triggerbox.enabled = true;
        this.gm = gm;
        isEndpoint = true;
    }

    protected override Vector3 SelectMove()
    {
        if(enableV){
            // Update status if target was caught
            if (this.target == null || (this.target != null && this.target.isCaught && index == midpoint)){
                findTarget();
            }
            else if (this.pivot != null){
                this.target = pivot.target;
            }

            Debug.DrawLine(target.transform.position, pivot.transform.position, Color.blue);
            
            // Calculate if it should close the circle
            if (index == midpoint)
                this.to_close = shouldClose();
            else
                this.to_close = pivot.to_close;
            
            // If we are not closing, move to v point, the position in the v formation
            if (!to_close && index != midpoint){
                return pickVPosition() - this.transform.position;
            }
            // if sufficient catchers in v-formation, pivot can move
            else if (!to_close && index == midpoint && nrCatchersInFlightPosition > midpoint){
                // If we are the pivot, just move towards the target until a certain distance, others follow
                nrCatchersInFlightPosition = 0;
                return target.gameObject.transform.position - transform.position;
            }
            // If we are closing, endpoints move to close circle
            else if (this.to_close){
                return goToMeetingPoint();
            }
            // don't move
            else {
                return Vector3.zero;
            }
        }
        else{
            return alt_direction;
        }
    }

    
    // Sets the current target to the closest uncaught fleer from the pivot
    public void findTarget(){
        List<Fleer> fleers = Observations.GetFleerList();
        float min_dist = 100000f;
        
        foreach (Fleer f in fleers){
            if (!f.isCaught){
                float dist = Vector3.Distance(f.gameObject.transform.position, this.transform.position);
                if (dist < min_dist){
                    this.target = f;
                    min_dist = dist;
                }
            }
        }
    }

    private Vector3 pickVPosition(){
        // The V formation point is obtained by getting the vector from the pivot to the target
        // Normalizing it, and multiplying it based on max_distance and index to get the distance~
        // Each point should be from the pivot in a straight line to the target.
        // We then rotate the point to make the V formation based on index (left vs right wing)
        Vector3 dist = target.gameObject.transform.position - pivot.gameObject.transform.position;
        dist.Normalize();
        dist = dist * max_distance * Mathf.Abs(midpoint - this.index) * 2;

        if (this.index < midpoint)
            dist = Quaternion.Euler(0,0,v_angle/2) * dist;
        else
            dist = Quaternion.Euler(0,0,-v_angle/2) * dist;

        Vector3 move = (pivot.gameObject.transform.position)/2 + dist;
        Debug.DrawLine(transform.position, move, Color.green);

        
        // if near flight position, then already in flight position
        if ((move - transform.position).magnitude < 3){
            nrCatchersInFlightPosition++;
        }
        return move;
    }

    private bool shouldClose(){
        // We close the position if the target position is inside the triangle made from the 
        // Pivot and the endpoints
        List<Vector3> catchers = Observations.GetCatcherPositions();
        
        List<Vector3> fleers = Observations.GetFleerPositions();
        Vector3 endPoint1 = catchers[0];
        Vector3 endPoint2 = catchers[catchers.Count-1];
        
        foreach(Vector3 f in fleers){
            if (PointInTriangle(f, endPoint1, endPoint2, transform.position))
                return true;
        }
        return false;
    }


    // Returns true if given 3 points, and a 4th, if the 4th is inside the triangle [WARNING: CODE OBTAINED FROM THE NET]
    private bool PointInTriangle(Vector3 pt, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        float sign (Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
        float d1, d2, d3;
        bool has_neg, has_pos;

        d1 = sign(pt, v1, v2);
        d2 = sign(pt, v2, v3);
        d3 = sign(pt, v3, v1);

        has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(has_neg && has_pos);
    }


    private bool PointInCircle(Vector3 pt, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        if (p1.x == p0.x || p2.x == p1.x)
            return false;

        float mr = (p1.y - p0.y) / (p1.x - p0.x);
        float mt = (p2.y - p1.y) / (p2.x - p1.x);

        if (mr == mt)
            return false;

        float x = (mr * mt * (p2.y - p0.y) + mr * (p1.x + p2.x) - mt * (p0.x + p1.x)) / (2f * (mr - mt));
        float y = (p0.y + p1.y) / 2f - (x - (p0.x + p1.x) / 2f) / mr;

        float radius = Mathf.Sqrt (Mathf.Pow (p1.x - x, 2f) + Mathf.Pow (p1.y - y, 2f));
        Vector2 centre = new Vector2 (x, y);

        Vector3 circle = new Vector3 (centre.x, centre.y, radius);

        if (Mathf.Pow(pt.x-centre.x,2) + Mathf.Pow(pt.y-centre.y,2) <= Mathf.Pow(radius,2))
            return true;
        else
            return false;
    }


    private Vector3 goToMeetingPoint(){
        Vector3 meeting_point;
        
        if (isEndpoint){
            List<Catcher> catchers = Observations.GetCatcherList();

            if (index < midpoint){
                meeting_point = transform.position + (catchers[catchers.Count-1].transform.position-transform.position)/2;
            }
            else {
                meeting_point = transform.position + (catchers[0].transform.position-transform.position)/2;
            }

            if (!PointInCircle(target.gameObject.transform.position, catchers[0].transform.position, catchers[catchers.Count-1].transform.position, catchers[midpoint].transform.position))
                meeting_point += (meeting_point - pivot.transform.position).normalized/3;
            Debug.DrawLine(transform.position, meeting_point, Color.white);
        }
        else if (index < midpoint) {
            List<Catcher> catchers = Observations.GetCatcherList();
            if (!obstacleInPath)
                meeting_point = catchers[catchers.Count-1].transform.position;
            else
                meeting_point = catchers[index-1].transform.position;
        }
        else if (index > midpoint) {
            List<Catcher> catchers = Observations.GetCatcherList();
            if (!obstacleInPath)
                meeting_point = catchers[0].transform.position;
            else
                meeting_point = catchers[index+1].transform.position;
        }
        else {
            meeting_point = target.gameObject.transform.position;
        }

        Vector3 move = meeting_point - transform.position;

        if (obstacleInPath && isEndpoint){
            if (this.index < midpoint)
                move = Quaternion.Euler(0,0,120) * move;
            else
                move = Quaternion.Euler(0,0,-120) * move;
        }

        Debug.DrawLine(transform.position, move + transform.position, Color.yellow);
        
        return move;
    }


    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "Catcher" && col.gameObject.GetComponent<Catcher>().isEndpoint && isEndpoint){
            gm.detectCatch();
            inEncirclement = true;
        }
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "Fleer" && to_close && index != midpoint){
            obstacleInPath = true;
        }
    }
    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.tag == "Fleer" && to_close && index != midpoint && obstacleInPath){
            StartCoroutine(WaitForObstacleSteering());
        }
        if (col.gameObject.tag == "Catcher" && col.gameObject.GetComponent<Catcher>().isEndpoint && isEndpoint)
            inEncirclement = false;
    }


    // after exiting collision, allow the catcher some time for steering around the obstacle
    private IEnumerator WaitForObstacleSteering()
    {
        yield return new WaitForSeconds (3f);
        obstacleInPath = false;
    }
}
