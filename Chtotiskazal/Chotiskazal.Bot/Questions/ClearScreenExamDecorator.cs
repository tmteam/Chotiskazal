using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class ClearScreenExamDecorator: IExam
    {
        public bool NeedClearScreen => true;

        private readonly IExam _origin;

        public ClearScreenExamDecorator(IExam origin)
        {
            _origin = origin;
        }

        public string Name => "Clean "+ _origin.Name;
        public Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList) 
            => _origin.Pass(chat, service, word, examList);
    }
}
