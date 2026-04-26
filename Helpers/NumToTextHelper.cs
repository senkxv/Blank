using System;
using System.Text;

namespace Blank.Helpers
{
    public static class NumToTextHelper
    {
        /// <summary>
        /// Преобразует сумму в рубли и копейки прописью
        /// </summary>
        /// <param name="sum">Сумма в рублях</param>
        /// <returns>Строка с суммой прописью</returns>
        public static string SumInWords(decimal sum)
        {
            if (sum == 0) return "Ноль рублей 00 копеек";

            long rubles = (long)Math.Floor(Math.Abs(sum));
            int kopeks = (int)(Math.Abs(sum) * 100) % 100;

            string rublesStr = RublesInWords(rubles);
            string kopeksStr = KopeksInWords(kopeks);

            // Добавляем знак минус для отрицательных сумм
            if (sum < 0)
                return $"Минус {rublesStr} {kopeksStr}";

            return $"{rublesStr} {kopeksStr}";
        }

        /// <summary>
        /// Преобразует рубли в пропись
        /// </summary>
        private static string RublesInWords(long number)
        {
            if (number == 0) return "ноль";

            string[] units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] unitsFemale = { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };

            var result = new StringBuilder();
            long num = number;

            // Миллиарды
            if (num >= 1000000000)
            {
                long billions = num / 1000000000;
                result.Append($"{GetHundredsTensUnits(billions, false)} миллиард{GetPluralEnding(billions, "", "а", "ов")} ");
                num %= 1000000000;
            }

            // Миллионы
            if (num >= 1000000)
            {
                long millions = num / 1000000;
                result.Append($"{GetHundredsTensUnits(millions, false)} миллион{GetPluralEnding(millions, "", "а", "ов")} ");
                num %= 1000000;
            }

            // Тысячи
            if (num >= 1000)
            {
                long thousands = num / 1000;
                result.Append($"{GetHundredsTensUnits(thousands, true)} тысяч{GetPluralEndingForThousands(thousands)} ");
                num %= 1000;
            }

            // Рубли (единицы)
            result.Append(GetHundredsTensUnits(num, false));

            // Склонение слова "рубль"
            result.Append(GetRublesEnding(num));

            return result.ToString().Trim();
        }

        /// <summary>
        /// Преобразует копейки в пропись
        /// </summary>
        private static string KopeksInWords(int kopeks)
        {
            string kopeksStr = kopeks.ToString("00");
            string ending = GetKopeksEnding(kopeks);

            return $"{kopeksStr} {ending}";
        }

        /// <summary>
        /// Получает сотни, десятки и единицы числа
        /// </summary>
        private static string GetHundredsTensUnits(long number, bool isFemale)
        {
            string[] units = isFemale
                ? new[] { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" }
                : new[] { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };

            var result = new StringBuilder();
            long num = number;

            int h = (int)(num / 100);
            num %= 100;

            if (h > 0)
                result.Append(hundreds[h] + " ");

            if (num >= 20)
            {
                int t = (int)(num / 10);
                result.Append(tens[t] + " ");
                num %= 10;
            }
            else if (num >= 10)
            {
                result.Append(teens[num - 10] + " ");
                num = 0;
            }

            if (num > 0)
                result.Append(units[num] + " ");

            return result.ToString().Trim();
        }

        /// <summary>
        /// Возвращает окончание для слова "рубль"
        /// </summary>
        private static string GetRublesEnding(long number)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return " рублей";

            switch (lastDigit)
            {
                case 1: return " рубль";
                case 2:
                case 3:
                case 4: return " рубля";
                default: return " рублей";
            }
        }

        /// <summary>
        /// Возвращает окончание для слова "копейка"
        /// </summary>
        private static string GetKopeksEnding(int number)
        {
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "копеек";

            switch (lastDigit)
            {
                case 1: return "копейка";
                case 2:
                case 3:
                case 4: return "копейки";
                default: return "копеек";
            }
        }

        /// <summary>
        /// Возвращает окончание для тысяч
        /// </summary>
        private static string GetPluralEndingForThousands(long number)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "";

            switch (lastDigit)
            {
                case 1: return "а";
                case 2:
                case 3:
                case 4: return "и";
                default: return "";
            }
        }

        /// <summary>
        /// Возвращает окончание для существительных (миллион, миллиард и т.д.)
        /// </summary>
        private static string GetPluralEnding(long number, string singular, string dual, string plural)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return plural;

            switch (lastDigit)
            {
                case 1: return singular;
                case 2:
                case 3:
                case 4: return dual;
                default: return plural;
            }
        }

        /// <summary>
        /// Преобразует вес в килограммы и граммы прописью
        /// </summary>
        /// <param name="weight">Вес в килограммах</param>
        /// <returns>Строка с весом прописью</returns>
        public static string WeightInWords(decimal weight)
        {
            if (weight == 0) return "ноль килограммов";

            long kg = (long)Math.Floor(Math.Abs(weight));
            int grams = (int)((Math.Abs(weight) - kg) * 1000);

            string result = "";

            if (kg > 0)
            {
                result += RublesInWords(kg);
                result += GetKilogramsEnding(kg);
            }

            if (grams > 0)
            {
                if (kg > 0) result += " ";
                result += $"{grams} {GetGramsEnding(grams)}";
            }

            if (weight < 0)
                result = $"Минус {result}";

            return result;
        }

        /// <summary>
        /// Возвращает окончание для слова "килограмм"
        /// </summary>
        private static string GetKilogramsEnding(long number)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return " килограммов";

            switch (lastDigit)
            {
                case 1: return " килограмм";
                case 2:
                case 3:
                case 4: return " килограмма";
                default: return " килограммов";
            }
        }

        /// <summary>
        /// Возвращает окончание для слова "грамм"
        /// </summary>
        private static string GetGramsEnding(int number)
        {
            int lastDigit = number % 10;
            int lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "граммов";

            switch (lastDigit)
            {
                case 1: return "грамм";
                case 2:
                case 3:
                case 4: return "грамма";
                default: return "граммов";
            }
        }

        /// <summary>
        /// Преобразует количество мест прописью
        /// </summary>
        /// <param name="packages">Количество мест</param>
        /// <returns>Строка с количеством мест прописью</returns>
        public static string PackagesInWords(int packages)
        {
            if (packages == 0) return "ноль мест";

            long absPackages = Math.Abs((long)packages);
            string result = RublesInWords(absPackages);

            long lastDigit = absPackages % 10;
            long lastTwoDigits = absPackages % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                result += " мест";
            else
            {
                switch (lastDigit)
                {
                    case 1: result += " место"; break;
                    case 2:
                    case 3:
                    case 4: result += " места"; break;
                    default: result += " мест"; break;
                }
            }

            if (packages < 0)
                result = $"Минус {result}";

            return result;
        }
    }
}