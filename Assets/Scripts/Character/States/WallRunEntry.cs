using UnityEngine;

public static class WallRunEntry
{
    public static bool TryEnter(PlayerController controller)
    {
        float forwardInput = controller.InputProvider != null ? controller.InputProvider.MoveInput.y : 0f;

        if (forwardInput <= 0.1f || !controller.WallRun.WallRunAvailable)
            return false;

        Vector3 horizontalVelocity = controller.RB.linearVelocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.magnitude < controller.WallRun.MinEntrySpeed)
            return false;

        if (controller.WallDetector.IsWallFront)
        {
            controller.StateMachine.ChangeState(controller.WallRunUpState);
            return true;
        }

        if (controller.WallDetector.IsWallLeft || controller.WallDetector.IsWallRight)
        {
            controller.StateMachine.ChangeState(controller.WallRunState);
            return true;
        }

        return false;
    }
}
