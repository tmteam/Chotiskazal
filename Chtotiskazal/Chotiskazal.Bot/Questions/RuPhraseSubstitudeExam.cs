using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class RuPhraseSubstitudeExam : IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "Ru phrase substitude";
        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            if (!word.Phrases.Any())
                return ExamResult.Impossible;

            var phrase = word.Phrases.GetRandomItem();
            
            var replaced = phrase.Translation.Replace(phrase.TranslationWord, "...");
            if (replaced == phrase.Translation)
                return ExamResult.Impossible;

            var sb = new StringBuilder();
            
            sb.AppendLine($"\"{phrase.Origin}\"");
            sb.AppendLine($" translated as ");
            sb.AppendLine($"\"{replaced}\"");
            sb.AppendLine();
            sb.AppendLine($"Enter missing word: ");
            
            while (true)
            {
                var enter = await chat.WaitUserTextInput();
                if (string.IsNullOrWhiteSpace(enter))
                    continue;
                if (string.CompareOrdinal(phrase.TranslationWord.ToLower().Trim(), enter.ToLower().Trim()) == 0)
                {
                    service.RegistrateSuccess(word);
                    return ExamResult.Passed;
                }

                await chat.SendMessage($"Origin phrase was \"{phrase.Translation}\"");
                service.RegistrateFailure(word);
                return ExamResult.Failed;
            }
        }
    }
}