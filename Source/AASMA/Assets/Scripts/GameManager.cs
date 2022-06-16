using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;

public class GameManager : MonoBehaviour
{
    public const string SETTINGS_FILE = "settings.json";
    public const string DEF_SETTINGS_FILE = "default_settings.json";

    private class GameSettings
    {
        public float arenaLength;               // Lenght of the side of the square made from the black wall
        public int teamSize;                    // Number of agents per team 
        public float innerWallDistance;         // Distance between the outer and the inner wall
        public float distanceAgentsWall;        // Vertical distance between agents and wall when they spawn
        public bool autoStartAfterGameEnds;     // If true, game starts instantly after timelimit passes
        public float timeLimit;                 // How long the game can take
        public float agentAcceleration;         // The acceleration of the agents (fleers and catchers)
        public FLEER_TYPE fleerType;
        public CATCHER_TYPE catcherType;
    }

    public GameObject outerWall;                // Prefab containing the outer most wall 
    public GameObject innerWall;                // Prefab containing the inner most wall

    public float arenaLength;                   // Lenght of the side of the square made from the black wall
    public float innerWallDistance;             // Distance between the outer and the inner wall

    public GameObject fleer;                    // Prefab for the fleer agent
    private GameObject catcher;                  // Prefab for the catcher agent
    public Transform agentParentTransform;      // Main parent of every agent and chain, for easy resets

    public GameObject rope;

    public int teamSize;                            // Number of agents per team 
    private float distanceAgentsAgents = 1.085f;    // Lateral distance between agents when they spawn NOTE: Only 1.085 produces a decent result
    public float distanceAgentsWall;                // Vertical distance between agents and wall when they spawn
    public float agentAcceleration = 40f;           // The speed of the agents

    public PolygonCollider2D pc;                // The collider used to check what was caught via encirclement
    private bool isCheckingCaught = false;      // True if we are currently analysing who has been caught

    public List<GameObject> ropeList;

    private int fleersCaughtRound;                   // Number of fleers caught
    private int fleersCurrentlyCaught = 0;          
    private bool isGameDone = false;            // True if the game is done
    private bool waitingForInput = false;       // True if game is waiting for input to restart
    private int nrCatcherWins = 0;

    private Metrics metrics;
    public bool autoStartAfterGameEnds;         // If true, game starts instantly after timelimit passes
    public float timeLimit;                     // How long the game can take
    private Coroutine timerRoutine;
    public Observations observations = new Observations();

    public int maxRounds = 100;
    private int roundsPlayed = 0;
    private float avgTimeTaken = 0;
    private float avgFleersCaught = 0;
    private float avgNetForce = 0;
    private float avgNetForceRound = 0;
    private float rcRatio = 0;

    public List<GameObject> CatcherPrefabs;

    public enum FLEER_TYPE 
    {
        RANDOM = 0,
        GREEDY = 1,
        MIXED = 2
    }

    public FLEER_TYPE FleerType;

    public enum CATCHER_TYPE
    {
        V_FORMATION = 0,
        ROLES = 1,
        DECENTRALISED_ML = 2,
        CENTRALISED_ML = 3,
        CURRICULUM_ML = 4
    }

    public CATCHER_TYPE CatcherType;

    private bool UsingVFormationCatchers()
    {
        return CatcherType == CATCHER_TYPE.V_FORMATION;
    }

    private bool UsingRolesCatchers()
    {
        return CatcherType == CATCHER_TYPE.ROLES;
    }

    private bool UsingCentralisedMLCatchers()
    {
        return CatcherType == CATCHER_TYPE.CENTRALISED_ML;
    }

    private bool UsingDecentralisedMLCatchers()
    {
        return CatcherType == CATCHER_TYPE.DECENTRALISED_ML;
    }

    private bool UsingCurriculumMLCatchers()
    {
        return CatcherType == CATCHER_TYPE.CURRICULUM_ML;
    }

    private bool UsingRandomFleers()
    {
        return FleerType == FLEER_TYPE.RANDOM;
    }

    private bool UsingGreedyFleers()
    {
        return FleerType == FLEER_TYPE.GREEDY;
    }

    private bool UsingMixedFleers()
    {
        return FleerType == FLEER_TYPE.MIXED;
    }

    private void AssignRoles()
    {
        foreach (Catcher c in observations.GetCatcherList())
        {
            RoleCatcher rc = (c as RoleCatcher);
            rc.AssignRole();
        }
    }

