using System;
using System.Linq;
using System.Threading.Tasks;
using Chotiskazal.Bot.ChatFlows;
using Chotiskazal.Logic.Services;
using Dic.Logic.yapi;
using Telegram.Bot.Types.ReplyMarkups;

namespace Chotiskazal.Bot
{
    public class ChatRoomFlow
    {
        public ChatRoomFlow(Chat chat)
        {
            Chat = chat;
        }
        public NewWordsService WordsService { get; set; }
        public YandexDictionaryApiClient YaDictionaryApi { get; set; }
        public YandexTranslateApiClient YaTranslateApi { get; set; }
        public Chat Chat { get;}
        
        public async Task Run(){ 
            string mainMenuCommandOrNull = null;
            while(true)
            {
                try
                {
                    if(mainMenuCommandOrNull!=null)
                    {
                        await HandleMainMenu(mainMenuCommandOrNull); 
                        mainMenuCommandOrNull = null;
                    }
                    await ModeSelection();	
                }
                catch(UserAFKException){
                    await Chat.SendMessage("bybye");
                    return;		
                }
                catch(ProcessInteruptedWithMenuCommand e){
                    mainMenuCommandOrNull = e.Command;
                }
                catch(Exception e){
                    Console.WriteLine($"Error: {e}, from {Chat}");
                    await Chat.SendMessage("Oops. something goes wrong ;(");
                }
            }
        }
        
        Task SendNotAllowedTooltip() => Chat.SendTooltip("action is not allowed");
        Task Examinate() => Chat.SendTodo();
        
        //show stats to user here
        Task ShowStats()
        {
            var statsFlow = new GraphsStatsFlow(Chat, WordsService);
            return statsFlow.Enter();
        }

        Task EnterWord(string text = null)
        {
            var mode = new AddingWordsMode(Chat, YaTranslateApi, YaDictionaryApi,WordsService);
            return mode.Enter(text);
            
            /*string[] pairs = {
                "тест, испытание, проверка,проба",
                "тестирование, экзамен",
                "анализ",
                "обследование",
                "Ничего не подходит :("
            };
            
            Chat.SendMessage($"Choose translation for '{text}'", pairs.Select(
                (p,i)=>new InlineKeyboardButton
                {
                    CallbackData = i.ToString(),
                    Text = p.ToString()
                }).ToArray());
            
            while (true)
            {
                var input = await Chat.WaitUserInput();
                if (input.CallbackQuery != null)
                {
                    var btn = input.CallbackQuery.Data;
                    if (int.TryParse(btn, out var i))
                    {
                        if (i >= 0 && i < pairs.Length)
                        {
                            await Chat.SendMessage("Translate accepted");
                            return;
                        }
                    }
                }
                var _ = Chat.SendMessage("Choose an translate option or press /start");
            }*/
        }

      

        Task HandleMainMenu(string command){
            switch (command){
                case "/help": SendHelp(); break;
                case "/add":  return EnterWord(null);
                case "/train": return Examinate();
                case "/stats": ShowStats(); break;;
                case "/start": break;
            }
            return Task.CompletedTask;
        }

        private Task SendHelp() => Chat.SendMessage("Call 112 for help");

        async Task ModeSelection()
        {
            while (true)
            {
                var _  = Chat.SendMessage("Select mode, or enter a word to translate",
                    Buttons.EnterWords,
                    Buttons.Exam,
                    Buttons.Stats);

                while (true)
                {
                    var action = await Chat.WaitUserInput();

                    if (action.Message!=null)
                    {
                        await EnterWord(action.Message.Text);
                        return;
                    }

                    if (action.CallbackQuery!=null)
                    {
                        var btn = action.CallbackQuery.Data;
                        if (btn == Buttons.EnterWords.CallbackData)
                        {
                            await EnterWord();
                            return;
                        }
                        else if (btn == Buttons.Exam.CallbackData)
                        {
                            await Examinate();
                            return;
                        }

                        else if (btn == Buttons.Stats.CallbackData)
                        {
                            await ShowStats();
                            return;
                        }
                    }

                    await SendNotAllowedTooltip();
                }
            }
        }
    }
}