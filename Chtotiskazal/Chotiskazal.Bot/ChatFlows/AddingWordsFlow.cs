#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Chotiskazal.Logic.Services;
using Dic.Logic.DAL;
using Dic.Logic.yapi;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot.ChatFlows
{
    class AddingWordsMode
    {
        private readonly Chat _chat;
        private readonly YandexDictionaryApiClient _yapiDicClient;
        private readonly YandexTranslateApiClient _yapiTransClient;
        private readonly NewWordsService _wordService;

        public AddingWordsMode(
            Chat chat, 
            YandexTranslateApiClient yapiTranslateApiClient, 
            YandexDictionaryApiClient yandexDictionaryApiClient,
            NewWordsService wordService
            )
        {
            _chat = chat;
            _wordService = wordService;
            _yapiDicClient = yandexDictionaryApiClient;
            _yapiTransClient = yapiTranslateApiClient;
        }
        public async Task Enter(string? word = null)
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

            /*if (_yapiDicClient.IsOnline)
                await _chat.SendMessage("Yandex dic is online");
            else
                await _chat.SendMessage("Yandex dic is offline");

            if (_yapiTransClient.IsOnline)
                await _chat.SendMessage("Yandex trans is online");
            else
                await _chat.SendMessage("Yandex trans is offline");*/

            while (true)
            {
                if(!await EnterSingleWord(word))
                    break;
                word = null;
            }
        }

        async Task<bool> EnterSingleWord(string? word = null)
        {
            if (word == null)
            {
                await _chat.SendMessage("Enter english word", new InlineKeyboardButton
                {
                    CallbackData = "/exit",
                    Text = "Cancel"
                });
                while (true)
                {
                    var input = await _chat.WaitUserInput();
                    if (input.CallbackQuery != null && input.CallbackQuery.Data == "/exit")
                        throw new ProcessInteruptedWithMenuCommand("/start");

                    if (!string.IsNullOrEmpty(input.Message.Text))
                    {
                        word = input.Message.Text;
                        break;
                    }
                }
            }

            YaDefenition[]? definitions = null;
            if (_yapiDicClient.IsOnline)
                definitions = await _yapiDicClient.Translate(word);

            var translations = new List<TranslationAndContext>();
            if (definitions?.Any() == true)
            {
                var variants = definitions.SelectMany(r => r.Tr);
                foreach (var yandexTranslation in variants)
                {
                    var phrases = yandexTranslation.GetPhrases(word);

                    translations.Add(new TranslationAndContext(word, yandexTranslation.Text,
                        yandexTranslation.Pos, phrases.ToArray()));
                }

            }
            else
            {
                var dictionaryMatch = _wordService.GetTranslations(word);
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
                    var translateResponse = await _yapiTransClient.Translate(word);

                    if (string.IsNullOrWhiteSpace(translateResponse))
                    {
                        await _chat.SendMessage("No translations found. Check the word and try again");
                    }
                    else
                    {
                        translations.Add(new TranslationAndContext(word, translateResponse, null,
                            new Phrase[0]));
                    }
                }
                catch (Exception)
                {
                    await _chat.SendMessage("No translations found. Check the word and try again");
                }
            }

            if (!translations.Any()) return true;

            await _chat.SendMessage($"Choose translation for '{word}'",
                InlineButtons.CreateVariants(translations.Select(t => t.Translation)));
            while (true)
            {
                var input = await _chat.TryWaitInlineIntKeyboardInput();
                if (!input.HasValue)
                    return false;
                if (input.Value >= 0 && input.Value < translations.Count)
                {
                    var selected = translations[input.Value];
                    //var allTranslations = results.Select(t => t.Translation).ToArray();
                    var allPhrases = selected.Phrases; // results.SelectMany(t => t.Phrases).ToArray();
                    _wordService.SaveForExams(
                        word: word,
                        transcription: translations[0].Transcription,
                        translations: new[] {selected.Translation},
                        allMeanings: translations.Select(t => t.Translation.Trim().ToLower()).ToArray(),
                        phrases: allPhrases);
                    await _chat.SendMessage($"Saved. Translations: {1}, Phrases: {allPhrases.Length}");
                    return true;
                }
            }
        }

        /*
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
    */
    }

    }
