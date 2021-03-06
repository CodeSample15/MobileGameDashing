﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ZoomFillBoxRefillCounter : MonoBehaviour
{
    [SerializeField] public ZoomFillBox fillbox;

    private TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if(fillbox.TimeSinceLastRefill == fillbox.refillTime)
        {
            text.SetText("");
        }
        else
        {
            int timeLeft = (int)fillbox.refillTime - fillbox.TimeSinceLastRefill;
            text.SetText(timeLeft.ToString());
        }
    }
}
