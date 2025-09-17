using UnityEngine;

public class GoalkeeperMove : MonoBehaviour
{
    public float speed = 3f;            // Movement speed
    public float moveRange = 3f;        // How far left and right goalkeeper can move

    private Vector3 startPos;
    private bool movingRight = true;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (movingRight)
        {
            transform.Translate(Vector3.right * speed * Time.deltaTime);
            if (transform.position.x > startPos.x + moveRange)
                movingRight = false;
        }
        else
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
            if (transform.position.x < startPos.x - moveRange)
                movingRight = true;
        }
    }
}
