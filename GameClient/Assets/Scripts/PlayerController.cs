using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Animator animator;

    public float dragSensitivity = 0.02f;
    public float minX = -4f, maxX = 4f;
    public float minZ = -2f, maxZ = 2f;

    public GameObject projectilePrefab;
    public float fireInterval = 0.5f;

    public int maxHP = 3;
    private int currentHP;
    private float fireTimer = 0f;
    private Vector3 lastMousePos;
    private bool isDragging = false;

    void Start()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        HandleDrag();

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.mousePosition.y < Screen.height * 0.5f)
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
            }
        }
        if (Input.GetMouseButtonUp(0)) isDragging = false;

        if (isDragging)
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x + delta.x * dragSensitivity, minX, maxX);
            pos.z = Mathf.Clamp(pos.z + delta.y * dragSensitivity, minZ, maxZ);
            transform.position = pos;
            lastMousePos = Input.mousePosition;
        }
    }

    void Fire()
    {
        Vector3 spawnPos = transform.position + Vector3.forward * 1f;
        Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (animator != null)
        {
            animator.SetTrigger("attack");
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("Enemy"))
        {
            currentHP--;
            Destroy(col.gameObject);
            Debug.Log($"HP: {currentHP}");
            if (currentHP <= 0)
            {
                Debug.Log("Game Over");
                Time.timeScale = 0f;
            }
        }
    }
}