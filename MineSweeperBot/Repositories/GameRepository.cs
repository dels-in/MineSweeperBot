using Dapper;
using MineSweeperBot.Models;
using Npgsql;

namespace MineSweeperBot.Repositories;

public static class GameRepository
{
    public static async Task<List<Game>> GetGames(int top, bool monkey)
    {
        await using var db = new NpgsqlConnection(Config.SqlConnectionString);
        if (monkey)
        {
            var sql = $"select * from gamesmonkey order by time limit {top}";
            return (List<Game>)await db.QueryAsync<Game>(sql);
        }
        else
        {
            var sql = $"select * from gamesjoski order by time limit {top}";
            return (List<Game>)await db.QueryAsync<Game>(sql);
        }
    }

    public static async Task AddGame(Game game, bool monkey)
    {
        await using var db = new NpgsqlConnection(Config.SqlConnectionString);
        if (monkey)
        {
            var sql = $"insert into gamesmonkey (username, time) values (@username, @time)";
            await db.ExecuteAsync(sql, game);
        }
        else
        {
            var sql = $"insert into gamesjoski (username, time) values (@username, @time)";
            await db.ExecuteAsync(sql, game);
        }
    }
}