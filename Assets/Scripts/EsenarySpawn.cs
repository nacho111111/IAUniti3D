using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EsenarySpawn : MonoBehaviour
{
    public GameObject[] spawnAreas;
    public GameObject[] obectsToSpawn;
    private int count;
    private int countSpawns;

    private void Start()
    {
        count = obectsToSpawn.Length;
        countSpawns = spawnAreas.Length;
    }
    public void ResetRandomSpawn()
    {
        var enumerable = Enumerable.Range(0, countSpawns).OrderBy(x => Guid.NewGuid()).Take(count).ToArray();

        for (int i = 0; i < count; i++) 
        {
            obectsToSpawn[i].transform.position = spawnAreas[enumerable[i]].transform.position;
        }
    }
}
