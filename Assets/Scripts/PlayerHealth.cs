using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public float maxLife = 100f;
    public float currentLife;

    [Header("UI")]
    public Image lifeBar;

    private bool isDead = false;

    private void Start()
    {
        currentLife = maxLife;
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentLife -= damage;
        UpdateUI();

        // Impulso de cámara (YA lo tienes en tu controller)
        GetComponent<ThirdPersonController>()?.TakeDamage();

        if (currentLife <= 0)
        {
            Die();
        }
    }

    void UpdateUI()
    {
        if (lifeBar != null)
        {
            float percent = currentLife / maxLife;
            lifeBar.fillAmount = percent;

            lifeBar.color = Color.Lerp(Color.red, Color.green, percent);
        }
    }

    void Die()
    {
        isDead = true;

        // Bloquear movimiento
        GetComponent<ThirdPersonController>().enabled = false;

        // Reiniciar
        Invoke(nameof(RestartScene), 2f);
    }

    void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}