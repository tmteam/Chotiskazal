﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class RuWriteExam : IExam
    {
        public bool NeedClearScreen => false;
        public string Name => "Eng Write";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            var words = word.OriginWord.Split(',').Select(s => s.Trim());
            var minCount = words.Min(t => t.Count(c => c == ' '));
            if (minCount > 0 && word.PassedScore < minCount * 4)
                return ExamResult.Impossible;

            await chat.SendMessage($"=====>   {word.Translation}    <=====\r\nWrite the translation... ");
            var userEntry = await chat.WaitUserTextInput();
            if (string.IsNullOrEmpty(userEntry))
                return ExamResult.Retry;

            if (words.Any(t => string.Compare(userEntry, t, StringComparison.OrdinalIgnoreCase) == 0))
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }
            else
            {
                //search for other translation
                var translationCandidate = service.Get(userEntry.ToLower());
                if (translationCandidate != null)
                {

                    if (translationCandidate.GetTranslations().Any(t1=> word.GetTranslations().Any(t2=> string.CompareOrdinal(t1.Trim(), t2.Trim())==0)))
                    {
                        //translation is correct, but for other word
                        await chat.SendMessage($"the translation was correct, but the question was about the word '{word.OriginWord}'\r\nlet's try again");
                        //Console.ReadLine();
                        return ExamResult.Retry;
                    }
                    else
                    {
                        await chat.SendMessage($"'{userEntry}' translates as {translationCandidate.Translation}");
                        service.RegistrateFailure(word);
                        return ExamResult.Failed;
                    }
                }
                await chat.SendMessage("The translation was: " + word.OriginWord);
                service.RegistrateFailure(word);
                return ExamResult.Failed;
            }
        }
    }
}