using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatcherCentralizedIndividual : MonoBehaviour
{
    private Catcher c;  // References this catcher's catcher script

    CatcherCentralizedML ccml;

    // Start is called before the first frame update
    void Start()
    {
        this.c = GetComponent<Catcher>();

        // Only the first agent sets the centralized agent as active
        if (this.c.index == 0) 
        {
            this.transform.GetChild(0).gameObject.SetActive(true);
            this.ccml = this.transform.GetChild(0).gameObject.GetComponent<CatcherCentralizedML>();
        }
        else
        {
            this.transform.GetChild(0).gameObject.SetActive(false);
        }
    }
}