    private void loadSettings()
    {
        string json = File.ReadAllText(Application.dataPath + "/" + SETTINGS_FILE);
        GameSettings loadedSettings = JsonUtility.FromJson<GameSettings>(json);

        arenaLength = loadedSettings.arenaLength;
        teamSize = loadedSettings.teamSize;
        innerWallDistance = loadedSettings.innerWallDistance;
        distanceAgentsWall = loadedSettings.distanceAgentsWall;
        autoStartAfterGameEnds = loadedSettings.autoStartAfterGameEnds;
        timeLimit = loadedSettings.timeLimit;
        agentAcceleration = loadedSettings.agentAcceleration;
        FleerType = loadedSettings.fleerType;
        CatcherType = loadedSettings.catcherType;
    }

    private void writeSettings() 
    {
        GameSettings settings = new GameSettings();

        settings.arenaLength = arenaLength;
        settings.teamSize = teamSize;
        settings.innerWallDistance = innerWallDistance;
        settings.distanceAgentsWall = distanceAgentsWall;
        settings.autoStartAfterGameEnds = autoStartAfterGameEnds;
        settings.timeLimit = timeLimit;
        settings.agentAcceleration = agentAcceleration;
        settings.fleerType = FleerType;
        settings.catcherType = CatcherType;

        string json = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/" + SETTINGS_FILE, json);
    }

    private void resetSettings()
    {
        string json = File.ReadAllText(Application.dataPath + "/" + DEF_SETTINGS_FILE);
        File.WriteAllText(Application.dataPath + "/" + SETTINGS_FILE, json);
    }

    void Awake()
    {   
        try {
            loadSettings();
        } catch (System.IO.FileNotFoundException) {
            writeSettings();
        }

        // Spawn the walls
        instantiateWalls();

        metrics = GetComponent<Metrics>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // for curriculum training
        //teamSize = (int) Academy.Instance.EnvironmentParameters.GetWithDefault("team_size", teamSize);

        catcher = CatcherPrefabs[(int) CatcherType];
        Catcher c = catcher.GetComponent<Catcher>();
        c.Acceleration = agentAcceleration;
        c.Observations = observations;

        Fleer f = fleer.GetComponent<Fleer>();
        f.Acceleration = agentAcceleration;
        f.Observations = observations;
        
        if (UsingMixedFleers())
        {
            f.isMixedSmart = true;
        }
        else if (UsingGreedyFleers())
        {
            f.enableSmart = true;
        }

        // Set vars
        ropeList = new List<GameObject>(teamSize);
        fleersCaughtRound = 0;
        fleersCurrentlyCaught = 0;
        avgNetForceRound = 0;
        isGameDone = false;
        waitingForInput = false;
        
        // Spawn agents
        instantiateTeams();

        // Setup time limit and other metrics
        metrics.setMetric("Time", timeLimit);
        metrics.setMetric("Fleers caught", 0);
        metrics.setMetric("Revive/Catch Ratio", 0);
        metrics.setMetric("Net-Force Quotient", 0);
        timerRoutine = StartCoroutine(timeGame());
    }

    private void Update() {
        // Update revive catch ratio
        if (Fleer.totalNrCaught > 0) {
            float reviveCatchRatio = Fleer.totalNrRevived / Fleer.totalNrCaught;
            metrics.setMetric("Revive/Catch Ratio", reviveCatchRatio);
        }

        // See when game is finished
        if (!isGameDone && fleersCurrentlyCaught == teamSize) {
            nrCatcherWins++;
            isGameDone = true;

            List<Catcher> catcherList = observations.GetCatcherList();

            //Stop agents (No need to stop fleers as they are caught)
            foreach(Catcher a in catcherList) {
                a.GetComponent<Agent>().Acceleration = 0;
            }

            setAllAgentsReward(100f);
            endAllAgentsEpisode();

            if (autoStartAfterGameEnds) 
                resetGame();
            else waitingForInput = true;
        }

        if (waitingForInput && Input.GetAxis("Jump") > 0)
            resetGame();
        else if (Input.GetKeyDown(KeyCode.Escape)) // Force reset by pressing escape  
            resetGame();
        else if (Input.GetKeyDown(KeyCode.Backspace))  // Sets GameSettings to default and resets the game
        {
            resetSettings();
            resetGame();
        }
    }

