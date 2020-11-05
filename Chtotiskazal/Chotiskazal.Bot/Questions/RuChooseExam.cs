using System.Linq;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic;
using Dic.Logic.DAL;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot.Questions
{
    public class RuChooseExam: IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "RuChoose";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            var variants = examList.Randomize().Select(e => e.OriginWord).ToArray();

            var msg = $"=====>   {word.Translation}    <=====\r\nChoose the translation";
            await chat.SendMessage(msg, InlineButtons.CreateVariants(variants));
            
            var choice = await chat.TryWaitInlineIntKeyboardInput();
            if (choice == null)
                return ExamResult.Retry;
            
            if (variants[choice.Value] == word.OriginWord)
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }
            service.RegistrateFailure(word);

            return ExamResult.Failed;
        }
    }
}