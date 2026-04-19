using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float acceleration = 10f;
    public float deceleration = 30f;
    public float maxSpeed = 4f;
    private Rigidbody2D rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 input = new Vector2(horizontal, vertical);
        
        if (input.magnitude < 0.01f)
        {
            if (rb.linearVelocity.magnitude > 0.01f)
            {
                Vector2 decelerationVector = -rb.linearVelocity.normalized * deceleration * Time.deltaTime;
                
                if (decelerationVector.magnitude >= rb.linearVelocity.magnitude)
                {
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    rb.linearVelocity += decelerationVector;
                }
            }
            return;
        }

        Vector2 inputDirection = input.normalized;
        Vector2 targetVelocity = inputDirection * maxSpeed;
        
        Vector2 velocityChange = targetVelocity - rb.linearVelocity;
        Vector2 accelerationVector = velocityChange.normalized * acceleration * Time.deltaTime;
        
        if (accelerationVector.magnitude > velocityChange.magnitude)
        {
            accelerationVector = velocityChange;
        }
        
        rb.linearVelocity += accelerationVector;
        
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}
