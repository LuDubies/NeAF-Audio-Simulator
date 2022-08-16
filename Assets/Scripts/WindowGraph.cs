using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowGraph : MonoBehaviour
{

    private RectTransform graphContainer;

    private void Awake()
    {
        graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
    }
}

