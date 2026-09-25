using System;
using System.Collections.Generic;

namespace Lab1.Contracts
{
    public static class ContractRegistry
    {
        public const string RentCarKey = "Начать аренду";
        public const string ReturnCarKey = "Завершить аренду";
        public const string TopUpKey = "Пополнить баланс";

        private static readonly IReadOnlyDictionary<string, ContractInfo> _contracts =
            new Dictionary<string, ContractInfo>
            {
                [RentCarKey] = new ContractInfo(
                    Title: "Контракт операции «Начать аренду»",
                    Pre: "Автомобиль доступен && " +
                         "пользователь авторизован && пользователь не имеет активной аренды",
                    Post: "Автомобиль недоступен && пользователь имеет активную аренду",
                    Effects: "Автомобиль становится недоступным; " +
                             "у пользователя появляется активная аренда",
                    Exceptions:
                        "GuardViolationException — если не выполнено предусловие.\n" +
                        "Debug.Assert — если не выполнено постусловие " +
                        "(проверяется только в отладочной сборке).",
                    CorrectExample:
                        "Автомобиль существует, автомобиль доступен, " +
                        "пользователь авторизован, активной аренды нет.\n" +
                        "→ Операция выполняется: аренда оформляется, постусловие выполнено.",
                    IncorrectExample:
                        "Автомобиль существует, но недоступен, " +
                        "пользователь авторизован, активной аренды нет.\n" +
                        "→ Guard.Requires бросает исключение: автомобиль недоступен."),

                [ReturnCarKey] = new ContractInfo(
                    Title: "Контракт операции «Завершить аренду»",
                    Pre: "Пользователь авторизован && пользователь имеет активную аренду && " +
                         "расстояние — целое положительное число && тариф больше нуля",
                    Post: "Пользователь не имеет активной аренды",
                    Effects: "Активная аренда закрывается; баланс уменьшается на " +
                             "произведение расстояния и тарифа",
                    Exceptions:
                        "GuardViolationException — если не выполнено предусловие.\n" +
                        "Debug.Assert — если не выполнено постусловие " +
                        "(проверяется только в отладочной сборке).",
                    CorrectExample:
                        "Пользователь авторизован, есть активная аренда, " +
                        "расстояние = 10, тариф = 25, баланс = 1000.\n" +
                        "→ Операция выполняется: аренда закрывается, баланс становится 750.",
                    IncorrectExample:
                        "Пользователь авторизован, активной аренды нет, " +
                        "расстояние = 10, тариф = 25.\n" +
                        "→ Guard.Requires бросает исключение: нет активной аренды."),

                [TopUpKey] = new ContractInfo(
                    Title: "Контракт операции «Пополнить баланс»",
                    Pre: "Пользователь авторизован && сумма пополнения больше нуля",
                    Post: "Баланс увеличился ровно на сумму пополнения",
                    Effects: "Баланс пользователя увеличивается на указанную сумму",
                    Exceptions:
                        "GuardViolationException — если не выполнено предусловие.\n" +
                        "Debug.Assert — если не выполнено постусловие " +
                        "(проверяется только в отладочной сборке).",
                    CorrectExample:
                        "Пользователь авторизован, баланс = 500, сумма пополнения = 1000.\n" +
                        "→ Операция выполняется: баланс становится 1500, постусловие выполнено.",
                    IncorrectExample:
                        "Пользователь не авторизован, сумма пополнения = 1000.\n" +
                        "→ Guard.Requires бросает исключение: пользователь не авторизован."),
            };

        public static ContractInfo Get(string key) =>
            _contracts.TryGetValue(key, out var info)
                ? info
                : throw new KeyNotFoundException($"Контракт '{key}' не найден.");

        public static bool TryGet(string key, out ContractInfo info) =>
            _contracts.TryGetValue(key, out info!);
    }
}