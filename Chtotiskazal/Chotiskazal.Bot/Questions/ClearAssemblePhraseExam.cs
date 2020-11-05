using System;
using System.Linq;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class ClearAssemblePhraseExam : IExam
    {
        public bool NeedClearScreen => true;

        public string Name => "Assemble phrase";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList) 
        {
            if (!word.Phrases.Any())
                return ExamResult.Impossible;

            var targetPhrase = word.Phrases.GetRandomItem();

            string shuffled;
            while (true)
            {
                var split = 
                    targetPhrase.Origin.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 2)
                    return ExamResult.Impossible;

                shuffled = string.Join(" ", split.Randomize());
                if(shuffled!= targetPhrase.Origin)
                    break;
            }

            await chat.SendMessage("Words in phrase are shuffled. Write them in correct order:\r\n'" +  shuffled+ "'");
            string entry= null;
            while (string.IsNullOrWhiteSpace(entry))
            {
                entry = await chat.WaitUserTextInput();
                entry = entry.Trim();
            }

            if (string.CompareOrdinal(targetPhrase.Origin, entry) == 0)
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }

            await chat.SendMessage($"Original phrase was: '{targetPhrase.Origin}'");
            service.RegistrateFailure(word);
            return ExamResult.Failed;
        }
    }
}