    private void instantiateWalls()
    {
        float pos = arenaLength/2f;
        float scale = arenaLength/(0.15f);
        GameObject b;
        GameObject[] walls = {outerWall, innerWall};

        foreach(GameObject wall in walls)
        {
            List<Vector3> positions = new List<Vector3>();

            b = Object.Instantiate(wall, new Vector3(pos, 0f, 0f), Quaternion.identity);
            b.transform.localScale = new Vector3(0.25f, scale, 0f);
            positions.Add(b.transform.position);

            b = Object.Instantiate(wall, new Vector3(-pos, 0f, 0f), Quaternion.identity);
            b.transform.localScale = new Vector3(0.25f, scale, 0f);
            positions.Add(b.transform.position);

            b = Object.Instantiate(wall, new Vector3(0f, pos, 0f), Quaternion.identity);
            b.transform.localScale = new Vector3(scale, 0.25f, 0f);
            positions.Add(b.transform.position);

            b = Object.Instantiate(wall, new Vector3(0f,-pos, 0f), Quaternion.identity);
            b.transform.localScale = new Vector3(scale, 0.25f, 0f);
            positions.Add(b.transform.position);

            if (wall == outerWall) 
            {
                pos = arenaLength / 2 - innerWallDistance;
                scale = (arenaLength - innerWallDistance * 2) / (0.15f);
            
                observations.OuterWallPos = positions;
            } 
            else 
            {
                observations.InnerWallPos = positions;
            }
        }
    }

    private float getXAxisSpawnPosition(int i)
    {
        if (teamSize % 2 == 0)
            return distanceAgentsAgents * (0.5f + i - teamSize / 2);
        else
            return distanceAgentsAgents * (i - Mathf.Floor(teamSize / 2));
    }

    private void instantiateTeams() 
    {
        // Spawn coordinates depend if it is a pair or odd number of agents
        float y = arenaLength / 2f - innerWallDistance - distanceAgentsWall;
        y += Random.Range(-distanceAgentsWall/1.5f, distanceAgentsWall/1.5f); // Adds a little random variation
        int to_swap_sides = Random.Range(0, 2);
        int turn_90 = Random.Range(0, 2);
        if(to_swap_sides == 1) y = -y;

        for (int i = 0; i < teamSize; i++)
        {
            float x = getXAxisSpawnPosition(i);
            float new_y = y;

            if(turn_90 == 1){
                (x, new_y) = (new_y, x);
                //instantianteFleer(-x, new_y, i); // Symmetric spawn
                instantianteFleer(-x + Random.Range(-distanceAgentsWall/5f, distanceAgentsWall/5f), 
                                Random.Range(-arenaLength/2.5f + innerWallDistance + distanceAgentsWall, -(-arenaLength/2.5f + innerWallDistance + distanceAgentsWall)), i);
            }
            else{
                //instantianteFleer(x, new_y, i);    // Symmetric spawn
                instantianteFleer(Random.Range(-arenaLength/2.5f + innerWallDistance + distanceAgentsWall, -(-arenaLength/2.5f + innerWallDistance + distanceAgentsWall)),
                                    x + Random.Range(-distanceAgentsWall/5f, distanceAgentsWall/5f), i);
            }
            instantianteCatcher(x, new_y, i);
        }

        List<Catcher> catcherList = observations.GetCatcherList();

        // Set the first and last members as endpoints
        catcherList[0].setAsEndPoint(this);
        catcherList[teamSize-1].setAsEndPoint(this);

        // Link catchers
        linkCatchers(to_swap_sides == 1, turn_90 == 1, y);
    }

    private void instantianteFleer(float x, float y, int i) 
    {
        GameObject f = Object.Instantiate(fleer, new Vector3(x, y, 0), Quaternion.identity, agentParentTransform);
        observations.AddFleer(f);
        f.GetComponent<Fleer>().Observations = observations;
        f.GetComponent<Fleer>().index = i;
    }
    private void instantianteCatcher(float x, float y, int i){
        GameObject c = Object.Instantiate(catcher, new Vector3(x, -y, 0), Quaternion.identity, agentParentTransform);
        observations.AddCatcher(c);
        c.GetComponent<Catcher>().Observations = observations;
        c.GetComponent<Catcher>().index = i;
    }

