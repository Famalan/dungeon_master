using NUnit.Framework;
using UnityEngine;

public class PlayerControllerEditModeTests
{
    GameObject playerObject;

    [TearDown]
    public void TearDown()
    {
        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void TeleportToWorksBeforeStart()
    {
        playerObject = new GameObject("Player");
        CharacterController characterController = playerObject.AddComponent<CharacterController>();
        PlayerController playerController = playerObject.AddComponent<PlayerController>();
        Vector3 targetPosition = new Vector3(3f, 1.25f, -2f);

        Assert.DoesNotThrow(() => playerController.TeleportTo(targetPosition));
        Assert.That(Vector3.Distance(playerObject.transform.position, targetPosition), Is.LessThan(0.001f));
        Assert.IsTrue(characterController.enabled);
    }
}
