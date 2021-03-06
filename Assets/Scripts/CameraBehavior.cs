﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehavior : MonoBehaviour {

    BattleController battleController;

    public Animator cameraAnimator;
    public GameObject leftSprite;
    public GameObject rightSprite;
    public Camera cam;
    public bool cameraIntroIsDone;

    SpriteRenderer leftSpriteRender;
    SpriteRenderer rightSpriteRender;
    float animationSpeed = 1.0f;    //ideal is 3
    float t;
    float travelDistance = 20.0f;
    Vector3 leftCurrentPosition;    //Upated in Main Loop
    Vector3 rightCurrentPosition;
    Vector3 leftObjectStart;        //Animation Start positions
    Vector3 rightObjectStart;
    Vector3 leftObjectEnd;          //Animation End Positions
    Vector3 rightObjectEnd;
    float milliSeconds;
   

    // Use this for initialization
    void Start () {

        cam = GetComponent<Camera>();
        battleController = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<BattleController>();

        //get transform of objects
        leftObjectStart = new Vector3(20, 0, 13);
        rightObjectStart = new Vector3(-20, 0, 13);
        leftObjectEnd = new Vector3(-20, 0, 13);
        rightObjectEnd = new Vector3(20, 0, 13);
        //leftObjectEnd = new Vector3(leftObjectStart.x - travelDistance, leftObjectStart.y, leftObjectStart.z);
        //rightObjectEnd = new Vector3(leftObjectStart.x + travelDistance, leftObjectStart.y, leftObjectStart.z);

        //Get render components of both objects for flicker() method
        leftSpriteRender = leftSprite.GetComponent<SpriteRenderer>();
        rightSpriteRender = rightSprite.GetComponent<SpriteRenderer>();

    }

    // Update is called once per frame
    void Update () {

        //AnimationStates
        if (cameraAnimator.GetBool("cameraFocus"))
        {
            curtainSlide();
            introMovement();
        }

        
    }

    void curtainSlide()
    {
        //flash "Curtain" sprites
        //flicker();

        //track sprites position
        leftCurrentPosition = leftSprite.transform.localPosition;
        rightCurrentPosition = rightSprite.transform.localPosition;

        //sprite movement
        t += Time.deltaTime / animationSpeed;
        leftSprite.transform.localPosition = Vector3.Lerp(leftObjectStart, leftObjectEnd, t);
        rightSprite.transform.localPosition = Vector3.Lerp(rightObjectStart, rightObjectEnd, t);
        //check if End position is met and End loop
        if ((leftSprite.transform.localPosition == leftObjectEnd) && (rightSprite.transform.localPosition == rightObjectEnd))
        {
            //leftSpriteRender.enabled = false;
            //rightSpriteRender.enabled = false;
            cameraAnimator.SetBool("cameraFocus", false);
            cameraIntroIsDone = true;
            battleController.IsBattling = true;
        }
    }


    void flicker()
    {
            //flicker needs to be slowed down
            leftSpriteRender.enabled = !leftSpriteRender.isVisible;
            rightSpriteRender.enabled = !leftSpriteRender.isVisible;

    }

    void introMovement()
    {
        
    }

}
