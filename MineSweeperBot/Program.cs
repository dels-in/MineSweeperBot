﻿using System.Diagnostics;
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
    { userFolder, "RiderProjects", "MineSweeperBot" };
var dir = Path.Combine(currentFolder);
var textFile = Path.Combine(dir, "Token.txt");

var textReader = new StreamReader(textFile);
var token = textReader.ReadLine();
textReader.Close();

Console.WriteLine(textFile);
Console.WriteLine(token);

var botClient = new TelegramBotClient(token!);

var remove = new ReplyKeyboardRemove();
var stopwatch = new Stopwatch();
var games = new Dictionary<long,GameBoard>();
var flag = false;
bool check;

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

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    check = true;
    switch (update.Type)
    {
        case UpdateType.Message:
            await BotOnMessageReceived(bot, update.Message!);
            break;
        case UpdateType.CallbackQuery:
            await OnAnswer(bot, update.CallbackQuery!, update.CallbackQuery!.From.Id);
            break;
        case UpdateType.Unknown:
        case UpdateType.InlineQuery:
        case UpdateType.ChosenInlineResult:
        case UpdateType.EditedMessage:
        case UpdateType.ChannelPost:
        case UpdateType.EditedChannelPost:
        case UpdateType.ShippingQuery:
        case UpdateType.PreCheckoutQuery:
        case UpdateType.Poll:
        case UpdateType.PollAnswer:
        case UpdateType.MyChatMember:
        case UpdateType.ChatMember:
        case UpdateType.ChatJoinRequest:
        default:
            await bot.SendTextMessageAsync
            (
                chatId: update.Message!.Chat.Id,
                text: "Такое остается в игноре"
            );
            break;
    }
}

async Task BotOnMessageReceived(ITelegramBotClient bot, Message message)
{
    if (message.Type != MessageType.Text)
    {
        await bot.SendTextMessageAsync
        (
            chatId: message.Chat.Id,
            text: "Такое остается в игноре"
        );

        return;
    }

    var user = message.From;
    var chatId = message.Chat.Id;

    Console.WriteLine($"Receive message type: {message.Type}");
    Console.WriteLine($"Received a '{message.Text}' message in chat {chatId} from @{user!.Username}");

    var action = message.Text!;
    switch (action)
    {
        case "/start":
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Привет, это игра \"Сапёр\". \nПеред игрой ознакомься с правилами.",
                replyMarkup: replyKeyboardHello
            );
            break;
        case "Начать игру":
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Выбери уровень сложности",
                replyMarkup: replyKeyboardLevel
            );
            break;
        case "Правила":
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Правила просты, я или ты... \n" +
                      "Кароче, твоя задача открыть все ячейки, не содержащие мин, а сами мины пометить флажками, боец." +
                      "\nПосле выполнения задачи ОБЯЗАТЕЛЬНО нажать на кнопку \"Поставить флажок\", это приказ." +
                      "\nP.S. Нажатие на поле предыдущей игры приведет к падению бота."
            );
            break;
        case "Монки-мэн":
            gameBoard = new GameBoard(6, 5, 5);
            var monkey = NewGame(user.Id, gameBoard);
            var inlineKeyboardMonkey = new InlineKeyboardMarkup(monkey.CreateBoard(false));
            stopwatch.Start();
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Всем монкам - поехали!",
                replyMarkup: remove
            );
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "И помни: главное - участие",
                replyMarkup: inlineKeyboardMonkey
            );
            break;
        case "Жоский чел":
            gameBoard = new GameBoard(10, 8, 12);
            var joski = NewGame(user.Id, gameBoard);
            var inlineKeyboardJoski = new InlineKeyboardMarkup(joski.CreateBoard(false));
            stopwatch.Start();
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "Жоский чел, уверенный. Покажи себя",
                replyMarkup: remove
            );
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "И помни: главное - победа",
                replyMarkup: inlineKeyboardJoski
            );
            break;
        case "Лучшие гвардейцы":
            await bot.SendTextMessageAsync
            (
                chatId: chatId,
                text: "In progress...zZz"
            );
            break;
        default:
            await Echo(bot, message);
            break;
    }
}

async Task OnAnswer(ITelegramBotClient bot, CallbackQuery callbackQuery, long userId)
{
    TryParse(callbackQuery.Data![0].ToString(), out var row);
    TryParse(callbackQuery.Data![1].ToString(), out var colomn);
    var game = games[userId];
    if (game.IsGameWon())
    {
        stopwatch.Stop();
        await bot.AnswerCallbackQueryAsync
        (
            callbackQueryId: callbackQuery.Id,
            text: "Winner, winner chicken dinner!" +
                  $"\n{stopwatch.ElapsedMilliseconds / 1000} секунд. Похвально, гвардеец",
            showAlert: true
        );
        await bot.DeleteMessageAsync
        (
            chatId: userId,
            messageId: callbackQuery.Message!.MessageId
        );
        await bot.SendTextMessageAsync
        (
            chatId: userId,
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
            await bot.EditMessageReplyMarkupAsync
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

            await bot.EditMessageReplyMarkupAsync
            (
                chatId: callbackQuery.From.Id,
                messageId: callbackQuery.Message!.MessageId,
                replyMarkup: new InlineKeyboardMarkup(game.CreateBoard(false))
            );

            Thread.Sleep(2000);
            await bot.DeleteMessageAsync
            (
                chatId: callbackQuery.From.Id,
                messageId: callbackQuery.Message!.MessageId
            );
            await bot.SendTextMessageAsync
            (
                chatId: callbackQuery.From.Id,
                text: "Новый трай, huh?",
                replyMarkup: replyKeyboardHello
            );
            break;
        default:
            await HandleMove(bot, callbackQuery, callbackQuery.From.Id, row, colomn, flag);
            if (check)
            {
                await bot.EditMessageReplyMarkupAsync
                (
                    chatId: callbackQuery.From.Id,
                    messageId: callbackQuery.Message!.MessageId,
                    replyMarkup: new InlineKeyboardMarkup(game.CreateBoard(flag))
                );
            }

            break;
    }
}

async Task HandleMove(ITelegramBotClient bot, CallbackQuery callbackQuery, long userId, int row,
    int colomn, bool flag)
{
    try
    {
        if (!games.ContainsKey(userId))
            throw new Exception("Неверный ID");

        var game = games[userId];

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
            await bot.AnswerCallbackQueryAsync
            (
                callbackQueryId: callbackQuery.Id,
                text: "Погиб :D",
                showAlert: true
            );

            check = false;
            callbackQuery.Data = "exit";
            await OnAnswer(bot, callbackQuery, userId);

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
                        await HandleMove(bot, callbackQuery, userId, i, j, false);
                }
            }
        }

        var list = new List<int>(new[]{1,2,3,4,5,6,7,8});
        foreach (var i in list.Where(i => game.GetNeighborMineCount(row, colomn) == i))
        {
            games[userId].Numbers[i-1][row, colomn] = true;
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
}

GameBoard NewGame(long userId, GameBoard game)
{
    flag = false;
    if (games.ContainsKey(userId))
        games.Remove(userId);
    games.Add(userId, game);
    return game;
}

async Task Echo(ITelegramBotClient bot, Message message)
{
    await bot.SendTextMessageAsync
    (
        chatId: message.Chat.Id,
        text: $"{message.Text}",
        replyToMessageId: message.MessageId
    );
}

Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception,
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