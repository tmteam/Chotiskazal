﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;

namespace Chotiskazal.Bot.Questions
{
    public class EngWriteExam : IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "Eng Write";

        public async Task<ExamResult> Pass(Chat chat, NewWordsService service, PairModel word, PairModel[] examList)
        {
            var translations = word.GetTranslations();
            var minCount = translations.Min(t => t.Count(c => c == ' '));
            if (minCount>0 && word.PassedScore< minCount*4)
                return ExamResult.Impossible;

            await chat.SendMessage($"=====>   {word.OriginWord}    <=====\r\nWrite the translation... ");
            var translation = await chat.WaitUserTextInput();
            if (string.IsNullOrEmpty(translation))
                return ExamResult.Retry;

            if (translations.Any(t => string.Compare(translation, t, StringComparison.OrdinalIgnoreCase) == 0))
            {
                service.RegistrateSuccess(word);
                return ExamResult.Passed;
            }
            else
            {
                if (word.GetAllMeanings()
                    .Any(t => string.Compare(translation, t, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    await chat.SendMessage($"Choosen translation is out of scope (but it is correct). Expected translations are: " + word.Translation);
                    return ExamResult.Impossible;
                }
                await chat.SendMessage("The translation was: "+ word.Translation);
                service.RegistrateFailure(word);
                return ExamResult.Failed;
            }
        }
    }
}