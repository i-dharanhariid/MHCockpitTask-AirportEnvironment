using UnityEngine;

/// <summary>Swallows the footstep / land events baked into the locomotion clips so NPCs don't spam the console.</summary>
public class NpcAnimationEvents : MonoBehaviour
{
    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }
}
