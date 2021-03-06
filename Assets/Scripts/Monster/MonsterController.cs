﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]

public class MonsterController : MonoBehaviour
{

    UserController userController;
    public PlayerController playerController;
    public BattleController BC;
    public PlayerHandController playerHand;

    public Monster monster;
    public State monsterState;
    public GameObject trackingTickObject;
    public PlayerTickController playerTickController;
    public Canvas tickCanvas;
    public Image[] tickImages;
    public Material monsterMat;

    [SerializeField]
    private GameObject healthTextGameObject;
    [SerializeField]
    private GameObject blockTextGameObject;

    public enum State
    {
        WAITING,    //waiting until turn
        READY,      //when monster is ready 
        SELECTING,  //is ready now player chooses action
        CHARGING,   //wait time delay needed to perform action
        PERFORMING,
        DEAD,
        PAUSED,
        STUNNED
    }

    //used to quickly identify in the queuer the type of monster 
    public enum Team
    {
        UNASSIGNED,
        ENEMY,
        PLAYER
    }

    [SerializeField]
    public List<Attack> moveSet;
    public bool done;
    public bool isSpeedPaused;
    bool targeted;
    public bool isDead;
    public bool isChargingToAttack;
    public Team team;
    public new string name;
    public string description;
    public string spriteFile;
    public int currentHealth, maxHealth, attack, defense, level;
    public int baseAttack, baseDefense;
    public float currentSpeed, baseSpeed;
    public float chargeTimer, chargeDuration;
    public int damage;
    public int block;
    private RuntimeAnimatorController animator;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    //use this to check if targeted then change color
    Color lerpedColor;

    //used for the info panel
    GameObject panelInfoObject;
    Vector2 canvasPos;
    Vector2 screenPointToTarget;
    Text[] panelInfoText;

    //used for internal turns
    [SerializeField]
    private HandleTurn currentAction;
    public HandleTurn previousAction;
    public List<HandleTurn> actions = new List<HandleTurn>();
    public List<GameObject> targets = new List<GameObject>();
    private bool comboFinish;
    public bool combosQueued;
    public int stunNumberOfTurns;
    public bool stunCheckFinish;
    public float stunCounter;

    public float AddDuration
    {
        set { chargeDuration += value; }
    }

    public bool DoneComboing
    {
        get { return comboFinish;  }
        set { comboFinish = value; }
    }

    //at the end of every attack after executed (or canceled call this)
    public void ResetAttack()
    {
        if (team == Team.ENEMY)
        {
            trackingTickObject.GetComponent<EnemyTickController>().state = EnemyTickController.GaugeState.RESET;
            DoneComboing = true;
        }
        if (team == Team.PLAYER)
        {
            if (playerTickController == null)
            {
                playerTickController = GameObject.FindGameObjectWithTag("PlayerTick").GetComponent<PlayerTickController>();
            }
            playerHand.GetComponent<PlayerHandController>().state = PlayerHandController.State.IDLE;

            playerTickController.ChangeState(PlayerTickController.GaugeState.INCREASING);

            userController.IsUsersTurn = false;
            BC.HideCombatUI();
        }
        chargeDuration = 0.0f;
        chargeTimer = 0.0f;
        currentSpeed = 0.0f;
        isChargingToAttack = false;
        monsterState = State.WAITING;
        done = false;

        stunCheckFinish = false;
    }

    public GameObject SetHealthTextObject
    {
        set { healthTextGameObject = value;  }
    }

    public GameObject SetBlockUIText
    {
        set { blockTextGameObject = value; }
    }

    public float CurrentSpeed
    {
        get { return currentSpeed; }
        set { currentSpeed = value; }
    }

    public void ChangeState(State state)
    {
        monsterState = state;
        done = false;
    }

    public void StartTest()
    {
        StartCoroutine(FlashInput());
    }

    public void ResetTargetedToClear()
    {
        if (tickCanvas == null)
        {
            tickCanvas = trackingTickObject.GetComponent<Canvas>();
        }

        if (monsterMat == null)
        {
            monsterMat = GetComponent<Renderer>().material;
        }

        monsterMat.color = Color.white;

        tickImages = trackingTickObject.GetComponentsInChildren<Image>();
        tickImages[0].color = Color.white;
        tickImages[1].color = Color.white;

        tickCanvas.overrideSorting = false;
    }

