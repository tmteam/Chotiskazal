using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;
using Dic.Logic.yapi;

namespace Chotiskazal.Bot.ChatFlows
{
    class WordAdditionMode
    {
        private readonly Chat _chat;
        private readonly YandexDictionaryApiClient _yapiDicClient;
        private string _wordForTranslate;
        private readonly YandexTranslateApiClient _yapiTransClient;
        public WordAdditionMode(
            Chat chat, 
            YandexTranslateApiClient yapiTranslateApiClient, 
            YandexDictionaryApiClient yandexDictionaryApiClient,
                string wordForTranslate = null
            )
        {
            _chat = chat;
            _yapiDicClient = yandexDictionaryApiClient;
            _wordForTranslate = wordForTranslate;
            _yapiTransClient = yapiTranslateApiClient;
        }
        public string Name => "Add new words";
        public async Task Enter(NewWordsService service)
        {
            var dicPing = _yapiDicClient.Ping();
            var transPing = _yapiTransClient.Ping();
            await dicPing;
            await transPing;
            var timer = new Timer(5000) { AutoReset = false };
            timer.Enabled = true;
            timer.Elapsed += (s, e) => {
                var pingDicApi = _yapiDicClient.Ping();
                var pingTransApi = _yapiTransClient.Ping();
                Task.WaitAll(pingDicApi, pingTransApi);
                timer.Enabled = true;
            };

            if (_yapiDicClient.IsOnline)
                await _chat.SendMessage("Yandex dic is online");
            else
                await _chat.SendMessage("Yandex dic is offline");

            if (_yapiTransClient.IsOnline)
                await _chat.SendMessage("Yandex trans is online");
            else
                await _chat.SendMessage("Yandex trans is offline");

            while (true)
            {
                if (_wordForTranslate == null)
                {
                    await _chat.SendMessage("Enter [e] for exit or ");
                    await _chat.SendMessage("Enter english word: ");
                    _wordForTranslate = await _chat.WaitUserTextInput();
                    if (_wordForTranslate == "e")
                        break;
                }

                YaDefenition[] defenitions = null;
                if (_yapiDicClient.IsOnline)
                    defenitions = await _yapiDicClient.Translate(_wordForTranslate);

                var translations = new List<TranslationAndContext>();
                if (defenitions?.Any() == true)
                {
                    var variants = defenitions.SelectMany(r => r.Tr);
                    foreach (var yandexTranslation in variants)
                    {
                        var phrases = yandexTranslation.GetPhrases(_wordForTranslate);

                        translations.Add(new TranslationAndContext(_wordForTranslate, yandexTranslation.Text, yandexTranslation.Pos, phrases.ToArray()));
                    }

                }
                else
                {
                    var dictionaryMatch = service.GetTranslations(_wordForTranslate);
                    if (dictionaryMatch != null)
                    {
                        translations.AddRange(
                            dictionaryMatch.Translations.Select(t =>
                                new TranslationAndContext(dictionaryMatch.Origin, t, dictionaryMatch.Transcription,
                                    new Phrase[0])));
                    }
                }

                if (!translations.Any())
                {
                    try
                    {
                        var translateResponse = await _yapiTransClient.Translate(_wordForTranslate);

                        if (string.IsNullOrWhiteSpace(translateResponse))
                        {
                            await _chat.SendMessage("No translations found. Check the word and try again");
                        }
                        else
                        {
                            translations.Add(new TranslationAndContext(_wordForTranslate, translateResponse, null, new Phrase[0]));
                        }
                    }
                    catch (Exception e)
                    {
                        await _chat.SendMessage("No translations found. Check the word and try again");
                    }
                }

                if (translations.Any())
                {
                    await _chat.SendMessage("e: [back to main menu]");
                    await _chat.SendMessage("c: [CANCEL THE ENTRY]");
                    int i = 1;
                    foreach (var translation in translations)
                    {
                        if(translation.Phrases.Any())
                            await _chat.SendMessage($"{i}: {translation.Translation}\t (+{translation.Phrases.Length})");
                        else
                            await _chat.SendMessage($"{i}: {translation.Translation}");
                        i++;
                    }

                    try
                    {
                        var results = await ChooseTranslation(translations.ToArray());
                        if (results?.Any() == true)
                        {
                            var allTranslations = results.Select(t => t.Translation).ToArray();
                            var allPhrases = results.SelectMany(t => t.Phrases).ToArray();
                            service.SaveForExams(
                                word: _wordForTranslate,
                                transcription: translations[0].Transcription,
                                translations: allTranslations,
                                allMeanings: translations.Select(t=>t.Translation.Trim().ToLower()).ToArray(),
                                phrases: allPhrases);
                            await _chat.SendMessage($"Saved. Tranlations: {allTranslations.Length}, Phrases: {allPhrases.Length}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    _wordForTranslate = null;
                }
            }
        }


        async Task<TranslationAndContext[]> ChooseTranslation(TranslationAndContext[] translations)
        {
            while (true)
            {
                await _chat.SendMessage("Choose the word:");
                var res = await _chat.WaitUserTextInput();
                res = res.Trim();
                if (res.ToLower() == "e")
                    throw new OperationCanceledException();
                if (res.ToLower() == "c")
                    return null;

                if (!int.TryParse(res, out var ires))
                {
                    var subItems = res.Split(',');
                    if (subItems.Length > 1)
                    {
                        try
                        {
                            return subItems
                                .Select(s => int.Parse(s.Trim()))
                                .Select(i => translations[i - 1])
                                .ToArray();
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                    }
                    if (res.Length > 1)
                        return new[] { new TranslationAndContext(translations[0].Origin, res, translations[0].Transcription, new Phrase[0]) };
                    else continue;
                }
                if (ires == 0)
                    return null;
                if (ires > translations.Length || ires < 0)
                    continue;
                return new[] { translations[ires - 1] };
            }
        }
    }

}
