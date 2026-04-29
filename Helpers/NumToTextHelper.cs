using System;
using System.Text;

namespace Blank.Helpers
{
    public static class NumToTextHelper
    {
        public static string SumInWords(decimal sum)
        {
            if (sum == 0) return "Ноль рублей ноль копеек";

            long rubles = (long)Math.Floor(Math.Abs(sum));
            int kopeks = (int)Math.Round((Math.Abs(sum) - rubles) * 100);

            if (rubles == 0 && kopeks == 0) return "Ноль рублей ноль копеек";

            string rublesStr = rubles == 0 ? "ноль" : RublesInWords(rubles);
            string kopeksStr = GetKopeksInWords(kopeks);

            if (sum < 0)
                return $"Минус {rublesStr} рубль {kopeksStr}";

            return $"{rublesStr} рубль {kopeksStr}";
        }

        private static string GetKopeksInWords(int kopeks)
        {
            if (kopeks == 0) return "ноль копеек";

            string kopeksWord = NumberToWords(kopeks);
            return $"{kopeksWord} {GetKopeksEnding(kopeks)}";
        }

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

            if (num >= 1000000000)
            {
                long billions = num / 1000000000;
                result.Append($"{GetHundredsTensUnits(billions, false)} миллиард{GetPluralEnding(billions, "", "а", "ов")} ");
                num %= 1000000000;
            }

            if (num >= 1000000)
            {
                long millions = num / 1000000;
                result.Append($"{GetHundredsTensUnits(millions, false)} миллион{GetPluralEnding(millions, "", "а", "ов")} ");
                num %= 1000000;
            }

            if (num >= 1000)
            {
                long thousands = num / 1000;
                result.Append($"{GetHundredsTensUnits(thousands, true)} {GetThousandsWord(thousands)} ");
                num %= 1000;
            }

            if (num > 0)
            {
                result.Append(GetHundredsTensUnits(num, false));
            }
            else if (number < 1000 && number > 0)
            {
                result.Append(GetHundredsTensUnits(num, false));
            }

            return result.ToString().Trim();
        }

        private static string NumberToWords(long number)
        {
            if (number == 0) return "ноль";

            string[] units = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };

            var result = new StringBuilder();
            long num = number;

            int h = (int)(num / 100);
            num %= 100;

            if (h > 0)
            {
                result.Append(hundreds[h]);
                if (num > 0) result.Append(" ");
            }

            if (num >= 20)
            {
                int t = (int)(num / 10);
                result.Append(tens[t]);
                num %= 10;
                if (num > 0) result.Append(" ");
            }
            else if (num >= 10)
            {
                result.Append(teens[num - 10]);
                num = 0;
            }

            if (num > 0)
            {
                result.Append(units[num]);
            }

            return result.ToString().Trim();
        }

        private static string GetThousandsWord(long number)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return "тысяч";

            switch (lastDigit)
            {
                case 1: return "тысяча";
                case 2:
                case 3:
                case 4: return "тысячи";
                default: return "тысяч";
            }
        }

        private static string GetHundredsTensUnits(long number, bool isFemale)
        {
            if (number == 0) return "";

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
            {
                result.Append(hundreds[h]);
                if (num > 0) result.Append(" ");
            }

            if (num >= 20)
            {
                int t = (int)(num / 10);
                result.Append(tens[t]);
                num %= 10;
                if (num > 0) result.Append(" ");
            }
            else if (num >= 10)
            {
                result.Append(teens[num - 10]);
                num = 0;
            }

            if (num > 0)
            {
                result.Append(units[num]);
            }

            return result.ToString().Trim();
        }

        private static string GetRublesEnding(long number)
        {
            if (number == 0) return " рублей";

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

        public static string WeightInWords(decimal weight)
        {
            if (weight == 0) return "ноль килограммов ноль граммов";

            long kg = (long)Math.Floor(Math.Abs(weight));
            int grams = (int)Math.Round((Math.Abs(weight) - kg) * 1000);

            string result = "";

            if (kg > 0)
            {
                result += NumberToWords(kg);
                result += GetKilogramsEnding(kg);
            }
            else
            {
                result += "ноль килограммов";
            }

            if (grams > 0)
            {
                if (kg > 0) result += " ";
                result += NumberToWords(grams);
                result += GetGramsEnding(grams);
            }
            else
            {
                result += " ноль граммов";
            }

            if (weight < 0)
                result = $"Минус {result}";

            return result.Trim();
        }

        private static string GetKilogramsEnding(long number)
        {
            if (number == 0) return " килограммов";

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

        private static string GetGramsEnding(int number)
        {
            if (number == 0) return "граммов";

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

        public static string PackagesInWords(int packages)
        {
            if (packages == 0) return "ноль мест";

            long absPackages = Math.Abs((long)packages);
            string result = NumberToWords(absPackages);
            result += GetPackagesEnding(absPackages);

            if (packages < 0)
                result = $"Минус {result}";

            return result;
        }

        private static string GetPackagesEnding(long number)
        {
            long lastDigit = number % 10;
            long lastTwoDigits = number % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 19)
                return " мест";

            switch (lastDigit)
            {
                case 1: return " место";
                case 2:
                case 3:
                case 4: return " места";
                default: return " мест";
            }
        }
    }
}