    public void IsTargeted(bool b)
    {
        if (tickCanvas == null)
        {
            tickCanvas = trackingTickObject.GetComponent<Canvas>();
        }

        if(monsterMat == null)
        {
            monsterMat = GetComponent<Renderer>().material;
        }

        if (b)
        {
            monsterMat.color = new Color(1, 0.5f, 0.5f, 1);

            tickImages = trackingTickObject.GetComponentsInChildren<Image>();
            tickImages[0].color = Color.red;
            tickImages[1].color = new Color(1, 0.5f, 0.5f, 1);
            tickCanvas.overrideSorting = true;
            tickCanvas.sortingOrder = 3;
        }
        else
        {
            monsterMat.color = Color.white;

            tickImages = trackingTickObject.GetComponentsInChildren<Image>();
            tickImages[0].color = Color.white;
            tickImages[1].color = Color.white;

            tickCanvas.overrideSorting = false;
        }

    }

    public void IsPlayerTargeted(bool b)
    {
        if (tickCanvas == null)
        {
            tickCanvas = trackingTickObject.GetComponent<Canvas>();
        }

        if (monsterMat == null)
        {
            monsterMat = GetComponent<Renderer>().material;
        }


        if (b)
        {
            monsterMat.color = new Color(0.5f, 1, 0.5f, 1);            //light green

            tickImages = trackingTickObject.GetComponentsInChildren<Image>();
            tickImages[0].color = Color.green;
            tickImages[1].color = new Color(0.5f, 1, 0.5f, 1);
            tickCanvas.overrideSorting = true;
            tickCanvas.sortingOrder = 3;
        }
        else
        {
            monsterMat.color = Color.white;

            tickImages = trackingTickObject.GetComponentsInChildren<Image>();
            tickImages[0].color = Color.white;
            tickImages[1].color = Color.white;

            tickCanvas.overrideSorting = false;
        }

    }

    int CheckBlockFor(int damage)
    {
        int newBlock = block - damage;
        //if we block all or some damage
        if (newBlock >= 0)
        {
            BC.SpawnBattleTextAboveWithString(this.gameObject, "Blocked");
            block = newBlock;
            return 0;
        }
        else
        //no more block remaining then do damage
        {
            BC.SpawnBattleTextAbove(this.gameObject, damage);
            block = 0;
            return Mathf.Abs(newBlock);
        }
        
    }

    public void Damage(int amount)
    {
        //this.currentHealth -= amount;
        this.currentHealth -= CheckBlockFor(amount);

        //Play damage sprite here
        if (this.currentHealth <= 0)
        {
            this.currentHealth = 0;
            this.isDead = true;
        }

        if (isDead)
        {
            monsterState = State.DEAD;

            //run death animation here
            //Design decision: either fade out and disable now, or allow them to be revived?

            if (team == Team.PLAYER)
            {
                //display losing text
                BC.PlayerLose();
            }

            if (team == Team.ENEMY)
            {
                if (BC.AllEnemiesDead())
                {
                    BC.PlayerWin();
                }
            }

        }
    }

    public void Heal(int amount)
    {
        this.currentHealth += amount;
        if (this.currentHealth < 0)
        {
            this.currentHealth = 0;
            this.isDead = true;
        }
    }

    void DoDeath()
    {
        //Hide Tick
        //Design Decision: Animate an X crossing out animation
        trackingTickObject.SetActive(false);
        //plays fade animation
        //healthTextGameObject.GetComponent<HealthText>().StartCoroutine(DeathFade());
        StartCoroutine(DeathFade());
        
    }

