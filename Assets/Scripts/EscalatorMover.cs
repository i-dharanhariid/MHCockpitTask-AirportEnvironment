using System.Collections;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Attach to an escalator entry trigger cube (e.g. "1Cube"). When the player enters,
/// keyboard movement is locked (mouse look stays free) and the player is carried
/// smoothly from this cube's position to the paired destination cube's position.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class EscalatorMover : MonoBehaviour
{
    [Tooltip("The destination cube this escalator carries the player to (e.g. '1PCube').")]
    public Transform destinationCube;

    [Tooltip("Time in seconds for the ride from this cube to the destination cube.")]
    public float rideDuration = 4f;

    private bool _isRiding;

    private void OnTriggerEnter(Collider other)
    {
        if (_isRiding || destinationCube == null) return;

        var fpc = other.GetComponent<FirstPersonController>();
        var input = other.GetComponent<StarterAssetsInputs>();
        var controller = other.GetComponent<CharacterController>();
        if (fpc == null || input == null || controller == null) return;

        StartCoroutine(RideEscalator(other.transform, fpc, input));
    }

    private IEnumerator RideEscalator(Transform player, FirstPersonController fpc, StarterAssetsInputs input)
    {
        _isRiding = true;

        Vector3 offset = player.position - transform.position;
        Vector3 startPos = player.position;
        Vector3 targetPos = destinationCube.position + offset;

        float elapsed = 0f;
        while (elapsed < rideDuration)
        {
            // Lock keyboard movement/jump but leave mouse look (input.look) untouched.
            input.move = Vector2.zero;
            input.jump = false;
            fpc.Grounded = true;

            player.position = Vector3.Lerp(startPos, targetPos, elapsed / rideDuration);

            elapsed += Time.deltaTime;
            yield return null;
        }

        player.position = targetPos;
        _isRiding = false;
    }
}
