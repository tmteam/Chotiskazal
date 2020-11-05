using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot.Questions
{
    public class RuTrustExam : IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "Ru trust";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            var msg = "=====>   " + word.Translation + "    <=====";
            var _ = chat.SendMessage(msg,
                new InlineKeyboardButton()
                {
                    CallbackData = "1",
                    Text = "See the translation"
                });
            await chat.WaitInlineIntKeyboardInput();
            
            _= chat.SendMessage("Translation is \r\n" + word.OriginWord + "\r\n Did you guess?",
                
                new InlineKeyboardButton
                {
                    CallbackData = "1",
                    Text = "Yes"
                },
                new InlineKeyboardButton{
                    CallbackData = "0",
                    Text = "No"
                });
            
            var choice = await chat.WaitInlineIntKeyboardInput();

            if (choice == 1)
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }
            else
            {
                service.RegistrateFailure(word);
                return ExamResult.Failed;
            }
        }
    }
}