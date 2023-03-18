using System.Diagnostics;
using MineSweeperBot;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Int32;

using CancellationTokenSource cts = new();

var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);   
var currentFolder = new[]
    { userFolder, "RiderProjects", "MineSweeperBot"};
var dir = Path.Combine(currentFolder);
var textFile = Path.Combine(dir, "Token.txt");

var textReader = new StreamReader(textFile);
var token = textReader.ReadLine();
textReader.Close();

Console.WriteLine(textFile);
Console.WriteLine(token);

var botClient = new TelegramBotClient(token!);

var gameId = 0;
var remove = new ReplyKeyboardRemove();
var games = new Dictionary<GameKey, GameBoard>();
var stopwatch = new Stopwatch();
var flag = false;
bool check;

GameKey gameKey = null!;
GameBoard gameBoard;

ReplyKeyboardMarkup replyKeyboardHello = new(new[]
{
    new KeyboardButton[] { "Начать игру", "Правила", "Лучшие гвардейцы" },
})
{
    ResizeKeyboard = true,
    OneTimeKeyboard = true
};

ReplyKeyboardMarkup replyKeyboardLevel = new(new[]
{
    new KeyboardButton[] { "Монки-мэн", "Жоский чел" },
})
{
    IsPersistent = true,
    ResizeKeyboard = true,
    OneTimeKeyboard = true
};

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    check = true;
    switch (update.Type)
    {
        case UpdateType.Message:
            await BotOnMessageReceived(botClient, update.Message!);
            break;
        case UpdateType.CallbackQuery:
            await OnAnswer(botClient, update.CallbackQuery!, update.CallbackQuery!.From.Id);
            break;
        default:
            await botClient.SendTextMessageAsync
            (
                chatId: update.Message!.Chat.Id,
                text: "Такое остается в игноре"
            );
            break;
    }
}

async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
{
    if (message.Type != MessageType.Text)
        return;

    var user = message.From;
    var chatId = message.Chat.Id;
    GameBoard game;

    Console.WriteLine($"Receive message type: {message.Type}");
    Console.WriteLine($"Received a '{message.Text}' message in chat {chatId} from @{user!.Username}");

    var action = message.Text!;
    switch (action)
    {
        case "/start":
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Привет, это игра \"Сапёр\". \nПеред игрой ознакомься с правилами.",
                replyMarkup: replyKeyboardHello
            );
            break;
        case "Начать игру":
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Выбери уровень сложности",
                replyMarkup: replyKeyboardLevel
            );
            break;
        case "Правила":
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Правила просты, я или ты... \n" +
                      "Кароче, твоя задача открыть все ячейки, не содержащие мин, а сами мины пометить флажками, боец." +
                      "\nПосле выполнения задачи ОБЯЗАТЕЛЬНО нажать на кнопку \"Поставить флажок\", это приказ." +
                      "\nP.S. Нажатие на поле предыдущей игры приведет к падению бота."
            );
            break;
        case "Монки-мэн":
            flag = false;
            gameBoard = new GameBoard(6, 5, 5);
            gameKey = NewGame(user.Id, gameBoard);
            game = games[gameKey];
            var inlineKeyboardMonkey = new InlineKeyboardMarkup(game.CreateBoard(false));
            stopwatch.Start();
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Всем монкам - поехали!",
                replyMarkup: remove
            );
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "И помни: главное - участие",
                replyMarkup: inlineKeyboardMonkey
            );
            break;
        case "Жоский чел":
            flag = false;
            gameBoard = new GameBoard(10, 8, 12);
            gameKey = NewGame(user.Id, gameBoard);
            game = games[gameKey];
            var inlineKeyboardJoski = new InlineKeyboardMarkup(game.CreateBoard(false));
            stopwatch.Start();
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Жоский чел, уверенный. Покажи себя",
                replyMarkup: remove
            );
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "И помни: главное - победа",
                replyMarkup: inlineKeyboardJoski
            );
            break;
        case "Лучшие гвардейцы":
            await botClient.SendTextMessageAsync
            (
                chatId: chatId,
                text: "In progress...zZz"
            );
            break;
        default:
            await Echo(botClient, message);
            break;
    }
}

