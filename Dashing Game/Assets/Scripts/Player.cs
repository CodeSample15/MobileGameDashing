﻿using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityStandardAssets.CrossPlatformInput;
using Boo.Lang;

public class Player : MonoBehaviour
{
    #region Public Variables
    public Joystick joystick;
    public Slider health_bar;
    public Slider dash_meter;
    public TextMeshProUGUI scoreText;
    public Animator bar_animation;
    public Animator damage_animation;
    public ParticleSystem dash_particles;
    public GameObject particle_holder;
    public ParticleSystem enemy_death_particles;
    #endregion

    #region Private Variables
    private Rigidbody2D rb;

    private float movementSpeed;
    private float dashSpeed;
    private float jumpForce;
    private int minDashPower;
    private float dashRechargeRate;
    private float dashDischargeRate;
    private int enemyDamage;

    private float health;
    private float dashPower;
    private bool dashing;
    private int pointsPerKill;
    private int score;

    //movement variables
    private Vector2 movement; //for walking / dashing movement
    private Vector2 lastDashDir; //for dashing

    private Vector3 velocity = Vector3.zero;

    //lists / arrays
    private List<ParticleSystem> enemyParticles;
    #endregion

    public int Score
    {
        get { return score; }
    }

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        //adjustable variables
        movementSpeed = 6f;
        dashSpeed = 15f;
        jumpForce = 16f;
        minDashPower = 30;
        dashRechargeRate = 15f;
        dashDischargeRate = 40f;
        enemyDamage = 5;

        //fixed variables for things like health and the amount of dash ability left
        health = 100f;
        dashPower = 100f;
        dashing = false; 
        score = 0;
        pointsPerKill = 15;

        lastDashDir = new Vector2(0, 1);

        enemyParticles = new List<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        //get user controll input
        movement.x = joystick.Horizontal;
        movement.y = joystick.Vertical;

        //only dashes if the dash meter is above a certain point
        if (dashPower >= minDashPower)
        {
            dashing = (CrossPlatformInputManager.GetAxis("Dash") == 1); //checking if the dash button is pressed or not

            //resetting the dash animation
            bar_animation.SetTrigger("Stop");
        }
        else if(dashing && dashPower > 0)
        {
            if (dashPower <= 0)
            {
                dashing = false;
                dashPower = 0;
            }
            else
            {
                dashing = (CrossPlatformInputManager.GetAxis("Dash") == 1);
            }
        }
        else
        {
            bar_animation.SetTrigger("Recharge");
            dashing = false;
        }


        //refilling the dash meter
        if(!dashing && dashPower < 100)
        {
            //if the player doesn't have enough dash ability and isn't dashing, recharge
            dashPower += Time.deltaTime * dashRechargeRate;
        }
        else if(dashPower > 100)
        {
            //if the player has more dash ability than the max, reset it to the max
            dashPower = 100;
        }
        else if(dashing)
        {
            //if dashing, subtract the proper amount of dash ability
            dashPower -= dashDischargeRate * Time.deltaTime;
        }


        //getting rid of any enemy particle systems that aren't active
        for(int i=0; i<enemyParticles.Count; i++)
        {
            if(!enemyParticles[i].IsAlive())
            {
                Destroy(enemyParticles[i].gameObject);
                enemyParticles.RemoveAt(i);
            }
        }


        //update UI
        health_bar.value = health / 100;
        dash_meter.value = dashPower / 100;

        scoreText.text = score.ToString();
    }

    private void FixedUpdate()
    {
        if(!dashing)
        {
            //walking code (from Brackey's 2D character movement script)---------------------------------------------------
            rb.gravityScale = 2;

            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(movement.x * movementSpeed, rb.velocity.y);
            // And then smoothing it out and applying it to the character
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, .05f);

            lastDashDir = new Vector2(0, 1); //when the player starts to dash, they will always start by going up first

            //hiding the particles
            dash_particles.Stop();
        }
        else
        {
            //dashing code
            rb.gravityScale = 0;

            Vector3 targetVelocity;

            if (movement == Vector2.zero)
            {
                targetVelocity = lastDashDir;
            }
            else
            {
                targetVelocity = movement;
                lastDashDir = movement;
            }

            targetVelocity = targetVelocity.normalized;
            targetVelocity *= dashSpeed;
            rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref velocity, .02f);

            //putting in the particles
            if (!dash_particles.isPlaying)
                dash_particles.Play();

            particle_holder.transform.rotation = Quaternion.FromToRotation(Vector3.right, (Vector2)targetVelocity.normalized);
            particle_holder.transform.Rotate(0, 0, -90);
        }


        //Checking to see if the player is touching an enemy
        Collider2D col = 
    }

    //public methods
    public void jump()
    {
        if (!dashing) {
            if (Physics2D.Raycast(transform.position, transform.TransformDirection(Vector2.down), 0.31f))
            {
                rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            }
        }
    }

    //private methods
    private void OnCollisionEnter2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("Enemy"))
        {
            if (dashing)
            {
                Destroy(other.gameObject);
                score += pointsPerKill;
                dashPower-=2;

                //particle effect
                enemyParticles.Add(Instantiate(enemy_death_particles, other.gameObject.transform.position, Quaternion.identity));
                enemyParticles[enemyParticles.Count - 1].Play();
            }
            else
            {
                health -= enemyDamage;
                damage_animation.SetTrigger("Damage");
            }
        }
    }
}