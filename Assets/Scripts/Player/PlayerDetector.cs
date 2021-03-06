﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector: MonoBehaviour
{
    public GameObject enemy;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            enemy.GetComponent<Enemy>().PlayerInTrigger(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            enemy.GetComponent<Enemy>().PlayerLeaveTrigger();
        }
    }
}