    void SpawnEnemy()
    {
        //will replace this with scriptable objects?
        if (!MonsterInfoDatabase.IsPopulated)
        {
            MonsterInfoDatabase.Populate();
        }

        if (team == Team.PLAYER)
        {
            //Set to squidra for now until we expand on graphics
            monster = MonsterInfoDatabase.monsters[0];
            //Set tick icon concatenate "f" for friendly
            spriteFile = monster.MonsterBase.spriteFile + "_f";
            trackingTickObject.GetComponent<PlayerTickController>().SetTickIcon(spriteFile);
        }
        else
        {
            //for now we will just a random selection within our database
            //We can expand on this later
            int randomIndex = Random.Range(0, MonsterInfoDatabase.monsters.Count);   //returns 0 - 1
            monster = MonsterInfoDatabase.monsters[randomIndex];
            //monster = MonsterInfoDatabase.monsters[1];
            //get moves from the "deck" of the enemy
            moveSet = monster.MoveSet;
            //Set tick icon
            spriteFile = monster.MonsterBase.spriteFile;
            trackingTickObject.GetComponent<EnemyTickController>().SetTickIcon(spriteFile);
        }
        name = monster.MonsterBase.name;
        description = monster.MonsterBase.description;
        if (team == Team.PLAYER)
        {
            //We concatenate _b to refer rear sprite images for player sprites
            spriteFile = monster.MonsterBase.spriteFile + "_b";
        }
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
        //add slight variance so icons are not the same
        float variance = Random.Range(-.5f, .5f);
        baseSpeed = monster.Speed + variance;
        BC.ThresholdUpdate(baseSpeed);
        //Debug.Log(monster.Print);
    }

    //randomly select an attack in their move list
    void SelectAttack()
    {
        //if (!stunCheckFinish)
        //{
        //    ForceCheckOwnerOfStuns();
        //}

        //prevent enemies from continue attack until user does something on their turn
        if (userController.IsUsersTurn)
        {
            return;
        }

        BC.PauseSpeedsForAllMonsters(true); 

        //randomly select attack from attack set for enemies
        if (team == Team.ENEMY)
        {
            int random = Random.Range(0, moveSet.Count);
            damage = moveSet[random].damage;
            chargeDuration = moveSet[random].chargeTime;
            bool isCanceling = moveSet[random].isCanceling;
            List<GameObject> target = new List<GameObject>();
            //will always target player unless a buff or heal
            target.Add(BC.player);
            HandleTurn turn = new HandleTurn(this.gameObject, target, moveSet[random].targetArea, moveSet[random].damage, moveSet[random].block, chargeDuration, isCanceling, stunNumberOfTurns, DrawType.NONE);
            //BC.AddTurnToQueue(turn);
            actions.Add(turn);

            //reset the current speed for measuring stuns
            currentSpeed = 0;

            //Begin charging
            EnemyTickController enemyTickController = trackingTickObject.GetComponent<EnemyTickController>();
            enemyTickController.ChangeState(EnemyTickController.GaugeState.CHARGING);
            monsterState = State.CHARGING;
            BC.PauseSpeedsForAllMonsters(false);
        }
        //wait until card controller changes users state from here
        if (team == Team.PLAYER)
        {
            userController.IsUsersTurn = true;
            //BC.PauseSpeedsForAllMonsters(false); is set in card controller
        }
    }

    void IncreaseSpeed()
    {
        //do not do anything while animations are occuring during attack execution
        if (BC.MonsterIsAnimating)
        {
            return;
        }

        //this will prevent speed increasing when pause menu is open, end of game, or user's turn
        if (BC.PauseGame || userController.IsUsersTurn || (BC.BattleState == BattleController.State.ENDCONDITION) || (BC.BattleState == BattleController.State.BEGIN))
        {
            return;
        }

        if (currentSpeed >= BC.Threshold)
        {
            monsterState = State.READY;
        }
        else
        {
            currentSpeed += baseSpeed * Time.deltaTime;
        }
    }

    void ChargeSpeed(float durationInSeconds)
    {
        //do not do anything while animations are occuring during attack execution
        if (BC.MonsterIsAnimating)
        {
            return;
        }

        //this will prevent speed increasing when still user's turn
        if (BC.PauseGame || userController.IsUsersTurn || (BC.BattleState == BattleController.State.ENDCONDITION))
        {
            return;
        }

        if (chargeTimer >= durationInSeconds) //change back to BC.Threshold for testing
        {
            monsterState = State.PERFORMING;
            //ExecuteTurn();
        }
        else
        {
            isChargingToAttack = true;
            chargeTimer += Time.deltaTime;
        }
    }

