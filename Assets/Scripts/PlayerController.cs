﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    UserController userController;
    public BattleController BC;
    public PlayerHandController playerHand;
    public PlayerTickController playerTickController;


    public bool draw;
    public PlayerState currentUserState;
    //public bool done;
    bool count;
    public bool isDead;
    public bool isCharingToAttack;
    public GameObject EndTurnButton;
    public EndTurnButtonController endTurnButtonScript;

    public enum PlayerState
    {
        WAITING,    //waiting until turn
        READY,      //when monster is ready / drawing cards
        SELECTING,  //is ready now player chooses action
        CHARGING,   //wait time delay needed to perform action
        PERFORMING,
        DEAD
    }

    public Monster monster;
    public string spriteFile;
    public int currentHealth, maxHealth, attack, defense, level;
    public float currentSpeed, baseSpeed;
    //charge timer is time increrent on update
    //charging time is time passed by the card
    public float chargeTimer, chargeDuration;
    public int baseAttack, baseDefense;
    public GameObject HealthBarPrefab;
    private RuntimeAnimatorController animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    bool targeted;
    Color lerpedColor;
    //use this to check if targeted then change color

    public bool IsTargeted
    {
        set { targeted = value; }
    }

    //at the end of every attack after executed (or canceled call this)
    public void ResetAttack()
    {
        Debug.Log("Resetting");
        if (playerTickController == null)
        {
            playerTickController = GameObject.FindGameObjectWithTag("PlayerTick").GetComponent<PlayerTickController>();
        }
        //playerTickController.state = PlayerTickController.GaugeState.RESET;
        playerTickController.ChangeState(PlayerTickController.GaugeState.INCREASING);
        chargeTimer = 0.0f;
        currentSpeed = 0.0f;
        isCharingToAttack = false;
        currentUserState = PlayerState.WAITING;
        EndTurnButton.SetActive(false);
    }

    public void HideCombatUI()
    {
        EndTurnButton.SetActive(false);
    }

    public void Damage(int amount)
    {
        this.currentHealth -= amount;
        //Play damage sprite here
        if (this.currentHealth <= 0)
        {
            this.currentHealth = 0;
            this.isDead = true;
        }

        if (isDead)
        {
            //run death animation here
            //Design decision: either fade out and disable now, or allow them to be revived?

            //end battle scene
            currentUserState = PlayerState.DEAD;
            //pause all enemies
            BC.PauseMonsters();
            //display losing text
            BC.PlayerLose();
        }
    }

    void CheckAttack()
    {
        if (currentSpeed > BC.Threshold)
        {
            BC.IsPaused = true;
            Debug.Log(this.gameObject + " Speed at:" + currentSpeed);
        }
    }

    public void ResetTurn(int threshold)
    {
        currentSpeed -= threshold;
    }

    void Awake()
    {
        currentUserState = PlayerState.WAITING;
        BC = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<BattleController>();
        animator = GetComponent<RuntimeAnimatorController>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            Debug.Log(this.gameObject + " monster animator is null");
        if (spriteRenderer == null)
            Debug.Log(this.gameObject + " monster sprite renderer is null");

        if (!MonsterInfoDatabase.IsPopulated)
            MonsterInfoDatabase.Populate();
    }

    // Use this for initialization
    void Start()
    {
        if (EndTurnButton == null)
        {
            EndTurnButton = GameObject.FindGameObjectWithTag("EndTurnButton");
            endTurnButtonScript = EndTurnButton.GetComponent<EndTurnButtonController>();
        }
        EndTurnButton.SetActive(false);

        //get reference to UserController to check if mouse is on top of this monster
        userController = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<UserController>();
        //get reference to hand to draw cards
        playerHand = GameObject.FindGameObjectWithTag("Hand").GetComponent<PlayerHandController>();
        //Set to squidra for now until we expand on graphics
        monster = MonsterInfoDatabase.monsters[0];
        //We concatenate _b to refer rear sprite images
        spriteFile = monster.MonsterBase.spriteFile + "_b";
        spriteRenderer.sprite = Resources.Load<Sprite>("Sprites/mon/mon_" + spriteFile);
        //We add box collider later because it inherits the sprites dimensions otherwise 
        //the box collider would not generate the appropriate size for the monster
        gameObject.AddComponent<BoxCollider2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        if (boxCollider2D == null)
        {
            Debug.Log(this.gameObject + " does not have a BoxCollider2d");
        }
        currentHealth = monster.Health;
        maxHealth = monster.MaxHealth;
        baseSpeed = monster.Speed;
        BC.ThresholdUpdate(baseSpeed);
        Debug.Log(monster.Print);
    }

    void IncreaseSpeed()
    {
        if (currentSpeed >= BC.Threshold) 
        {
            currentUserState = PlayerState.READY;
        }
        else
        {
            currentSpeed += baseSpeed * Time.deltaTime;
        }
    }

    void ChargeSpeed(float durationInSeconds)
    {
        if (chargeTimer >= durationInSeconds) //change back to BC.Threshold for testing
        {
            //this will ensure it will not reset without trigger if it is not their turn
            if (BC.turnList[0].owner == this.gameObject)
            {
                BC.ExecuteTurnFor(this.gameObject);
                ResetAttack();
                //currentUserState = PlayerState.WAITING;
            }
        }
        else
        {
            isCharingToAttack = true;
            chargeTimer += Time.fixedDeltaTime;
        }
    }

    void Update()
    {
        switch (currentUserState)
        {
            case (PlayerState.WAITING):
                IncreaseSpeed();
                break;
            case (PlayerState.READY):
                playerHand.SetupHand();
                EndTurnButton.SetActive(true);
                currentUserState = PlayerState.SELECTING;
                break;
            case (PlayerState.SELECTING):
                //Necessary to prevent monsters from increasing their speeds when its players turn
                BC.PauseSpeedsForEnemies(true);
                Debug.Log("Waiting for user to select cards");
                break;
            case (PlayerState.CHARGING):
                ChargeSpeed(chargeDuration);
                break;
            case (PlayerState.DEAD):
                HideCombatUI();
                break;
            default:
                break;
        }
    }

}