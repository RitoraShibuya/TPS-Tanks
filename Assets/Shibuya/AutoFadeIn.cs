using UnityEngine;

public class AutoFadeIn : MonoBehaviour
{
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animationName = "FadeOut";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetAnimator.Play(animationName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
