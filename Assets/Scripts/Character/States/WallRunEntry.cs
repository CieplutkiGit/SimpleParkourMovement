public static class WallRunEntry
{
    public static bool TryEnter(PlayerController controller)
    {
        float forwardInput = controller.InputProvider != null ? controller.InputProvider.MoveInput.y : 0f;

        if (forwardInput <= 0.1f || !controller.WallRun.WallRunAvailable)
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