    private void linkCatchers(bool swap_sides, bool turn_90, float y) 
    {
        List<Catcher> catcherList = observations.GetCatcherList();

        for(int i = 0; i < teamSize - 1; i++){
            // Get catchers
            Catcher leftCatcher = catcherList[i];
            Catcher rightCatcher = catcherList[i+1];

            // Spawn chain
            GameObject r;
            if(!turn_90){
                Vector3 midPoint = new Vector3((leftCatcher.transform.position.x + rightCatcher.transform.position.x) / 2, -y, 0);
                r = Object.Instantiate(rope, midPoint, Quaternion.Euler(0, 0, 90), agentParentTransform);
            } else{
                Vector3 midPoint = new Vector3(y, (leftCatcher.transform.position.y + rightCatcher.transform.position.y) / 2, 0);
                r = Object.Instantiate(rope, midPoint, Quaternion.Euler(0, 0, 0), agentParentTransform);
            }
            ropeList.Add(r);

            // Link chain to agents
            r.transform.GetChild(1).gameObject.GetComponents<HingeJoint2D>()[1].connectedBody = leftCatcher.GetComponent<Rigidbody2D>();
            
            r.transform.GetChild(r.transform.childCount - 2).gameObject.GetComponent<HingeJoint2D>().connectedBody = rightCatcher.GetComponent<Rigidbody2D>();
        }
    }

    public void detectCatch()
    {
        if (!isCheckingCaught) 
        {
            List<Catcher> catcherList = observations.GetCatcherList();

            //Set number of points and point coordinates for polygon collider
            int nPoints = teamSize + (teamSize - 1) * 8; // Counts catchers and rope segment points
            
            Vector2[] col_points = new Vector2[nPoints];

            // Get the points for an agent, then the rope segment
            for (int i = 0; i < teamSize - 1; i++ ) {
                col_points[i * 9] = catcherList[i].transform.position;

                // Get points for each rope segment
                GameObject r = ropeList[i];

                col_points[i * 9 +1] = r.transform.GetChild(1).transform.TransformPoint(r.transform.GetChild(1).GetComponents<HingeJoint2D>()[1].anchor);

                for(int j = 1; j < 8; j++) {
                    col_points[i * 9 + 1 + j] = r.transform.GetChild(j).transform.TransformPoint(r.transform.GetChild(j).GetComponent<HingeJoint2D>().anchor);
                }
            }

            col_points[nPoints - 1] = catcherList[teamSize-1].transform.position;

            pc.points = col_points;


            pc.enabled = true;
            StartCoroutine(disableCaughtCheck());
        }
    }

