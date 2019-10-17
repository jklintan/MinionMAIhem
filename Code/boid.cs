using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/* Created by Josefine Klintberg, 2019
 * 
 * Class for a boid-like object
 * This scripts controls the behaviour of each individual
 * boid-like object by applying the rules of cohesion, separation & alignment
 */

public class boid : MonoBehaviour
{
    public bool gameScene = false;
    private bool fight = false;
    private bool goalReached = false;
    public Animator theAnimation;

    //Core class members for each boid
    public Vector3 position;
    private Vector3 velocity;
    public string team;
    public float speedFactor = 4;
    public Vector3 goal; //Goal position to move towards

    // Sphere collider to search for neighbouring boids
    private SphereCollider surroundings;
    private MeshRenderer showing;

    // Screen values
    private int width = 30;
    private int height = 20;
    private int depth = 20;

    //User input
    public GameObject thisBoid; //Prefab
    public float radius = 3;

    //Storing of the neighbouring boids
    public List<GameObject> neighbourBoids;

    //Set the maximum distance from the flock the boid can move
    private float maxDistanceFlock;

    //Basic steering rules
    private float cohesionFactor;
    private float separationFactor;
    private float alignmentFactor;

    //Distance to keep the flock within
    private float wallDist = 15f; 
    private float wallScale = 1f;

    public float fearFactor = 3; //How fast to sprint when chased

    public bool lead = false; //Tells if current boid is a leader
    public float maxDistLead = 12; //Maximum distance to leader
    Vector3 maxDistLeader = new Vector3(1, 0, 1);

    //Find leader and enemy in scene and tells if active or not
    public GameObject leader = null;
    public GameObject enemy = null;
    private bool activeEnemy = false;
    private bool activeLeader = false;
    private bool assemb = false; //If the leader is assembling minions
    bool fleeing = false;

    // Setting the area for searching for boids
    void Start()
    {
        //Set properties if current scene is game 
        if (gameScene)
        {
            wallDist = 30f;
            cohesionFactor = 3;
            separationFactor = 2;
            alignmentFactor = 1;
        }
        else //Playground area
        {
            cohesionFactor = 3;
            separationFactor = 1;
            alignmentFactor = 1;
        }

        surroundings = GetComponent<SphereCollider>();
        goal = new Vector3(0, 0, 0); //Move towards middle initially

        //Set the neighbouring area
        if (radius <= 0)
            surroundings.radius = 1;
        else
            surroundings.radius = radius;

        //Set initial velocity
        velocity = new Vector3(1, 1, 1);

        //Set initial random position
        Vector3 spawn = boidFlock.spawnPos;
        position = new Vector3(UnityEngine.Random.Range(spawn.x - 3, spawn.x + 3), 0, spawn.z);
        thisBoid.transform.position = Vector3.zero;

        //Display direction
        //Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
    }