async Task OnAnswer(ITelegramBotClient botClient, CallbackQuery callbackQuery, long userId)
{
    TryParse(callbackQuery.Data![0].ToString(), out var row);
    TryParse(callbackQuery.Data![1].ToString(), out var colomn);
    var game = games[gameKey];
    if (game.IsGameWon())
    {
        stopwatch.Stop();
        await botClient.AnswerCallbackQueryAsync
        (
            callbackQueryId: callbackQuery.Id,
            text: "Winner, winner chicken dinner!" +
                  $"\n{stopwatch.ElapsedMilliseconds / 1000} секунд. Похвально, гвардеец",
            showAlert: true
        );
        await botClient.DeleteMessageAsync
        (
            chatId: callbackQuery.From.Id,
            messageId: callbackQuery.Message!.MessageId
        );
        await botClient.SendTextMessageAsync
        (
            chatId: callbackQuery.From.Id,
            text: "Новый трай, huh?",
            replyMarkup: replyKeyboardHello
        );
        check = false;
        return;
    }

    switch (callbackQuery.Data)
    {
        case "flag":
            flag = !flag;
            await botClient.EditMessageReplyMarkupAsync
            (
                chatId: callbackQuery.From.Id,
                messageId: callbackQuery.Message!.MessageId,
                replyMarkup: new InlineKeyboardMarkup(game.CreateBoard(flag))
            );
            break;
        case "exit":
            for (var i = 0; i < game.Rows - 1; i++)
            {
                for (var j = 0; j < game.Colomns; j++)
                {
                    game.Hidden[i, j] = false;
                }
            }

            await botClient.EditMessageReplyMarkupAsync
            (
                chatId: callbackQuery.From.Id,
                messageId: callbackQuery.Message!.MessageId,
                replyMarkup: new InlineKeyboardMarkup(game.CreateBoard(false))
            );

            Thread.Sleep(2000);
            await botClient.DeleteMessageAsync
            (
                chatId: callbackQuery.From.Id,
                messageId: callbackQuery.Message!.MessageId
            );
            await botClient.SendTextMessageAsync
            (
                chatId: callbackQuery.From.Id,
                text: "Новый трай, huh?",
                replyMarkup: replyKeyboardHello
            );
            break;
        default:
            await HandleMove(botClient, callbackQuery, callbackQuery.From.Id, gameId, row, colomn, flag);
            if (check)
            {
                await botClient.EditMessageReplyMarkupAsync
                (
                    chatId: callbackQuery.From.Id,
                    messageId: callbackQuery.Message!.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(game.CreateBoard(flag))
                );
            }

            break;
    }
}

async Task HandleMove(ITelegramBotClient botClient, CallbackQuery callbackQuery, long userId, int gameId, int row,
    int colomn, bool flag)
{
    try
    {
        if (!games.ContainsKey(gameKey))
            throw new Exception("Неверный ID");

        var game = games[gameKey];

        if (game.Hidden[row, colomn] == false)
        {
            check = false;
            return;
        }

        if (flag)
        {
            game.Hidden[row, colomn] = true;
            game.Flagged[row, colomn] = !game.Flagged[row, colomn];
            return;
        }

        game.Hidden[row, colomn] = false;

        if (game.IsMine(row, colomn) && !game.Flagged[row, colomn])
        {
            stopwatch.Stop();
            await botClient.AnswerCallbackQueryAsync
            (
                callbackQueryId: callbackQuery.Id,
                text: "Погиб :D",
                showAlert: true
            );

            check = false;
            callbackQuery.Data = "exit";
            await OnAnswer(botClient, callbackQuery, userId);

            throw new Exception("Погиб :D");
        }

        if (game.GetNeighborMineCount(row, colomn) == 0)
        {
            for (var i = row - 1; i <= row + 1; i++)
            {
                for (var j = colomn - 1; j <= colomn + 1; j++)
                {
                    if (i == row && j == colomn)
                        continue;

                    if (i >= 0 && j >= 0 && i < game.Rows && j < game.Colomns && game.Hidden[i, j])
                        await HandleMove(botClient, callbackQuery, userId, gameId, i, j, false);
                }
            }
        }

        if (game.GetNeighborMineCount(row, colomn) == 1)
        {
            SetNumber(0, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 2)
        {
            SetNumber(1, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 3)
        {
            SetNumber(2, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 4)
        {
            SetNumber(3, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 5)
        {
            SetNumber(4, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 6)
        {
            SetNumber(5, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 7)
        {
            SetNumber(6, gameKey, row, colomn);
        }

        if (game.GetNeighborMineCount(row, colomn) == 8)
        {
            SetNumber(7, gameKey, row, colomn);
        }

        // switch (game.GetNeighborMineCount(row, colomn))
        // {
        //     case 1:
        //         await SetNumber(1, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 2:
        //         await SetNumber(2, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 3:
        //         await SetNumber(3, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 4:
        //         await SetNumber(4, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 5:
        //         await SetNumber(5, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 6:
        //         await SetNumber(6, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 7:
        //         await SetNumber(7, callbackQuery, gameId, row, colomn);
        //         break;
        //     case 8:
        //         await SetNumber(8, callbackQuery, gameId, row, colomn);
        //         break;
        // }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

void SetNumber(int i, GameKey gameKey, int row, int colomn)
{
    var game = games[gameKey];
    game.Numbers[i][row, colomn] = true;
}

GameKey NewGame(long userId, GameBoard game)
{
    gameId = games.Count;
    //gameKey.UserId = userId;
    gameKey = new GameKey(userId, gameId);
    games.Add(gameKey, game);
    return gameKey;
}

async Task Echo(ITelegramBotClient botClient, Message message)
{
    await botClient.SendTextMessageAsync
    (
        chatId: message.Chat.Id,
        text: $"{message.Text}",
        replyToMessageId: message.MessageId
    );
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
    CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}