using System.ComponentModel;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MineSweeperBot;

public class GameBoard
{
    public int Rows;
    public int Colomns;
    public int Mines;
    private List<InlineKeyboardButton[]> _inlineBoard = new();
    public List<bool[,]> Numbers;

    public bool[,] One { get; set; }
    public bool[,] Two { get; set; }
    public bool[,] Three { get; set; }
    public bool[,] Four { get; set; }
    public bool[,] Five { get; set; }
    public bool[,] Six { get; set; }
    public bool[,] Seven { get; set; }
    public bool[,] Eight { get; set; }
    public bool[,] Board { get; set; }
    public bool[,] Hidden { get; set; }
    public bool[,] Flagged { get; set; }

    public GameBoard(int rows, int colomns, int mines)
    {
        Rows = rows;
        Colomns = colomns;
        Mines = mines;
        Board = new bool[Rows, Colomns];
        Hidden = new bool[Rows, Colomns];
        Flagged = new bool[Rows, Colomns];

        Numbers = new List<bool[,]>(new[] { One, Two, Three, Four, Five, Six, Seven, Eight }!);

        for (var j = 0; j < Numbers.Count; j++)
        {
            for (var i = 0; i < Numbers.Count; i++)
            {
                Numbers[j] = new bool[Rows, Colomns];
            }
        }


        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Colomns; j++)
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

        Random rand = new Random();
        for (var i = 0; i < Mines; i++)
        {
            var row = rand.Next(0, Rows - 1);
            var colomn = rand.Next(0, Colomns);
            if (!Board[row, colomn])
                Board[row, colomn] = true;
            else
                i--;
        }
    }

    public bool IsMine(int row, int colomn)
    {
        if (colomn < 0 || row < 0 || colomn >= Colomns || row >= Rows)
            return false;

        return Board[row, colomn];
    }

    public int GetNeighborMineCount(int row, int colomn)
    {
        var count = 0;
        for (var i = row - 1; i <= row + 1; i++)
        {
            for (var j = colomn - 1; j <= colomn + 1; j++)
            {
                if (IsMine(i, j))
                    count++;
            }
        }

        return count;
    }

    public bool IsGameWon()
    {
        for (var i = 0; i < Rows-1; i++)
        {
            for (var j = 0; j < Colomns; j++)
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
            List<InlineKeyboardButton> inlineColomn = new();
            for (var colomn = 0; colomn < Colomns; colomn++)
            {
                if (Hidden[row, colomn] && !Flagged[row, colomn])
                    inlineColomn.Add(
                        InlineKeyboardButton.WithCallbackData(text: "⬛", callbackData: $"{row}{colomn}"));
                if (Hidden[row, colomn] && Flagged[row, colomn])
                    inlineColomn.Add(
                        InlineKeyboardButton.WithCallbackData(text: "🚩", callbackData: $"{row}{colomn}"));

                switch (Hidden[row, colomn])
                {
                    case false when Board[row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "💣", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[0][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "1️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[1][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "2️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[2][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "3️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[3][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "4️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[4][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "5️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[5][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "6️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[6][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "7️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false when Numbers[7][row, colomn]:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: "8️⃣️", callbackData: $"{row}{colomn}"));
                        break;
                    case false:
                        inlineColomn.Add(
                            InlineKeyboardButton.WithCallbackData(text: " ", callbackData: $"{row}{colomn}"));
                        break;
                }
            }

            _inlineBoard.Add(inlineColomn.ToArray());
        }

        if (setting)
        {
            _inlineBoard.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Ставишь флаг", callbackData: "flag"),
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