    void Update()
    {
        if (!goalReached)
        {
            CollisionCheck(); //Check for collision

            //If active leader
            float behindLeader = 0;
            float distEnemy = 0;
            if (leader != null)
            {
                behindLeader = Vector3.Distance(this.transform.position, leader.transform.position);
            }

            //If active enemy
            if (enemy != null && activeEnemy)
            {
                Vector3 en;
                en = enemy.gameObject.transform.position;
                distEnemy = Vector3.Distance(this.transform.position, en);
                distEnemy = Math.Abs(distEnemy);
            }
            if (distEnemy > 10 && fleeing)
            {
                fleeing = false;
                speedFactor -= 5;
            }
            if (activeEnemy && enemy != null && distEnemy < 6 && !fleeing)
            {
                //Flee
                if (fleeing)
                    goal += fearFactor * (this.transform.position - enemy.transform.position) / (distEnemy);
                else
                    goal = fearFactor * (this.transform.position - enemy.transform.position) / (distEnemy);

                goal.y = 0;
                speedFactor += 5;
                moveForward();
                fleeing = true;
            }
            else if (fleeing)
            {
                goal += fearFactor * (this.transform.position - enemy.transform.position) / (distEnemy);
                goal.y = 0;
                moveForward();
            }
            else if (activeLeader && leader != null && !lead && behindLeader > maxDistLead)
            {
                goal = leader.transform.position;
                updateNeighbours();
                separation();
                cohesion();
                alignment();
                moveForward();
            }
            else if (fight)
            {
                Debug.Log("fight");
                GameObject[] enemies = GameObject.FindGameObjectsWithTag("boid");
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (enemies[i].GetComponent<boid>().team != team)
                    {
                        Attack(enemies[i]);
                    }
                }
            }
            else if (assemb)
            {
                goal = leader.transform.position;
                moveForward();

                if (Vector3.Distance(transform.position, goal) < 0.5f)
                {
                    assemb = false;
                }
            }
            else
            {
                updateNeighbours();
                separation();
                cohesion();
                alignment();
                moveForward();

            }

            if (!withinBounds())
            {
                avoidWalls();
            }

        }

    }

    #region Obstacle Avoidance

    //Obstacle avoidance and goal reaching (for game)
    void CollisionCheck()
    {
        Collider[] thingsInBounds = Physics.OverlapSphere(transform.position, 0.2f);
        foreach (Collider c in thingsInBounds)
        {
            if (c.gameObject.tag == "stop")
            {
                if (c != this)
                {
                    avoidWalls();
                    //goal = -goal;
                }
            }

            if (c.gameObject.tag == "goal" && gameScene)
            {
                if (c != this)
                {
                    theAnimation.Play("Idle");
                    goalReached = true;
                    gameLogic.minionsAssembled++;
                    this.gameObject.GetComponent<BoxCollider>().enabled = false;
                    this.gameObject.GetComponent<Rigidbody>().freezeRotation = true;
                    this.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                }
            }
        }
    }
    //Obstacle avoidance
    void avoidWalls()
    {
        if (gameScene)
            goal = (this.transform.position) / radius;
        else
            goal = -goal;     //When avoiding walls, simply change directions back within the box
    }

    //Keep the boids within a certain area
    bool withinBounds()
    {
        Vector3 dist = getDistance();
        if (Math.Abs(dist.x) > wallDist || Math.Abs(dist.z) > wallDist)
        {
            return false;
        }
        return true;
    }

    #endregion

    #region basic steering rules

    /* Function for getting the boids within the specific radius
     * around the current boid. Adding them to the list and 
     * handling the removal of neighbouring boids that no longer
     * are neighbours. 
     */
    void updateNeighbours()
    {
        neighbourBoids.Clear();
        Collider[] thingsInBounds = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider c in thingsInBounds)
        {//&& c.gameObject.GetComponent<boid>().team == this.team
            if (c.gameObject != this.gameObject && c.gameObject.tag == "boid" && c.gameObject.GetComponent<boid>().team == this.team)
            {
                GameObject temp = c.gameObject;
                if (!neighbourBoids.Contains(temp))
                    neighbourBoids.Add(temp);
            }
        }

        //Remove old neighbouringboids that no longer are within radius
        for (int i = 0; i < neighbourBoids.Count; i++)
        {
            if (neighbourBoids[i] == null)
                neighbourBoids.Remove(neighbourBoids[i]);
        }

        if (neighbourBoids.Count == 0)
        {
            goal += new Vector3(UnityEngine.Random.Range(-1, 1), 0, UnityEngine.Random.Range(-1, 1));
        }
    }



    /* Calculating of the cohesion factor
     * The cohesion factor strives to calculate
     * the average heading of all the boids within
     * the specific radius (the neighbouring boids)
     * */
    private void cohesion()
    {
        Vector3 averagePos = new Vector3(0, 0, 0);
        if (neighbourBoids.Count != 0)
        {
            foreach (GameObject b in neighbourBoids)
            {
                if (b != null)
                    averagePos += b.GetComponent<boid>().position;
            }
            
            //Goal is the average group position
            averagePos /= neighbourBoids.Count;

            //Updating the goal position for this boid
            goal += (averagePos - position) * cohesionFactor;
            //velocity += (averagePos - position) * cohesionFactor;
            //velocity += (averagePos - position) * cohesionFactor;
        }
    }

    /* Calculating of the separation factor
     * The separation factor strives to keep
     * the boids within the flock at a certain
     * distance from each other to avoid collisions. 
     * */
    private void separation()
    {
        Vector3 separationVector = new Vector3(0, 0, 0);
        if (neighbourBoids.Count != 0)
        {
            foreach (GameObject b in neighbourBoids)
            {
                if (b != null)
                    separationVector += (position - b.GetComponent<boid>().position);
            }
            separationVector /= neighbourBoids.Count;

            //Update the goal position for the current boid
            goal += separationVector * separationFactor;
            //velocity += separationVector * separationFactor;
        }
    }

    //Calculating the alignment factor
    private void alignment()
    {
        Vector3 v = new Vector3(0, 0, 0);
        if (neighbourBoids.Count != 0)
        {
            foreach (GameObject b in neighbourBoids)
            {
                if (b != null)
                {
                    v += new Vector3(b.GetComponent<boid>().velocity.x - velocity.x, b.GetComponent<boid>().velocity.y - velocity.y, b.GetComponent<boid>().velocity.z - velocity.z);
                }
            }
            v /= neighbourBoids.Count;
            goal += (v - velocity) * alignmentFactor;
        }
    }

    // Move the current boid forward towards goal position
    public void moveForward()
    {
        float dt = Time.deltaTime * speedFactor;
        var direction = velocity.normalized;
        var speed = velocity.magnitude;
        velocity = Mathf.Clamp(speed, 0, 1) * direction;
        position = Vector3.MoveTowards(transform.position, goal, dt);
        position.y = 0; //for 2D implementation
        goal.y = 0;
        if (goal - position != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(goal - position, Vector3.up); //Rotate towards current heading
            transform.SetPositionAndRotation(position, rot);
        }
        Vector3 newDir = Vector3.RotateTowards(transform.forward, velocity, 5 * dt, 0.0f);
        transform.position = position;
        //Debug.DrawRay(transform.position, newDir, Color.red);
    }
    #endregion

    #region updateByUserInput
    public void updateCohesion(float c)
    {
        goal = Vector3.zero;
        cohesionFactor = c;
    }

    public void updateSeparation(float s)
    {
        goal = Vector3.zero;
        separationFactor = s;
    }

    public void updateAlignment(float a)
    {
        goal = Vector3.zero;
        alignmentFactor = a;
    }

    public void setSpeed(float s)
    {
        speedFactor = s;
    }

    public void assemble(Vector3 leaderPos)
    {
        goal = leaderPos;
        assemb = true;
    }

    public void activateLeader()
    {
        activeLeader = true;
    }

    public void deactivateLeader()
    {
        activeLeader = false;
    }

    public void activateEnemy()
    {
        activeEnemy = true;
        //Debug.Log("Enemy activated");
    }

    public void fightMinions()
    {
        fight = true;
        Debug.Log("fight");
    }

    #endregion

    #region Auxiliary functions
    //Returns the distance to another gameobject
    private Vector3 distance(GameObject thisObject)
    {
        return thisObject.transform.position;
    }

    //Working on fighting and hunting ability
    private void hunt()
    {
        Camera main = Camera.main;
        Vector3 mousePos = Input.mousePosition;

        Vector3 hunt = main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, main.nearClipPlane));

        var Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var point = Ray.origin + (Ray.direction * 7.5f);

        goal += new Vector3(point.x, 0, point.y) * 80;

        moveForward();

        Debug.Log("World point " + point);
    }

    public void Attack(GameObject enemy)
    {
        goal = enemy.transform.position;
        moveForward();
        if (Math.Abs(distance(enemy).x) < 0.5f || Math.Abs(distance(enemy).z) < 0.5f)
        {
            if (enemy.GetComponent<boid>().team != team)
            {
                theAnimation.Play("Attack 01");
                Destroy(enemy);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(transform.position, 1);
    }
    public void reset()
    {
        Start();
    }


    public Vector3 getDistance()
    {
        return this.transform.position;
    }


    #endregion
}
