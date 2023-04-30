using Dapper;
using MineSweeperBot.Models;
using Npgsql;

namespace MineSweeperBot.Repositories;

public static class GameRepository
{
    public static async Task<List<Game>> GetGamesMonkey(int top)
    {
        await using var db = new NpgsqlConnection(Config.SqlConnectionString);
        var sql = $"select * from games where difficulty = 'Монки-мэн' order by score limit {top}";
        return (List<Game>)await db.QueryAsync<Game>(sql);
    }
    
    public static async Task<List<Game>> GetGamesJoski(int top)
    {
        await using var db = new NpgsqlConnection(Config.SqlConnectionString);
        var sql = $"select * from games where difficulty = 'Жоский чел' order by score limit {top}";
        return (List<Game>)await db.QueryAsync<Game>(sql);
    }

    public static async Task AddGame(Game game)
    {
        await using var db = new NpgsqlConnection(Config.SqlConnectionString);
        var sql = $"insert into games (username, difficulty, score) values (@username, @difficulty, @score)";
        await db.ExecuteAsync(sql, game);
    }
}