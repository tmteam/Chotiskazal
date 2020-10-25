using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot
{
    public static class Buttons{
        public readonly static InlineKeyboardButton EnterWords = new InlineKeyboardButton{ CallbackData = "~EnterWords", Text = "Enter words"};
        public readonly static InlineKeyboardButton Exam = new InlineKeyboardButton{ CallbackData = "~Exam", Text = "Examination"};
        public readonly static InlineKeyboardButton Stats = new InlineKeyboardButton{ CallbackData = "~Stats", Text = "Stats"};
    }
}