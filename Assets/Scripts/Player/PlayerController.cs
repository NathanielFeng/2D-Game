﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{

    //Components
    private Rigidbody2D m_rb;
    private Animator m_anim;
    public Collider2D m_coll;
    public LayerMask m_ground;
    public Slider m_slider;

    //Player param
    public float m_health;  //生命值0~100之间
    public float m_speed;
    public float m_jumpForce;
    public float m_dashForce;
    public float m_dashDistance;
    public float m_floatingWindow;
    public float m_attackIdle;

    //Player private info
    private bool m_isJumping;
    private bool m_isFloating;
    private bool m_isFalling;
    private bool m_isAttacking;
    private bool m_isDashing;
    private float m_velocityRecY;
    private float m_velocityRecX;
    private float m_posRecY;
    private float m_posRecX;
    private float m_gravityRec;
    private float m_attackIdleCnt;
    private bool m_nextJump;
    private bool m_isOnGround;


    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        Jump();
        Attack();
        GroundCheck();
        Dash();
        //更新生命值
        m_slider.value = m_health;
    }

    //########################################
    //#            PLAYER FUNCTION           #
    //########################################

    //Initialized Function
    void Initialize()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_anim = GetComponent<Animator>();
        m_attackIdleCnt = 0;
        m_nextJump = true;
    }

    // Control player's movement
    void Movement()
    {
        //Initialized
        float horizontalMove = Input.GetAxis("Horizontal");
        float faceDirection = Input.GetAxisRaw("Horizontal");

        //Turn around
        if (faceDirection != 0)
        {
            float formerDirection = transform.localScale.x;
            //Set new direction
            transform.localScale = new Vector3(faceDirection, 1, 1);
            //If player face direction is changed
            if (formerDirection == transform.localScale.x)
            {
                m_anim.SetBool("Turning", false);
            }
            else
            {
                m_anim.SetBool("Turning", true);
            }
        }
        if (!m_isDashing && (!m_isAttacking || (m_isAttacking && (m_isFalling || m_isFloating || m_isJumping)))) 
        {
            m_rb.velocity = new Vector2(horizontalMove * m_speed, m_rb.velocity.y);
            m_anim.SetFloat("HorizontalSpeed", Mathf.Abs(faceDirection));
        }
    }

    //Player jump
    void Jump()
    {
        //Prevents player hold the key and jump
        if(Input.GetButtonUp("Jump"))
        {
            m_nextJump = true;
        }
        //Fall without jump
        if (!m_isJumping && !m_isFloating && /*!m_coll.IsTouchingLayers(m_ground)*/!m_isOnGround)
        {
            m_isFalling = true;
            m_anim.SetBool("Falling", true);
        }
        //Jump
        if (Input.GetButton("Jump") && /*m_coll.IsTouchingLayers(m_ground)*/m_isOnGround && !m_isAttacking 
            && !m_isFalling && !m_isFloating && m_nextJump) 
        {
            m_rb.velocity = new Vector2(m_rb.velocity.x, m_jumpForce);
            m_anim.SetBool("Jumping", true);
            m_isJumping = true;
            m_nextJump = false;
        }
        //Float
        else if (m_isJumping && ((m_rb.velocity.y < m_floatingWindow && m_rb.velocity.y > -1.0 * m_floatingWindow)
            || Input.GetButtonUp("Jump")))
        {
            if(!(m_rb.velocity.y < m_floatingWindow && m_rb.velocity.y > -1.0 * m_floatingWindow))
            {
                m_rb.velocity = new Vector2(m_rb.velocity.x, m_floatingWindow);
            }
            m_isJumping = false;
            m_isFloating = true;
            m_anim.SetBool("Jumping", false);
            m_anim.SetBool("Floating", true);
        }
        //Fall
        else if (m_isFloating && m_rb.velocity.y < -1.0 * m_floatingWindow)
        {
            m_isFloating = false;
            m_isFalling = true;
            m_anim.SetBool("Floating", false);
            m_anim.SetBool("Falling", true);
        }
        //On the ground
        else if (m_isFalling /*&& m_coll.IsTouchingLayers(m_ground) */&& m_isOnGround)
        {
            m_isFalling = false;
            m_anim.SetBool("Falling", false);
            //Reset Attack
            if (m_anim.GetBool("AttackingDown"))
            {
                m_anim.SetBool("AttackingDown", false);
                m_isAttacking = false;
            }
        }
    }


    //Player Attack
    void Attack()
    {
        if (m_attackIdleCnt <= 0)
        {
            if (Input.GetButtonDown("Fire1") && !m_anim.GetBool("Attacking"))
            {
                m_attackIdleCnt = m_attackIdle;
                if (Input.GetButton("Vertical"))
                {
                    if (Input.GetAxisRaw("Vertical") < 0)
                    {
                        if (m_isJumping || m_isFalling || m_isFloating)
                        {
                            m_anim.SetBool("AttackingDown", true);
                        }
                        else
                        {
                            //Not allow AttackingDown while on the ground
                            m_anim.SetBool("Attacking", true);
                        }
                    }
                    else
                    {
                        m_anim.SetBool("AttackingUp", true);
                    }
                }
                else
                {
                    m_anim.SetBool("Attacking", true);
                }
                m_isAttacking = true;
                //Stop moving while attack
                if (!m_isJumping && !m_isFloating && !m_isFalling)
                {
                    m_rb.velocity = new Vector2(0, m_rb.velocity.y);
                }
            }
        }
        else
        {
            m_attackIdleCnt -= Time.deltaTime;
            if (Input.GetButtonDown("Fire1"))
            {
                m_anim.SetTrigger("Attacking2");
                m_isAttacking = true;
            }
        }
    }

    void AttackFinished()
    {
        m_anim.SetBool("Attacking", false);
        m_isAttacking = false;
    }

    void AttackUpFinished()
    {
        m_anim.SetBool("AttackingUp", false);
        m_isAttacking = false;
    }

    void AttackDownFinished()
    {
        m_anim.SetBool("AttackingDown", false);
        m_isAttacking = false;
    }

    void AttackFinished2()
    {
        m_isAttacking = false;
    }

    void Dash()
    {
        if (Input.GetButtonDown("Dash"))
        {
            int directionSymbol = 0;
            float direction = transform.localScale.x;
            if(transform.localScale.x > 0)
            {
                directionSymbol = 1;
            }
            else
            {
                directionSymbol = -1;
            }
            m_anim.SetBool("Dash", true);
            if (!m_isDashing)//Prevents variables being updated twice
            {
                m_velocityRecY = m_rb.velocity.y;
                m_velocityRecX = m_rb.velocity.x;
                m_gravityRec = m_rb.gravityScale;
                m_posRecX = transform.position.x;
            }
            m_isDashing = true;
            m_rb.gravityScale = 0;//No gravity effect while dashing
            m_rb.velocity = new Vector2(directionSymbol * m_dashForce, 0);
        }
        if (m_isDashing && (Mathf.Abs(transform.position.x - m_posRecX) > m_dashDistance))
        {
            DashFinished();
        }
    }

    void DashFinished()
    {
        m_anim.SetBool("Dash", false);
        m_isDashing = false;
        m_rb.velocity = new Vector2(m_velocityRecX, m_velocityRecY);
        m_rb.gravityScale = m_gravityRec;
    }

    //Use raycast to check if player is on the ground
    void GroundCheck()
    {
        Vector2 pos = transform.position;
        BoxCollider2D boxColl = (BoxCollider2D)m_coll;
        Vector2 rightOffset = new Vector2(boxColl.size.x / 2, -boxColl.size.y / 2);
        Vector2 leftOffset = new Vector2(-boxColl.size.x / 2, -boxColl.size.y / 2);
        float distance = 0.2f;

        RaycastHit2D rightFoot = Physics2D.Raycast(pos + rightOffset + m_coll.offset, Vector2.down, distance, m_ground);
        RaycastHit2D leftFoot = Physics2D.Raycast(pos + leftOffset + m_coll.offset, Vector2.down, distance, m_ground);

        //Debug
        //Color c = Color.red;
        //if (rightFoot || leftFoot)
        //{
        //    c = Color.green;
        //}
        //Debug.DrawRay(pos + rightOffset + m_coll.offset, Vector2.down, c, distance);
        //Debug.DrawRay(pos + leftOffset + m_coll.offset, Vector2.down, c, distance);

        if (rightFoot || leftFoot)
        {
            m_isOnGround = true;
        }
        else
        {
            m_isOnGround = false;
        }
    }

    void WallCheck()
    {

    }

    public void takeDamage(float damage)
    {
        if (m_health > 0) 
            m_health -= damage;
    }


}
