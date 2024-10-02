using UnityEngine;

public class PlayerViewCamera : MonoBehaviour
{
    public GameObject attachedObject;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public float movementSpeed = 10.0f;
    public float boostMultiplier = 2.0f;
    public float rotationSpeed = 2.0f;
    
    private Vector3 direction;

    void Update()
    {
        if (attachedObject == null){
            PlayerControl();
            //attachedObject = GameObject.FindGameObjectWithTag("Robot");
        } else {
            AttachedControl();
        }

    }

    void PlayerControl(){
        // Handle rotation using the mouse
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        // Rotate the object along the y-axis (left/right) and x-axis (up/down)
        transform.Rotate(Vector3.up, mouseX);
        transform.Rotate(Vector3.left, mouseY);

        // Get movement input (WASD and arrow keys)
        direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Move up and down (space and left shift)
        if (Input.GetKey(KeyCode.Space)) direction.y += 1;
        if (Input.GetKey(KeyCode.LeftShift)) direction.y -= 1;

        // Handle boost speed
        float currentSpeed = Input.GetKey(KeyCode.LeftControl) ? movementSpeed * boostMultiplier : movementSpeed;

        // Move the character based on the input
        transform.Translate(direction * currentSpeed * Time.deltaTime, Space.Self);
    }

    void AttachedControl(){
        Transform lookat_transform = attachedObject.transform;
        lookat_transform.Translate(lookat_transform.right * -1f + lookat_transform.up * 0.5f);
        transform.position = lookat_transform.position;
        transform.LookAt(attachedObject.transform);
        
    }
}
