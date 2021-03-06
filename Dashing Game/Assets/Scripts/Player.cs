﻿using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using System.Collections;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    #region Public Variables
    public Animator           character_animations;
    public EnemyController    enemy_controller;
    public Joystick           joystick;
    public Slider             health_bar;
    public Slider             dash_meter;
    public TextMeshProUGUI    scoreText;
    public Animator           bar_animation;
    public Animator           damage_animation;
    public ParticleSystem     dash_particles;
    public GameObject         particle_holder;
    public ParticleSystem     enemy_death_particles;
    public ParticleController particleController;
    #endregion

    #region Private Variables
    private Rigidbody2D rb;
    private Collider2D col;

    private float movementSpeed; // how fast the player travels at maximum speed
    private float walkingSpeed; // where the animation will switch from walking to running
    private float dashSpeed; // how fast the player travels when dashing
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
    #endregion

    #region public versions of private variables
    public float DashPower
    {
        get { return dashPower; }
        set { dashPower = value; }
    }

    public int Score
    {
        get { return score; }
    }

    public float Health
    {
        get { return health; }
        set { health = value; }
    }

    public bool isDashing
    {
        get { return dashing; }
    }

    public bool isAlive
    {
        get { return health > 0; }
    }
    #endregion

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        //adjustable variables
        movementSpeed = 6f;
        walkingSpeed = 0.3f;
        dashSpeed = 15f;
        jumpForce = 16f;
        minDashPower = 20;
        dashRechargeRate = 3.2f;
        dashDischargeRate = 35f;
        enemyDamage = 5;

        //fixed variables for things like health and the amount of dash ability left
        health = 100f;
        dashPower = 100f;
        dashing = false; 
        score = 0;
        pointsPerKill = 15;

        lastDashDir = new Vector2(0, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
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
            else if (dashing && dashPower > 0)
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


            //refilling the dash meter--------------------------------------
            if (!dashing && dashPower < 100)
            {
                //if the player doesn't have enough dash ability and isn't dashing, recharge
                dashPower += Time.deltaTime * dashRechargeRate;
            }
            else if (dashPower > 100)
            {
                //if the player has more dash ability than the max, reset it to the max
                dashPower = 100;
            }
            else if (dashing)
            {
                //if dashing, subtract the proper amount of dash ability
                dashPower -= dashDischargeRate * Time.deltaTime;
            }

            //handle animations--------------------------------------------------------------------- (Animations)
            //set the speed of the running animation
            character_animations.SetFloat("RunSpeed", movement.x);

            if (Mathf.Abs(movement.x) > 0)
                character_animations.SetBool("Running", !character_animations.GetBool("Falling"));
            else
                character_animations.SetBool("Running", false);

            //detecting if the player is traveling slow enough to be walking
            if(Mathf.Abs(movement.x) <= walkingSpeed && movement.x != 0)
            {
                character_animations.SetBool("Walking", true);
                character_animations.SetBool("Running", false);
            }
            else
            {
                character_animations.SetBool("Walking", false);
            }

            if(movement.x < 0)
            {
                //character is running to the left
                transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
            }
            else if(movement.x > 0)
            {
                //character is running to the right
                transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
            }
            
            character_animations.SetBool("Falling", rb.velocity.y < -0.1 && !dashing);
            //-------------------------------------------------------------------------------------- (Animations)

            //detect collisions with enemies
            DetectEnemies();
        }
        else
        {
            //If the player is dead:
            transform.position = Vector2.zero;
            enemy_controller.Spawning = false;
        }

        //update UI-----------------------------------
        health_bar.value = health / 100;
        dash_meter.value = dashPower / 100;

        scoreText.text = score.ToString();
        //--------------------------------------------
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
    }

    //public methods
    public void jump()
    {
        if (!dashing) {
            if (Physics2D.Raycast(transform.position, transform.TransformDirection(Vector2.down), 1f))
            {
                character_animations.SetTrigger("Jump");
                rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
            }
        }
    }

    /// <summary>
    /// Detecting if the player is touching any enemies and destroying them if the player is dashing
    /// 
    /// The script will loop through each active enemy gameobject in the EnemyController's active enemies array to search for collisions between the two gameobjects
    /// </summary>
    private void DetectEnemies()
    {
        if (dashing)
        {
            Collider2D other;

            //looping through each enemy and checking if it is colliding with the player
            for (int i = 0; i < enemy_controller.ActiveEnemies.Count; i++)
            {
                if (enemy_controller.ActiveEnemies[i].gameObject != null)
                {
                    other = enemy_controller.ActiveEnemies[i].GetComponent<Collider2D>();

                    if (other.IsTouching(col))
                    {

                        //particle effect
                        int enemyType = other.gameObject.GetComponent<Enemy>().Type;
                        Color deathParticleColor = other.gameObject.GetComponent<Enemy>().getColor(enemyType);

                        //making enemy particles and setting the color of the enemy death particles
                        particleController.AddParticles(Instantiate(enemy_death_particles, other.gameObject.transform.position, Quaternion.identity));
                        ParticleSystem.MainModule settings = particleController.Particles[particleController.Particles.Count - 1].main;
                        settings.startColor = deathParticleColor;

                        //updating stats
                        score += pointsPerKill;

                        enemy_controller.clearEnemy(i); //clear out the dead enemy from the list
                        i--;
                    }
                }
            }
        }
    }
}