namespace shared.GameObjects;

public class Ball
{
    public float PositionX { get; set; } = Constants.GAME_WIDTH / 2f - Constants.BALL_RADIUS / 2f;
    public float PositionY { get; set; } = Constants.GAME_HEIGHT / 2f - Constants.BALL_SPEED / 2f;
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
}