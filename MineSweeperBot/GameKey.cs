namespace MineSweeperBot;

public class GameKey
{
    private long UserId { get; }
    private int GameId { get; }

    public GameKey(long userId, int gameId)
    {
        UserId = userId;
        GameId = gameId;
    }
}