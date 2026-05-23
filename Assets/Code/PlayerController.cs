using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed = 10f;
    public float sprintMultiplier = 3f;
    public float lookSensitivity = 2f;

    float yaw;
    float pitch;
    bool isActive = true;

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        // Escape unlocks cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Left-click reactivates camera ONLY if not clicking UI
        if (!isActive && Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                LockCursor();
            }
        }

        if (!isActive)
            return;

        // Mouse look
        yaw += Input.GetAxis("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Movement
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            speed *= sprintMultiplier;

        Vector3 input =
            transform.forward * Input.GetAxisRaw("Vertical") +
            transform.right * Input.GetAxisRaw("Horizontal");

        transform.position += input.normalized * speed * Time.deltaTime;

        // Up / Down
        if (Input.GetKey(KeyCode.E))
            transform.position += Vector3.up * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.Q))
            transform.position -= Vector3.up * speed * Time.deltaTime;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isActive = true;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isActive = false;
    }
}
