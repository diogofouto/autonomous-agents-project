using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Observations
{
    public List<GameObject> catcherList = new List<GameObject>();
    public List<GameObject> fleerList = new List<GameObject>();
    public List<Vector3> innerWallPos = new List<Vector3>();
    public List<Vector3> outerWallPos = new List<Vector3>();

    public List<Vector3> InnerWallPos
    {
        get => innerWallPos;
        set => innerWallPos = value;
    } 

    public List<Vector3> OuterWallPos
    {
        get => outerWallPos;
        set => outerWallPos = value;
    }

    public void AddCatcher(GameObject catcher) 
    {
        catcherList.Add(catcher);
    }

    public void AddFleer(GameObject fleer)
    {
        fleerList.Add(fleer);
    }

    public List<Vector3> GetCatcherPositions()
    {
        List<Vector3> catcherPositions = new List<Vector3>();
        foreach(GameObject go in catcherList)
        {
            catcherPositions.Add(go.transform.position);
        }

        return catcherPositions;
    }

    public List<Vector3> GetFleerPositions()
    {
        List<Vector3> fleerPositions = new List<Vector3>();
        foreach(GameObject go in fleerList)
        {
            fleerPositions.Add(go.transform.position);
        }

        return fleerPositions;
    }

    public List<Vector3> GetFreeFleersPositions()
    {
        List<Vector3> freeFleersPositions = new List<Vector3>();
        foreach(GameObject go in fleerList)
        {
            if (!go.GetComponent<Fleer>().isCaught){
                freeFleersPositions.Add(go.transform.position);
            }
        }

        return freeFleersPositions;
    }

    public List<Catcher> GetCatcherList()
    {
        List<Catcher> list = new List<Catcher>();
        foreach(GameObject go in catcherList)
        {   
            list.Add(go.GetComponent<Catcher>());
        }

        return list;
    }

    public List<Fleer> GetFleerList()
    {
        List<Fleer> list = new List<Fleer>();
        foreach(GameObject go in fleerList)
        {   
            list.Add(go.GetComponent<Fleer>());
        }

        return list;
    }

    public bool GetEncirclement()
    {
        List<Catcher> c = GetCatcherList();
        return c[0].inEncirclement;
    }
}