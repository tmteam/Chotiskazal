using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.ChatFlows
{
    class GraphsStatsFlow
    {
        private readonly Chat _chat;
        private readonly NewWordsService _wordsService;

        public GraphsStatsFlow(Chat chat, NewWordsService wordsService)
        {
            _chat = chat;
            _wordsService = wordsService;
        }
        public async Task Enter()
        {
            var allWords = _wordsService.GetAll();

            /*
             //todo Histogram output

            var historgramMessage = new StringBuilder();
            historgramMessage.Append("```\r\n");
            RenderKnowledgeHistogram(allWords,historgramMessage);
            historgramMessage.Append("```\r\n");
            await _chat.SendMessage(historgramMessage.ToString());
            historgramMessage.Clear();

            historgramMessage.Append("```\r\n");
            RenderAddingTimeLine(allWords,historgramMessage);
            historgramMessage.Append("```\r\n");
            await _chat.SendMessage(historgramMessage.ToString());
            historgramMessage.Clear();

            historgramMessage.Append("```\r\n");
            RenderExamsTimeLine(_wordsService.GetAllExams(),historgramMessage);
            historgramMessage.Append("```\r\n");
            await _chat.SendMessage(historgramMessage.ToString());
            */
            
            var sb = new StringBuilder();
            
            sb.AppendLine($"Context phrases count = {_wordsService.GetContextPhraseCount()}");
            sb.AppendLine($"Words count = {allWords.Count(w=>!w.OriginWord.Contains(' '))}");
            sb.AppendLine($"Words and sentences count = {allWords.Length}");


            var groups = allWords
                .GroupBy(s => s.State)
                .OrderBy(s => (int)s.Key)
                .Select(s => new { state = s.Key, count = s.Count() });

            var doneCount = allWords.Count(a => a.PassedScore >= PairModel.MaxExamScore);

            sb.AppendLine($"Done: {doneCount} words  ({(doneCount * 100 / allWords.Length)}%)");
            sb.AppendLine($"Unknown: {allWords.Length - doneCount} words");
            sb.AppendLine();
            var learningRate = GetLearningRate(allWords);

            Console.WriteLine("Score is "+ learningRate);
            if (learningRate<100)
                sb.AppendLine("You have to add more words!");
            else if (learningRate < 200)
                sb.AppendLine("It's time to add new words!");
            else if (learningRate <300)
                sb.AppendLine("Zen!");
            else if (learningRate < 400)
                sb.AppendLine("Let's do some exams");
            else
            {
                sb.AppendLine("Exams exams exams!");
                sb.AppendLine($"You have to make at least {(learningRate-300)/10} more exams");
            }
            var __ = _chat.SendMessage(sb.ToString());
        }

        private static void RenderKnowledgeHistogram(PairModel[] allWords, StringBuilder stringBuilder)
        {
            var length = 19;
            var wordHystogramm = new int[length];

            int maxCount = 0;
            foreach (var pairModel in allWords)
            {
                var score = pairModel.PassedScore;
                if (score >= wordHystogramm.Length)
                    score = wordHystogramm.Length - 1;
                wordHystogramm[score]++;
                maxCount = Math.Max(wordHystogramm[score], maxCount);
            }

            stringBuilder.Append("     Knowledge histogram (v: words amount, h: knowledge)\r\n");
            stringBuilder.Append("  ");
            //Console.ForegroundColor = ConsoleColor.Yellow;

            for (int row = 0; row < wordHystogramm.Length; row++)
            {
                stringBuilder.Append("____");
            }

            stringBuilder.Append("\r\n");

            int height = 15;
            for (int line = 0; line < height; line++)
            {
                stringBuilder.Append(" |");

                for (int row = 0; row < wordHystogramm.Length; row++)
                {
                    var rowHeight = Math.Ceiling(((height * wordHystogramm[row]) / (double) maxCount));
                    if (rowHeight >= height - line)
                        stringBuilder.Append("|_| ");
                    else
                        stringBuilder.Append("    ");
                }

                stringBuilder.Append("|\r\n");
            }

            stringBuilder.Append(" |");
            for (int row = 0; row < wordHystogramm.Length; row++)
            {
                stringBuilder.Append("____");
            }

            stringBuilder.Append("|\r\n");
            //Console.ResetColor();

        }

        private static int GetLearningRate(PairModel[] allModels)
        {

            //PairModel.MaxExamScore+2, a.AggedScore 
            double sum= 0;
            foreach (var pair in allModels)
            {
                var hiLim = PairModel.MaxExamScore + 2;
                if (pair.PassedScore < hiLim)
                    sum += pair.PassedScore;
                else
                {
                    sum+= Math.Min(hiLim, Math.Max(pair.AggedScore, hiLim - 1));
                }
            }

            var count = allModels.Count();
            return (int) (count * PairModel.MaxExamScore -sum);
        }

        private static void RenderAddingTimeLine(PairModel[] allWords, StringBuilder stringBuilder)
        {
            var wordTimeline = new int[21];

            int maxCount = 0;
            foreach (var pairModel in allWords)
            {
                var score = (int)(DateTime.Now.Date - pairModel.Created.Date).TotalDays +1;
                if(score>wordTimeline.Length || score<0)
                    continue;
                wordTimeline[^score]++;
                maxCount = Math.Max(wordTimeline[^score], maxCount);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            stringBuilder.Append("     Add History (v: words amount, h: days)\r\n");
            stringBuilder.Append("  ");

            for (int row = 0; row < wordTimeline.Length; row++)
            {
                stringBuilder.Append("____");
            }

            stringBuilder.Append("\r\n");

            int height = 15;
            for (int line = 0; line < height; line++)
            {
                stringBuilder.Append(" |");

                for (int row = 0; row < wordTimeline.Length; row++)
                {
                    var rowHeight = Math.Round(((height * wordTimeline[row]) / (double)maxCount));
                    if (rowHeight >= height - line)
                        stringBuilder.Append("|_| ");
                    else
                        stringBuilder.Append("    ");
                }

                stringBuilder.Append("|\r\n");
            }

            stringBuilder.Append(" |");
            for (int row = 0; row < wordTimeline.Length; row++)
            {
                stringBuilder.Append("____");
            }

            stringBuilder.Append("|\r\n");
            //Console.ResetColor();

        }
        private static void RenderExamsTimeLine(Exam[] exams, StringBuilder stringBuilder)
        {
            var wordTimeline = new int[21];
            int maxCount = 0;
            //Console.ForegroundColor = ConsoleColor.DarkRed;

            foreach (var pairModel in exams)
            {
                var score = (int)(DateTime.Now.Date - pairModel.Started.Date).TotalDays + 1;
                if (score > wordTimeline.Length || score < 0)
                    continue;
                wordTimeline[^score]++;
                maxCount = Math.Max(wordTimeline[^score], maxCount);
            }
           
            int height = 15;
            for (int line = 0; line < height; line++)
            {
                stringBuilder.Append(" |");

                for (int row = 0; row < wordTimeline.Length; row++)
                {
                    var rowHeight = Math.Ceiling(((height * wordTimeline[row]) / (double)maxCount));
                    if (rowHeight >= height - line)
                        stringBuilder.Append("|_| ");
                    else
                        stringBuilder.Append("    ");
                }

                stringBuilder.Append("|\r\n");
            }

            stringBuilder.Append(" |");
            for (int row = 0; row < wordTimeline.Length; row++)
            {
                stringBuilder.Append("____");
            }

            stringBuilder.Append("|\r\n");
            stringBuilder.Append("     Exams History (v: exams amount, h: days)\r\n");
            stringBuilder.Append("  ");

            //Console.ResetColor();

        }
    }
}