    public void RemoveActionAtIndex(int i)
    {
        actions.RemoveAt(i);
    }



    public void ExecuteTurn()
    {
   
        //if we have nothing left exit, otherwise continue combos etc
        if (actions.Count == 0)
        {
            ResetAttack();
            //BC.MonsterIsAnimating = false; //this is called at the animation IEnumarators instead at the last frame
            DoneComboing = true;             //used to determine if we have completed all our actions
            return;
        }

        //check if an animation is occuring if so then return
        if (BC.MonsterIsAnimating)
        {
            return;
        }

        previousAction = actions[0];
        currentAction = previousAction;
        targets = currentAction.targets;
        //this will pause time for us so other monsters dont increment
        BC.MonsterIsAnimating = true;

        if(currentAction.targetArea == TargetArea.SINGLE)
        {
            StartCoroutine(BC.BeginAttack(currentAction.owner, currentAction.targets[0], 0.5f));
        }
        else
        {
            //AoE attacks wont animate in this case this is required
            BC.MonsterIsAnimating = false;
        }
        
        //Apply effects to player
        if(currentAction.drawType != DrawType.NONE)
        {
            //CheckDraw();
        }

        //Apply effect to targets
        foreach(GameObject target in targets)
        {
            // Attack Canceling?
            if (currentAction.isCanceling)
            {
                MonsterController targetsMonsterController = target.GetComponent<MonsterController>();
                //we have to make sure the target has an action queued up otherwise just do damage as normal
                if(targetsMonsterController.actions.Count >= 1)
                {
                    targetsMonsterController.RemoveActionAtIndex(0);
                    BC.SpawnBattleTextAboveWithString(target, "Attack Canceled!");
                }
            }

            // Stunning?
            // if we stun enemy add us as the owner otherwise increase the number
            if(currentAction.stunNumberOfTurns > 0)
            {
                Stun stun = target.GetComponent<Stun>();

                if(stun == null)
                {
                    target.AddComponent<Stun>();
                    stun = target.GetComponent<Stun>();
                    stun.Owner = this.gameObject;
                }
                else
                {
                    Debug.Log("Stuns found adding " + currentAction.stunNumberOfTurns);
                    stun.AddTurns(currentAction.stunNumberOfTurns);
                } 
                
            }

            //Are we increasing this monsters current armor?
            if(currentAction.block > 0)
            {
                ApplyBlock();
            }

            //Display only if damage was > 0
            if(currentAction.damage != 0)
            {
                target.GetComponent<MonsterController>().Damage(currentAction.damage);
                // moved text generation to Damage to control what is outputted
                //BC.SpawnBattleTextAbove(target, previousAction.damage);
            }

        }

        actions.RemoveAt(0);
        currentAction.Clear();
        ExecuteTurn();
    }

    void ApplyBlock()
    {
        this.block += currentAction.block;
        BC.SpawnBattleTextAboveWithString(this.gameObject, "Gained " + currentAction.block + " Block");
    }

    void OnMouseEnter()
    {

        if (userController.MenuIsActive || (BC.BattleState == BattleController.State.ENDCONDITION) || (BC.BattleState == BattleController.State.BEGIN))
        {
            return;
        }

        if (isDead)
            return;

        if(team == Team.PLAYER)
        {
            IsPlayerTargeted(true);
            return;
        }
        else
        {
            IsTargeted(true);
            UpdateInfo();
        }
    }

    void OnMouseExit()
    {

        if (isDead)
        {
            return;
        }  

        IsTargeted(false);
        RemoveInfo();
    }

    void RemoveInfo()
    {
        if (panelInfoObject == null)
        {
            return;
        }

        if (team == Team.PLAYER)
        {
            return;
        }
        else
        {
            //unhighlight tick
            trackingTickObject.GetComponent<Image>().color = Color.white;
            //move info out of camera frame
            panelInfoObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(300, 400);
        }

    }

