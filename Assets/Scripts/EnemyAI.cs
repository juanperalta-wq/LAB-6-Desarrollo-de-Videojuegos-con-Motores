using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;

    [Header("Stats")]
    public float damage = 20f;
    public float life = 30f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            ThirdPersonController controller = other.GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                controller.verticalVelocity = 5f;
            }

            Destroy(gameObject, 0.2f);
        }
    }

    public void TakeDamage(float dmg)
    {
        life -= dmg;

        if (life <= 0)
        {
            Destroy(gameObject);
        }
    }
}