﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor.Animations;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{

    //Components
    private Rigidbody2D m_rb;
    private Animator m_anim;
    public Collider2D m_coll;
    public LayerMask m_ground;
    public Slider m_slider;
    private AttackController m_atkEffect;
    private AudioSource m_audioSourse;
    public SpriteRenderer m_renderer;
    public Cinemachine.CinemachineCollisionImpulseSource impulse;
    

    //Player param
    public float m_speed;
    public float m_jumpForce;
    public float m_dashForce;
    public float m_dashDistance;
    public float m_floatingWindow;
    public float m_attackInterval;
    public int m_health;
    public float m_hurtInterval;
    public int m_healValue;
    public float m_dashTime;

    [Space]
    //Sound
    public AudioClip m_soundFootstepsGrass;
    public AudioClip m_soundFootstepsStone;
    public AudioClip m_soundAtk1;
    public AudioClip m_soundAtk2;

    //Player private info
    private bool m_isJumping;
    private bool m_isFloating;
    private bool m_isFalling;
    private bool m_isAttacking;
    private bool m_isDashing;
    private bool m_isHealing;
    private float m_velocityRecY;
    private float m_velocityRecX;
    private float m_posRecY;
    private float m_posRecX;
    private float m_gravityRec;
    private float m_attackTimeCnt;
    private float m_dashTimeCnt;
    private bool m_nextJump;
    private bool m_isOnGround;
    private bool m_nextDash;
    private string m_groundTag;
    private float m_hurtTimeCnt;
    private bool m_isInjured;
    private int m_maxHealth;
    private float[] m_soundInterval = new float[10];


    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        Heal();
        AnimationCheck();
        Movement();
        Jump();
        Attack();
        GroundCheck();
        Dash();
        AudioControl();
        HurtIntervalUpdate();
        //更新生命值
        HealthCheck();
    }


    //########################################
    //#            PLAYER FUNCTION           #
    //########################################

    //Initialized Function
    void Initialize()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_anim = GetComponent<Animator>();
        m_attackTimeCnt = 0f;
        m_hurtTimeCnt = 0f;
        m_gravityRec = m_rb.gravityScale;
        m_maxHealth = m_health;
        m_nextJump = true;
        m_nextDash = true;
        for (int i = 0; i < m_soundInterval.Length; i++) 
        {
            m_soundInterval[i] = 0f;
        }
        m_atkEffect = GameObject.Find("Attack Zone").GetComponent<AttackController>();
        m_audioSourse = GetComponent<AudioSource>();
    }

    // Control player's movement
    void Movement()
    {
        //受伤期间禁止移动
        if(LockOperation())
        {
            return;
        }

        //Initialized
        float horizontalMove = Input.GetAxis("Horizontal");
        float faceDirection = Input.GetAxisRaw("Horizontal");

        //转身判定(攻击期间无法转向)
        if (faceDirection != 0)
        {
            float formerDirection = transform.localScale.x;
            //设置新的方向
            AnimatorStateInfo info = m_anim.GetCurrentAnimatorStateInfo(0);
            if (!info.IsName("Attacking1") && !info.IsName("Attacking2") && !info.IsName("AttackUp") && !info.IsName("AttackDown"))
            {
                transform.localScale = new Vector3(faceDirection, 1, 1);
            }
            //比较新方向和旧方向判断玩家是否转向
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
        //受伤期间禁止跳跃
        if (LockOperation())
        {
            return;
        }

        //防止按住跳跃键连跳，必须松开再按才能跳
        if (Input.GetButtonUp("Jump"))
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
        if (Input.GetButton("Jump") && m_isOnGround && !m_isAttacking 
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
            //if jump button up, stop jumping
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
        //必须添加纵向速度为0作为二号检测条件，否则起跳一瞬间也会判定on the ground
        if (m_isOnGround)
        {
            m_nextDash = true;
            if (m_isJumping || m_isFloating)
            {
                m_anim.SetTrigger("OnGround");
            }
            if (m_isFalling)
            {
                m_isFalling = false;
                m_anim.SetBool("Falling", false);
            }
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
        //受伤期间禁止攻击
        if (LockOperation())
        {
            return;
        }

        if (m_attackTimeCnt <= 0)
        {
            if (Input.GetButtonDown("Fire1") && !m_anim.GetBool("Attacking"))
            {
                m_attackTimeCnt = m_attackInterval;
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
            m_attackTimeCnt -= Time.deltaTime;
            //Combo Attack
            if (!m_isAttacking && !m_isFalling && !m_isFloating)
            {
                if (Input.GetButtonDown("Fire1") /*&& !m_anim.GetBool("Attacking")*/ && m_attackTimeCnt <= m_attackInterval * 0.5f)
                {
                    m_anim.SetBool("Attacking2", true);
                    m_isAttacking = true;
                }
            }
        }
    }

    void CallAttackEffect()
    {
        m_atkEffect.AttackFront();
    }

    void CallAttackUpEffect()
    {
        m_atkEffect.AttackUp();
    }

    void CallAttackDone()
    {
        m_atkEffect.AttackFinished();
    }

    void CallAttackDownEffect()
    {
        m_atkEffect.AttackDown();
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
        m_anim.SetBool("Attacking2", false);
        m_isAttacking = false;
    }

    void Dash()
    {
        //受伤禁止冲刺
        if (LockOperation())
        {
            return;
        }

        if (Input.GetButtonDown("Dash") && m_nextDash && m_dashTimeCnt <= 0 && !m_isAttacking)
        {
            m_dashTimeCnt = m_dashTime;
            m_nextDash = false;
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
                m_posRecX = transform.position.x;
            }
            m_isDashing = true;
            m_rb.gravityScale = 0;//No gravity effect while dashing
            m_rb.velocity = new Vector2(directionSymbol * m_dashForce, 0);
        }
        else
        {
            m_dashTimeCnt -= Time.deltaTime;
        }
        if (m_isDashing && (Mathf.Abs(transform.position.x - m_posRecX) > m_dashDistance))
        {
            DashFinished();
        }
    }

    void DashFinished()
    {
        m_anim.SetBool("Dash", false);
        if (m_isDashing && (m_isJumping || m_isFloating || m_isFalling))
        {
            m_isJumping = false;
            m_isFloating = false;
            m_isFalling = true;
            m_anim.SetBool("Jumping", false);
            m_anim.SetBool("Floating", false);
            m_anim.SetBool("Falling", true);
        }
        m_isDashing = false;
        m_rb.velocity = new Vector2(m_velocityRecX, 0);
        m_rb.gravityScale = m_gravityRec;
    }

    //Use raycast to check if player is on the ground
    void GroundCheck()
    {
        Vector2 pos = transform.position;
        BoxCollider2D boxColl = (BoxCollider2D)m_coll;
        Vector2 rightOffset = new Vector2(boxColl.size.x / 2, -boxColl.size.y / 2 );
        Vector2 leftOffset = new Vector2(-boxColl.size.x / 2, -boxColl.size.y / 2 );
        float distance = 0.1f;

        RaycastHit2D rightFoot = Physics2D.Raycast(pos + rightOffset + boxColl.offset, Vector2.down, distance, m_ground);
        RaycastHit2D leftFoot = Physics2D.Raycast(pos + leftOffset + boxColl.offset, Vector2.down, distance, m_ground);

        //Debug
        Color c = Color.red;
        if (rightFoot || leftFoot)
        {
            c = Color.green;
        }
        Debug.DrawRay(pos + rightOffset + boxColl.offset, Vector2.down, c, distance);
        Debug.DrawRay(pos + leftOffset + boxColl.offset, Vector2.down, c, distance);

        if (rightFoot || leftFoot)
        {
            m_isOnGround = true;
            m_groundTag = rightFoot.collider.tag;
        }
        else
        {
            m_isOnGround = false;
        }
    }

    void AudioControl()
    {
        //声音间隔时间冷却
        for (int i = 0; i < m_soundInterval.Length; i++)
        {
            if (m_soundInterval[i] > 0f)
            {
                m_soundInterval[i] -= Time.deltaTime;
            }
        }
        //判断当前动画
        AnimatorStateInfo info = m_anim.GetCurrentAnimatorStateInfo(0);
        Vector3 CamPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        //播放脚步声--0
        if ((info.IsName("Move") || info.IsName("Turning") || info.IsName("Rush")) && m_soundInterval[0] <= 0f) 
        {
            if (m_groundTag == "Grass")
            {
                m_audioSourse.PlayOneShot(m_soundFootstepsGrass, 1.0f);
                m_soundInterval[0] = m_soundFootstepsGrass.length;
            }
            else if (m_groundTag == "Stone")
            {
                m_audioSourse.PlayOneShot(m_soundFootstepsStone, 1.0f);
                m_soundInterval[0] = m_soundFootstepsStone.length;
            }
        }
        //播放攻击--1
       if ((info.IsName("Attacking1") || info.IsName("AttackUp") || info.IsName("AttackDown")) && m_soundInterval[1] <= 0f)
       {
                m_audioSourse.PlayOneShot(m_soundAtk1, 1.0f);
                m_soundInterval[1] = m_attackInterval;
        }
        //播放连击--2
        if (info.IsName("Attacking2") && m_soundInterval[2] <= 0f)
        {
            m_audioSourse.PlayOneShot(m_soundAtk2, 1.0f);
            m_soundInterval[2] = m_attackInterval;
        }
    }

    //防止动画卡死
    public void AnimationCheck()
    {
        AnimatorStateInfo info = m_anim.GetCurrentAnimatorStateInfo(0);
        if(!info.IsName("Attacking1") && m_anim.GetBool("Attacking"))
        {
            m_isAttacking = false;
            m_anim.SetBool("Attacking", false);
        }
        if (!info.IsName("Attacking2") && m_anim.GetBool("Attacking2"))
        {
            m_isAttacking = false;
            m_anim.SetBool("Attacking2", false);
        }
        if (!Input.GetButton("Horizontal") && (info.IsName("Move") || info.IsName("Idle")) && !m_isInjured)
        {
            m_anim.SetFloat("HorizontalSpeed", 0f);
            m_rb.velocity = new Vector2(0, m_rb.velocity.y);
        }
        if(!info.IsName("Floating") && m_anim.GetBool("Floating"))
        {
            m_anim.SetBool("Floating",false);
            m_isFloating = false;
        }
    }


    public void Hurt(int damage)
    {
        if (m_health > 0 && m_hurtTimeCnt <= 0)
        {
            //重置所有动画参数
            impulse.GenerateImpulse();
            m_isInjured = true;
            ResetAnim();
            m_health -= damage;
            m_anim.SetTrigger("Hurt");
            m_hurtTimeCnt = m_hurtInterval;
            float directionSymbol;
            if (transform.localScale.x > 0)
            {
                directionSymbol = 1f;
            }
            else
            {
                directionSymbol = -1f;
            }
            m_rb.velocity = new Vector2(directionSymbol * -5f, 6f);
            m_rb.gravityScale = m_gravityRec;//恢复重力，防止冲刺时受伤后起飞
            //if(!m_isOnGround)
            //{
            //    m_anim.SetBool("Falling", true);
            //    m_isFalling = true;
            //}
        }
    }

    public void HurtIntervalUpdate()
    {
        if (m_hurtTimeCnt>0)
        {
            m_hurtTimeCnt -= Time.deltaTime;
        }
    } 

    public void Heal()
    {
        if(Input.GetButtonUp("Heal") && IsIdle())
        {
            m_anim.SetTrigger("Healing");
            m_isHealing = true;
        }
    }

    public bool IsIdle()
    {
        if(!m_isAttacking && !m_isDashing && !m_isFalling && !m_isJumping && !m_isFalling && !m_isInjured && !m_isHealing)
        {
            return true;
        }
        return false;
    }

    public void HealingDone()
    {
        m_isHealing = false;
    }

    public void AddHealth()
    {
        if (m_health + m_healValue < m_maxHealth)
            m_health += m_healValue;
        else
            m_health = m_maxHealth;
    }

    public bool LockOperation()
    {
        if (InjureCheck())
        {
            return true;
        }
        if (m_isHealing)
        {
            return true;
        }
        return false;
    }


    public void ResetAnim()
    {
        m_anim.SetBool("Turning", false);
        m_anim.SetBool("Moving", false);
        m_anim.SetBool("Idling", false);
        m_anim.SetBool("Jumping", false);
        m_anim.SetBool("Floating", false);
        m_anim.SetBool("Falling", false);
        m_anim.SetBool("Attacking", false);
        m_anim.SetBool("Attacking2", false);
        m_anim.SetBool("AttackingUp", false);
        m_anim.SetBool("AttackingDown", false);
        m_anim.SetBool("Dash", false);
        m_anim.SetFloat("HorizontalSpeed", 0f);
        m_anim.SetFloat("VerticalSpeed", 0f);
        m_isAttacking = false;
        m_isDashing = false;
        m_isFalling = false;
        m_isFloating = false;
        m_isJumping = false;
        m_isHealing = false;
    }

    public void InjureFinish()
    {
        m_isInjured = false;
    }

    //检测是否在受伤期间（受伤起跳——落地）
    public bool InjureCheck()
    {
        if (m_isInjured)
            return true;
        return false;
    }

    void HealthCheck()
    {
        m_slider.value = m_health;
        if (m_health <= 0)
            Invoke("returnToMainMenu", 0f);
    }
       
    void returnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
