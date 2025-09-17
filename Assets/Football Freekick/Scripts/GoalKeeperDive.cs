using UnityEngine;

public class GoalkeeperDive : MonoBehaviour
{
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Call this when the ball is shot
    public void Dive()
    {
        int dir = Random.Range(0, 2); // 0 = left, 1 = right

        if (dir == 0)
        {
            anim.SetTrigger("left");
        }
        else
        {
            anim.SetTrigger("right");
        }
    }
}
