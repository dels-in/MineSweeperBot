namespace MineSweeperBot;

public class GameKey
{
    public long UserId { get; set; }
    public int GameId { get; set; }

    public GameKey(long userId, int gameId)
    {
        UserId = userId;
        GameId = gameId;
    }
}