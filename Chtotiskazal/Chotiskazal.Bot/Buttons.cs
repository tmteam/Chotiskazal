using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot
{
    public static class Buttons{
        public readonly static InlineKeyboardButton EnterWords = new InlineKeyboardButton{ CallbackData = "~EnterWords", Text = "Enter words"};
        public readonly static InlineKeyboardButton Exam = new InlineKeyboardButton{ CallbackData = "~Exam", Text = "Examination"};
        public readonly static InlineKeyboardButton Stats = new InlineKeyboardButton{ CallbackData = "~Stats", Text = "Stats"};
        public  static InlineKeyboardButton[] CreateVariants(string[] variants) =>
            variants.Select((v, i) => new InlineKeyboardButton
            {
                CallbackData = i.ToString(),
                Text = v
            }).ToArray();

    }
}