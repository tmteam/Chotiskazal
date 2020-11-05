using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class EngChooseWordInPhraseExam : IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "Eng Choose word in phrase";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            if (!word.Phrases.Any())
                return ExamResult.Impossible;
            
            var phrase = word.Phrases.GetRandomItem();

            var replaced = phrase.Origin.Replace(phrase.OriginWord, "...");
            if (replaced == phrase.Origin)
                return ExamResult.Impossible;

            var sb = new StringBuilder();
            sb.AppendLine($"\"{phrase.Translation}\"");
            sb.AppendLine();
            sb.AppendLine($" translated as ");
            sb.AppendLine();
            sb.AppendLine($"\"{replaced}\"");
            sb.AppendLine($"Choose missing word...");

            var variants = examList.Randomize().Select(e => e.OriginWord).ToArray();
            var _ =chat.SendMessage(sb.ToString(), Buttons.CreateVariants(variants));

            var choice = await chat.TryWaitInlineIntKeyboardInput();
            if (choice == null)
                return ExamResult.Retry;

            if (variants[choice.Value] == word.OriginWord)
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }

            await chat.SendMessage($"Origin was: \"{phrase.Origin}\"");
            service.RegistrateFailure(word);
            return ExamResult.Failed;
        }
    }
}