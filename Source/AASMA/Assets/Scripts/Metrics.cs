using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Metrics : MonoBehaviour
{
    public Dictionary<string, float> metricList = new Dictionary<string,float>();   // Contains every pair <MetricLabel,Value> that will be printed 

    public TextMeshProUGUI textComponent;

    // Start is called before the first frame update
    void Awake()
    {
        // Create every metric here
        createMetric("Time");
        createMetric("Fleers caught");
        createMetric("Revive/Catch Ratio");
        createMetric("Net-Force Quotient");
        createMetric("Duration of Previous Game");
        createMetric("Total Games");
        createMetric("Catchers' Win Rate");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Update text
        updateText();
    }

    public void createMetric(string label, float initVal = 0)
    {
        if (!metricList.ContainsKey(label))
            metricList.Add(label, initVal);
        else
            Debug.Log("ERROR: Duplicate metric creation requested: "+label);
    }

    public void setMetric(string label, float val)
    {
        if (metricList.ContainsKey(label))
            metricList[label] = val;
        else 
            Debug.Log("ERROR: Unknown metric set requested: "+label);
    }

    public void incMetric(string label, float inc)
    {
        if (metricList.ContainsKey(label))
            metricList[label] += inc;
        else
            Debug.Log("ERROR: Unknown metric increment requested: "+label);
    }

    public float getMetric(string label)
    {
        if (metricList.ContainsKey(label))
            return metricList[label];
        
        Debug.Log("ERROR: Unknown metric read requested: "+label);
        return -1;
    }

    private void updateText()
    {
        textComponent.text = "";
        foreach (string key in metricList.Keys)
        {
            textComponent.text += key + ": " + metricList[key] + "\n";
        }
    }
}
