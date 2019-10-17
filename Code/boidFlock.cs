using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Created by Josefine Klintberg, 2019
 * 
 * Class for the boid flock
 * This controlscript just initialize the flock, after initialization
 * each individual boid controls their behaviour on there own and 
 * from this the total behaviour of the flock comes from
 */

public class boidFlock : MonoBehaviour
{
    //The number of boids being simulated
    public int numberOfBoids = 20;
    private int maxBoids = 200;
    public bool gameScene = false;

    public string team;

    //Game object to use a sprefab
    public GameObject boidPrefab;
    public GameObject leader;
    public GameObject enemy;

    public bool leaderActive = false;
    public bool enemyActive = false;

    //Array for storing the boids
    private GameObject[] flock;

    //Set the spawn area and position of this area
    public GameObject spawnArea;
    public static Vector3 spawnPos;

    //Set distance to the walls
    private float wallDist = 30.0f;

    // Initialize the boids
    void Start()
    {
        if (gameScene)
        {
            //Get number of boids from user input
            numberOfBoids = gameLogic.numbMinions;
        }

        spawnPos = spawnArea.transform.position;

        //Initialize the flock
        flock = new GameObject[numberOfBoids];

        //Initialize each individual boid
        flock[0] = boidPrefab;
        flock[0].transform.position = spawnPos;
        if (gameScene)
        {
            flock[0].GetComponent<boid>().activateEnemy();
        }
        for(int i = 1; i < numberOfBoids; i++)
        { 
            flock[i] = Instantiate(boidPrefab, spawnPos + new Vector3(3, 0, 3), new Quaternion()) as GameObject;
            flock[i].name = "boid" + team;
            //Debug.Log(team);
            flock[i].GetComponent<boid>().team = team;

            //If game scene, the enemy is the player and should be activated
            if (gameScene)
            {
                flock[i].GetComponent<boid>().activateEnemy(); 
            }
        }

        if(leader != null)
            leader.gameObject.SetActive(false);
    }

    #region auxiliary functions

    //Add more boids to the simulation
    public void addBoids()
    {
        if(numberOfBoids != maxBoids)
        {
            GameObject[] flockTemp = new GameObject[numberOfBoids + 1];
            flock.CopyTo(flockTemp, 0);
            flock = flockTemp;
            flock[numberOfBoids] = Instantiate(boidPrefab, spawnPos, new Quaternion()) as GameObject;
            //flock[numberOfBoids].transform.position = spawnPos;
            flock[numberOfBoids].name = "boid";
            flock[numberOfBoids].GetComponent<boid>().team = team;
            numberOfBoids++;
        }
        else
        {
            Debug.Log("Max number of boids");
        }
    }

    //Update cohesion from user input
    public void updateCohesion(float c)
    {
        foreach (GameObject boid in flock)
        {
            boid.GetComponent<boid>().updateCohesion(c);
        }
    }

    //Update separation from user input
    public void updateSeparation(float s)
    {
        foreach (GameObject boid in flock)
        {
            boid.GetComponent<boid>().updateSeparation(s);
        }
    }

    //Update alignment from user input
    public void updateAlignment(float a)
    {
        foreach (GameObject boid in flock)
        {
            boid.GetComponent<boid>().updateAlignment(a);
        }
    }

    public void updateSpeed(float s)
    {
        foreach (GameObject boid in flock)
        {
            boid.GetComponent<boid>().setSpeed(s);
        }
    }

    public void assembleMinions()
    {
        if (leader != null && leaderActive)
        {
            foreach (GameObject boid in flock)
            {
                boid.GetComponent<boid>().assemble(leader.transform.position);
            }
        }
    }

    public void activateLeader()
    {
        if (leader != null)
        {
            leader.gameObject.SetActive(true);
            leaderActive = true;
            foreach (GameObject boid in flock)
            {
                boid.GetComponent<boid>().activateLeader();
            }
        }
    }

    public void deactivateLeader()
    {
        if (leader != null)
        {
            leader.gameObject.SetActive(false);
            leaderActive = false;
            foreach (GameObject boid in flock)
            {
                boid.GetComponent<boid>().deactivateLeader();
            }
        }
    }

    public void activateEnemy()
    {
        Debug.Log(enemy);
        if(enemy != null)
        {
            if(!enemy.gameObject.activeSelf)
                enemy.gameObject.SetActive(true);
            enemyActive = true;
            //Debug.Log("activating...");
            foreach(GameObject boid in flock)
            {
                boid.GetComponent<boid>().activateEnemy();
            }
        }
    }

    public void attack()
    {
        foreach (GameObject boid in flock)
            {
                boid.GetComponent<boid>().fightMinions();
            }
    }

    public void setNumber(float f)
    {
        numberOfBoids = (int) f;
    }

    #endregion

    #region debug
    //Draw bounds for simulation
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0, 1) * 2*wallDist);
    }

    #endregion
}
