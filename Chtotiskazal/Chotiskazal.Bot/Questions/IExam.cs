using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public interface IExam
    {
        bool NeedClearScreen { get; }
        string Name { get; }
        Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList);
    }

    public enum ExamResult
    {
        Passed,
        Failed,
        Retry,
        Impossible,
        Exit
    }
}