    public void incCaught(int inc) 
    {
        if (inc > 0)
        {
            fleersCaughtRound += inc;
            fleersCurrentlyCaught += inc;
            metrics.incMetric("Fleers caught", inc);
            addAllAgentsReward(0.5f);
        }
        else if(inc < 0)
            fleersCurrentlyCaught += inc;
            addAllAgentsReward(-0.5f);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "Fleer")
        {
            Fleer f = col.gameObject.GetComponent<Fleer>();
            if (!f.isCaught) 
            {
                f.setCaught();

                if (!isCheckingCaught)
                    isCheckingCaught = true;
            }
        }
    }

    private void resetGame() 
    {
        // Stop timer
        StopCoroutine(timerRoutine);

        // Destroy agents and links
        while (agentParentTransform.childCount > 0) 
        {
            DestroyImmediate(agentParentTransform.GetChild(0).gameObject);
        }

        // Save duration of game
        float gameDuration = timeLimit - metrics.getMetric("Time");
        metrics.setMetric("Duration of Previous Game", gameDuration);

        metrics.incMetric("Total Games", 1);

        // Update win rate
        float winRate = ((float) nrCatcherWins) / metrics.getMetric("Total Games");
        metrics.setMetric("Catchers' Win Rate", winRate);

        loadSettings();

        // Reset everything
        observations = new Observations();

        UpdateAverages();
        roundsPlayed++;
        if (roundsPlayed != maxRounds)
        {
            Start();
        }
        else
        {
            DumpRunStatistics();
            Application.Quit();
        }
    }

    private float ComputeIncAvg(float current, float newValue, float n)
    {
        return current + (1 / (n + 1)) * (newValue - current);
    }

    private void UpdateAverages()
    {
        avgTimeTaken = ComputeIncAvg(avgTimeTaken, timeLimit - metrics.getMetric("Time"), roundsPlayed);
        avgFleersCaught = ComputeIncAvg(avgFleersCaught, fleersCaughtRound, roundsPlayed);
        avgNetForce = ComputeIncAvg(avgNetForce, avgNetForceRound, roundsPlayed);
        rcRatio = ComputeIncAvg(rcRatio, (float) Fleer.totalNrRevived / Fleer.totalNrCaught, roundsPlayed);
    }

    private void DumpRunStatistics()
    {
        using (StreamWriter writer = new StreamWriter("run_stats.txt"))  
        {  
            writer.WriteLine("Avg. Time Taken");  
            writer.WriteLine(avgTimeTaken);
            writer.WriteLine("\nAvg. Fleers Caught");  
            writer.WriteLine(avgFleersCaught);
            writer.WriteLine("\nAvg. Net Force Quotient");
            writer.WriteLine(avgNetForce); 
            writer.WriteLine("\nAvg. R/C Ratio");
            writer.WriteLine(rcRatio);  
        }  
    }

    // Used to disable the collider right after encirclement
    IEnumerator disableCaughtCheck()
    {
        yield return new WaitForSeconds(0.1f);
        pc.enabled = false;
        isCheckingCaught = false;
    }

    // Used to time the game
    IEnumerator timeGame()
    {
        List<Catcher> catcherList = observations.GetCatcherList();
        List<Fleer> fleerList = observations.GetFleerList();


        for(float time = timeLimit; time > 0; time -= 1)
        {
            yield return new WaitForSeconds(1f);
            addAllAgentsReward(-0.01f);
            metrics.incMetric("Time",-1);

            // Update Net-Force Quotient every 1 second
            metrics.setMetric("Net-Force Quotient", updateNFQ());
            avgNetForceRound = ComputeIncAvg(avgNetForce, metrics.getMetric("Net-Force Quotient"), timeLimit - time);

            if (UsingRolesCatchers())
            {
                AssignRoles();
            }
        }
        isGameDone = true;
        
        //Stop agents
        foreach(Catcher a in catcherList)
        {
            a.GetComponent<Agent>().Acceleration = 0;
        }

        foreach(Fleer a in fleerList)
        {
            a.GetComponent<Agent>().Acceleration = 0;
        }

        setAllAgentsReward(-1f);
        endAllAgentsEpisode();

        if (autoStartAfterGameEnds) resetGame();
        else waitingForInput = true;
    }

    float updateNFQ()
    {
        List<Catcher> catcherList = observations.GetCatcherList();

        Vector3 netForce = new Vector3(0, 0, 0); // NFQ numerator
        float totalForceMagnitude = 0; // NFQ denominator
        
        // get NFQ components
        foreach(Catcher c in catcherList){
            netForce += c.GetComponent<Agent>().netForce;
            totalForceMagnitude += c.GetComponent<Agent>().ownForce.magnitude + c.GetComponent<Agent>().externalForce.magnitude;
        }

        // NFQ is multiplied by 10^5 and only keeps the first two decimal places
        float nfq = (netForce.magnitude / totalForceMagnitude) * Mathf.Pow(10, 5);
        return Mathf.Round(nfq * 100.0f) * 0.01f;
    }

    public void setAllAgentsReward(float reward){
        if (UsingCentralisedMLCatchers() || UsingDecentralisedMLCatchers())
        {                
            List<Catcher> catcherList = observations.GetCatcherList();

            if (UsingCentralisedMLCatchers())
            {
                catcherList[0].gameObject.transform.GetChild(0).GetComponent<Unity.MLAgents.Agent>().SetReward(reward);
            }
            else
            {
                foreach(Catcher c in catcherList)
                {
                    c.GetComponent<Unity.MLAgents.Agent>().SetReward(reward);
                }
            }
        }
    }
    public void addAllAgentsReward(float reward){
        if (UsingCentralisedMLCatchers() || UsingDecentralisedMLCatchers() || UsingCurriculumMLCatchers())
        {
            List<Catcher> catcherList = observations.GetCatcherList();

            if (UsingCentralisedMLCatchers())
            {
                catcherList[0].gameObject.transform.GetChild(0).GetComponent<Unity.MLAgents.Agent>().SetReward(reward);
            }
            else
            {
                foreach(Catcher c in catcherList)
                {
                    c.GetComponent<Unity.MLAgents.Agent>().AddReward(reward);
                }
            }
        }
    }
    public void endAllAgentsEpisode(){
        if (UsingCentralisedMLCatchers() || UsingDecentralisedMLCatchers() || UsingCurriculumMLCatchers())
        {
            List<Catcher> catcherList = observations.GetCatcherList();
            if (UsingCentralisedMLCatchers())
            {
                catcherList[0].gameObject.transform.GetChild(0).GetComponent<Unity.MLAgents.Agent>().EndEpisode();
            }
            else
            {
                foreach(Catcher c in catcherList)
                {
                    c.GetComponent<Unity.MLAgents.Agent>().EndEpisode();
                }
            }
        }
    }
}
