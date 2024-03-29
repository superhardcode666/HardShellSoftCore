using System;
using UnityEngine;
using UnityEngine.UI;

public class Boost : MonoBehaviour
{
    [SerializeField] [Range(0f, 100f)] private float boostHeight = 4f;
    [SerializeField] [Range(0, 200)] private float maxBoostFuel = 200;
    [SerializeField] private float currentFuel;
    [SerializeField] [Range(0.1f, 1f)] private float fuelDepletionRate = 0.25f;
    [SerializeField] [Range(5f, 20f)] private float downwardMultiplier = 5f;
    [SerializeField] [Range(5f, 20f)] private float upwardMultiplier = 5f;

    [SerializeField] private float boostFuelUsed;

    [SerializeField] private AudioClip boostClip;

    [SerializeField] private Image fuelMeter;
    [SerializeField] private float fuelMeterLerpSpeed;
    private readonly float defaultGravityScale = 1f;

    private AudioSource audioSource;

    private bool boosterRequested;

    private InputHandler inputHandler;
    private InputSource inputSource;
    private bool isBoosting;
    private bool isOnGround;

    private Rigidbody2D rigidbody2D;
    private Collider2D collider2D;
    
    private Vector2 velocity;

    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
        if (inputHandler.InputSource != null) inputSource = inputHandler.InputSource;

        rigidbody2D = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<Collider2D>();
        
        audioSource = GameObject.FindWithTag("AudioPlayer").GetComponent<AudioSource>();

        currentFuel = maxBoostFuel;
    }

    private void Update()
    {
        if (!inputHandler.IsInputActive()) return;

        boosterRequested = inputHandler.InputSource.GetBoosterInput();

        velocity = rigidbody2D.velocity;


        // check if a boost was requested and perform it
        if (boosterRequested)
        {
            if (currentFuel >= 0)
            {
                isBoosting = true;
                PerformBoost();
            }
            else
            {
                isBoosting = false;
            }
        }
        else
        {
            isBoosting = false;
        }


        FuelMeterUpdate();

        // multipliers
        if (rigidbody2D.velocity.y > 0)
            // we're going up
            rigidbody2D.gravityScale = upwardMultiplier * 10;
        else if (rigidbody2D.velocity.y < 0)
            // we're going down
            rigidbody2D.gravityScale = downwardMultiplier;
        else if (rigidbody2D.velocity.y == 0)
            // we're on the ground
            rigidbody2D.gravityScale = defaultGravityScale;

        rigidbody2D.velocity = velocity;
    }

    private void FixedUpdate()
    {
        if (isBoosting) PerformBoost();
    }

    // TODO put this in the fucking UIManager
    private void FuelMeterUpdate()
    {
        fuelMeter.fillAmount = Mathf.Lerp(fuelMeter.fillAmount, currentFuel / maxBoostFuel, fuelMeterLerpSpeed);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("ChargeZone"))
        {
            if (currentFuel < maxBoostFuel)
                currentFuel += 1.25f * fuelDepletionRate;
        }
    }

    private void PerformBoost()
    {
        currentFuel -= 1 * fuelDepletionRate;

        var boostSpeed = Mathf.Sqrt(-2f * Physics2D.gravity.y * boostHeight);

        if (velocity.y >= 0) boostSpeed = Mathf.Max(boostSpeed - velocity.y, 0f);

        velocity.y += boostSpeed;
    }
}