using UnityEngine;

public class ScratchMarks : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField]
    private float lifetime = 10f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
