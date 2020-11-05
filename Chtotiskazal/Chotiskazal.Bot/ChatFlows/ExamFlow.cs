using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chotiskazal.Bot.Questions;
using Chotiskazal.Logic.DAL;
using Chotiskazal.Logic.Services;
using Dic.Logic;
using Dic.Logic.DAL;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot.ChatFlows
{
    
    public class ExamFlow
    {
        private readonly Chat _chat;
        private readonly NewWordsService _wordsService;

        public ExamFlow(Chat chat , NewWordsService service)
        {
            _chat = chat;
            _wordsService = service;
        }

        public async Task Enter()
        {
            //Randomization and jobs
            if (RandomTools.Rnd.Next() % 30 == 0)
            {
                //Add phrases with mutual words to vocab
                _wordsService.AddMutualPhrasesToVocab(10);
            }
            else
                _wordsService.UpdateAgingAndRandomize(50);

            var sb = new StringBuilder("Examination\r\n");
            var learningWords = _wordsService.GetPairsForLearning(9, 3);
            if (learningWords.Average(w => w.PassedScore) <= 4)
            {
                foreach (var pairModel in learningWords.Randomize())
                {
                    sb.AppendLine($"{pairModel.OriginWord}\t\t:{pairModel.Translation}");
                }
            }

            var startMessageSending = _chat.SendMessage(sb.ToString(), new InlineKeyboardButton {
                CallbackData = "/startExamination", 
                Text = "Start"
            }, new InlineKeyboardButton
            {
                CallbackData = "/start",
                Text= "Cancel",
            });
            
            var examsList = new List<PairModel>(learningWords.Length * 4);
            //Every learning word appears in test from 2 to 4 times

            examsList.AddRange(learningWords.Randomize());
            examsList.AddRange(learningWords.Randomize());
            examsList.AddRange(learningWords.Randomize().Where(w => RandomTools.Rnd.Next() % 2 == 0));
            examsList.AddRange(learningWords.Randomize().Where(w => RandomTools.Rnd.Next() % 2 == 0));

            while (examsList.Count > 32)
            {
                examsList.RemoveAt(examsList.Count - 1);
            }

            var delta = Math.Min(7, (32 - examsList.Count));
            var testWords = new PairModel[0];
            if (delta > 0)
            {
                var randomRate = 8 + RandomTools.Rnd.Next(5);
                testWords = _wordsService.GetPairsForTests(delta, randomRate);
                examsList.AddRange(testWords);
            }

        
            int examsCount = 0;
            int examsPassed = 0;
            await startMessageSending;
            
            DateTime started = DateTime.Now;

            int i = 0;
            ExamResult? lastExamResult = null;
            var userInput = await _chat.WaitInlineKeyboardInput();
            if (userInput != "/startExamination")
                return;
            foreach (var pairModel in examsList)
            {
                var exam = ExamSelector.GetNextExamFor(i < 9, pairModel);
                i++;
                bool retryFlag = false;
                do
                {
                    retryFlag = false;
                    Stopwatch sw = Stopwatch.StartNew();
                    var questionMetric = CreateQuestionMetric(pairModel, exam);

                    var learnList = learningWords;

                    if (!learningWords.Contains(pairModel))
                        learnList = learningWords.Append(pairModel).ToArray();

                    if (exam.NeedClearScreen)
                    {
                        if (lastExamResult == ExamResult.Failed)
                        {
                            await _chat.SendMessage("Don't peek\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.\r\n.");
                        }

                        if (lastExamResult != ExamResult.Impossible)
                        {
                            Console.Clear();
                            if (lastExamResult == ExamResult.Passed)
                                await WritePassed();
                        }
                    }

                    var result = await exam.Pass(_chat, _wordsService, pairModel, learnList);

                    sw.Stop();
                    questionMetric.ElaspedMs = (int) sw.ElapsedMilliseconds;
                    switch (result)
                    {
                        case ExamResult.Impossible:
                            exam = ExamSelector.GetNextExamFor(i == 0, pairModel);
                            retryFlag = true;
                            break;
                        case ExamResult.Passed:
                            await WritePassed();
                            _wordsService.SaveQuestionMetrics(questionMetric);
                            examsCount++;
                            examsPassed++;
                            break;
                        case ExamResult.Failed:
                            await WriteFailed();
                            questionMetric.Result = 0;
                            _wordsService.SaveQuestionMetrics(questionMetric);
                            examsCount++;
                            break;
                        case ExamResult.Retry:
                            retryFlag = true;
                            Console.WriteLine();
                            Console.WriteLine();
                            break;
                        case ExamResult.Exit: return;
                    }
                    lastExamResult = result;

                } while (retryFlag);


                _wordsService.RegistrateExam(started, examsCount, examsPassed);

            }
            var doneMessage = new StringBuilder($"Test done:  {examsPassed}/{examsCount}\r\n");
            foreach (var pairModel in learningWords.Concat(testWords))
            {
                doneMessage.Append(pairModel.OriginWord + " - " + pairModel.Translation + "  (" + pairModel.PassedScore +
                                  ")\r\n");
            }
            await _chat.SendMessage(doneMessage.ToString());
        }

        private Task WriteFailed() => _chat.SendMessage("[failed]");
        private Task WritePassed() => _chat.SendMessage("[PASSED]");
        private static QuestionMetric CreateQuestionMetric(PairModel pairModel, IExam exam)
        {
            var questionMetric = new QuestionMetric
            {
                AggregateScoreBefore = pairModel.AggregateScore,
                WordId = pairModel.Id,
                Created = DateTime.Now,
                ExamsPassed = pairModel.Examed,
                PassedScoreBefore = pairModel.PassedScore,
                PhrasesCount = pairModel.Phrases?.Count ?? 0,
                PreviousExam = pairModel.LastExam,
                Type = exam.Name,
                WordAdded = pairModel.Created
            };
            return questionMetric;
        }
    }
}
