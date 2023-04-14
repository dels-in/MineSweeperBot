using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineSweeperBot;

public class GameBoard
{
    public readonly int Rows;
    public readonly int Columns;
    private List<InlineKeyboardButton[]> _inlineBoard = new();
    public readonly List<bool[,]> Numbers;
    private bool[,] One { get; set; }
    private bool[,] Two { get; set; }
    private bool[,] Three { get; set; }
    private bool[,] Four { get; set; }
    private bool[,] Five { get; set; }
    private bool[,] Six { get; set; }
    private bool[,] Seven { get; set; }
    private bool[,] Eight { get; set; }
    private bool[,] Board { get; }
    public bool[,] Hidden { get; }
    public bool[,] Flagged { get; }

    public GameBoard(int rows, int columns, int mines)
    {
        Rows = rows;
        Columns = columns;
        Board = new bool[Rows, Columns];
        Hidden = new bool[Rows, Columns];
        Flagged = new bool[Rows, Columns];

        Numbers = new List<bool[,]>(new[] { One, Two, Three, Four, Five, Six, Seven, Eight }!);

        for (var j = 0; j < Numbers!.Count; j++)
        {
            for (var i = 0; i < Numbers.Count; i++)
            {
                Numbers[j] = new bool[Rows, Columns];
            }
        }


        for (var i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                Board[i, j] = false;
                Hidden[i, j] = true;
                Flagged[i, j] = false;

                foreach (var number in Numbers)
                {
                    number[i, j] = false;
                }
            }
        }

        var rand = new Random();
        for (var i = 0; i < mines; i++)
        {
            var row = rand.Next(0, Rows - 1);
            var column = rand.Next(0, Columns);
            if (!Board[row, column])
                Board[row, column] = true;
            else
                i--;
        }
    }

    public bool IsMine(int row, int column)
    {
        if (column < 0 || row < 0 || column >= Columns || row >= Rows)
            return false;

        return Board[row, column];
    }

    public int GetNeighborMineCount(int row, int column)
    {
        var count = 0;
        for (var i = row - 1; i <= row + 1; i++)
        {
            for (var j = column - 1; j <= column + 1; j++)
            {
                if (IsMine(i, j))
                    count++;
            }
        }

        return count;
    }

    public bool IsGameWon()
    {
        for (var i = 0; i < Rows - 1; i++)
        {
            for (var j = 0; j < Columns; j++)
            {
                if (Board[i, j] && !Flagged[i, j])
                    return false;
                if (!Board[i, j] && Hidden[i, j])
                    return false;
            }
        }

        return true;
    }

    public InlineKeyboardButton[][] CreateBoard(bool setting)
    {
        _inlineBoard = new List<InlineKeyboardButton[]>();
        for (var row = 0; row < Rows - 1; row++)
        {
            List<InlineKeyboardButton> inlineColumn = new();
            for (var column = 0; column < Columns; column++)
            {
                if (Hidden[row, column] && !Flagged[row, column])
                    inlineColumn.Add(
                        InlineKeyboardButton.WithCallbackData("⬛", $"{row}{column}"));
                if (Hidden[row, column] && Flagged[row, column])
                    inlineColumn.Add(
                        InlineKeyboardButton.WithCallbackData("🚩", $"{row}{column}"));

                switch (Hidden[row, column])
                {
                    case false when Board[row, column]:
                        inlineColumn.Add(
                            InlineKeyboardButton.WithCallbackData("💣", $"{row}{column}"));
                        break;
                    case false:
                        var text = new List<string>(new[]
                            { "1️⃣️", "2️⃣️", "3️⃣️", "4️⃣️", "5️⃣️", "6️⃣️", "7️⃣️", "8️⃣️" });
                        for (var i = 0; i < 7; i++)
                        {
                            if (!Numbers[i][row, column]) continue;
                            inlineColumn.Add(
                                InlineKeyboardButton.WithCallbackData(text[i], callbackData: $"{row}{column}"));
                            goto Found;
                        }

                        inlineColumn.Add(
                            InlineKeyboardButton.WithCallbackData(" ", $"{row}{column}"));
                        Found:
                        break;
                }
            }

            _inlineBoard.Add(inlineColumn.ToArray());
        }

        if (setting)
        {
            _inlineBoard.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(text: " Активно ставишь  🚩 ", callbackData: "flag"),
                InlineKeyboardButton.WithCallbackData(text: "Закончить игру 🧨", callbackData: "exit")
            });
        }
        else
        {
            _inlineBoard.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Поставить флажок ⛳", callbackData: "flag"),
                InlineKeyboardButton.WithCallbackData(text: "Закончить игру 🧨", callbackData: "exit")
            });
        }

        return _inlineBoard.ToArray();
    }
}