    void UpdateInfo()
    {
        if (!BC.isBattling)
        {
            return;
        }

        //obtain panel gameobject
        if (panelInfoObject == null)
        {
            panelInfoObject = GameObject.FindGameObjectWithTag("InfoPanel");
        }
        //obtain panel text
        if (panelInfoText == null)
        {
            panelInfoText = panelInfoObject.GetComponentsInChildren<Text>();
        }
        //set text
        panelInfoText[0].text = this.name;
        panelInfoText[1].text = this.description;
        //capture screen Pos in 2d space from 3d space
        screenPointToTarget = Camera.main.WorldToScreenPoint(this.transform.position);
        // Convert screen position to Canvas / RectTransform space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(BC.canvasRect, screenPointToTarget, null, out canvasPos);
        //move text to Game object's position
        panelInfoObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(canvasPos.x - 100, canvasPos.y -50);
    }

    void SetupHandForPlayer()
    {
        if(team == Team.PLAYER && playerHand.state == PlayerHandController.State.IDLE)
        {
            playerHand.isDrawing = true;
            playerHand.SetupHand();
            BC.EndTurnButton.SetActive(true);
        }
    }

    IEnumerator DeathFade()
    {
        for (float f = 1f; f > 0f; f -= 0.005f)
        {
            spriteRenderer.color = new Color(1, 1, 1, f); 
            yield return null;
        }
        //this.gameObject.SetActive(false); //do not set this to false, we may want to "Revive" in the future?
    }

    IEnumerator FlashInput()
    {
        for (float f = 1f; f >= 0.0f; f -= 0.1f)
        {
            spriteRenderer.color = new Color(1, f, f, 1);
            //yield return null;
        }
        for (float f = 0.0f; f <= 1.0f; f += 0.1f)
        {
            spriteRenderer.color = new Color(1, f, f, 1);
            //yield return null;
            yield return new WaitForSeconds(2f);
        }
    }

    IEnumerator SimulatePoison()
    {
        monster.ChangeHealth(-1);
        yield return new WaitForSeconds(1f);
    }

    void IncreaseStun()
    {
        if (BC.MonsterIsAnimating)
        {
            return;
        }

        //this will prevent speed increasing when pause menu is open, end of game, or user's turn
        if (BC.PauseGame || userController.IsUsersTurn || (BC.BattleState == BattleController.State.ENDCONDITION) || (BC.BattleState == BattleController.State.BEGIN))
        {
            return;
        }

        if (stunCounter >= BC.Threshold)
        {
            //remove stun component
            Stun stunComponent = GetComponent<Stun>();
            if (stunComponent != null)
            {
                stunComponent.Check();
                //reset speed and continue this process until the stun component changes this monsters status back to its originalState
                stunCounter = 0;
            }
            else
            {
                Debug.Log("IncreaseStun() was likely triggered but no stun component found for " + this.gameObject);
            }
        }
        else
        {
            stunCounter += baseSpeed * Time.deltaTime;
        }
    }

    void Awake()
    {
        if (BC == null)
        {
            BC = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<BattleController>();
        }
        
        if (animator == null)
        {
            animator = GetComponent<RuntimeAnimatorController>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Start ()
    {
        //get reference to UserController to check if mouse is on top of this monster
        userController = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<UserController>();
        if (team == Team.PLAYER && playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            playerHand = GameObject.FindGameObjectWithTag("Hand").GetComponent<PlayerHandController>();
        }
        SpawnEnemy();
    }

    void Update ()
    {
        if (done)
        {
            return;
        }

        switch (monsterState)
        {
            case (State.WAITING):
                IncreaseSpeed();
                break;
            case (State.READY):
                done = true;
                SetupHandForPlayer();
                ChangeState(State.SELECTING);
                break;
            case (State.SELECTING):
                SelectAttack();
                break;
            case (State.CHARGING):
                ChargeSpeed(chargeDuration);
                break;
            case (State.DEAD):
                DoDeath();
                break;
            case (State.PAUSED):
                break;
            case (State.STUNNED):
                IncreaseStun();
                break;
            case (State.PERFORMING):
                ExecuteTurn();
                break;
            default:
                break;
        }

    }
}
