using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Specialized;
using System.IO;
using System.Data;
using System.Net;
using System.Threading;
using CefBrowserWrapper;
using System.Diagnostics;
using System.Windows.Forms;
using CefSharp.Fluent;
using DatacolPluginTemplate;
using System.Runtime.InteropServices;
using CefSharp;
using System.Threading.Tasks;
using Point = System.Drawing.Point;
using CefSharp.DevTools.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DatacolPluginTemplate.CloudflareConsigned;
using cloudfare_bypass_cefsharp;

namespace Plugin
{
    /// <summary>
    /// Пример простого плагина загрузки страницы. Плагин загружает страницу по ссылке и возвращает исходный код либо ошибку.
    /// </summary>
    public class HandlerClass : PluginInterface.IPlugin
    {

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);



        /// <summary>
        /// Обработчик плагина
        /// </summary>
        /// <param name="parameters">Словарь параметров: ключ - имя параметра (string), 
        /// значение - содержимое параметра (object, который в зависимости от типа плагина (задается в parameters["type"])
        /// и ключа приводится к тому или иному типу) </param>
        /// <param name="error">Переменная (string), в которую возвращается ошибка работы плагина, 
        /// если таковая произошла. Если ошибки не произошло, данная переменная должна оставаться пустой строкой</param>
        /// <returns>Возвращаемое значение - это объект, который может иметь тот или иной тип,
        /// в зависимости от типа плагина (задается в  parameters["type"])</returns>
        public object pluginHandler(Dictionary<string, object> parameters, out string error)
        {
            string retVal = "";
          
            error = "";


            // URL текущей страницы
            string url = parameters["url"].ToString();
            // токен позволяет отследить, если пользователь остановил работу кампании с помощью ct.IsCancellationRequested
            CancellationToken ct = (CancellationToken)parameters["cancellation_token"];
            // Обертка для доступа к объекту браузера, в том числе командам вроде Click и т.п.

            // Переменная позволяет добавлять в сценарий элементы для отладки (например сообщения в виде диалогового окна) для случая,
            // если запуск плагина произведен из тестового приложения, а не из программы Datacol
            bool devMode = parameters.ContainsKey("dev");
            
            // Уровень вложенности текущей страницы, на котором она найдена парсером
            int nestingLevel = Convert.ToInt32(parameters["nestinglevel"]);
            // Ссылка на страницу, на которой парсер нашел ссылку на текущую страницу
            string referer = parameters["referer"].ToString();

            //Проверяем в качестве какого из плагинов вызывается текущая сборка: как плагин перед загрузкой страницы в Cefsharp
            //или как плагин после загрузки страницы в CefSharp.
            if (String.Compare(parameters["type"].ToString(), "after_load_page_plugin") == 0)
            {
                // Параметр, показывающий задал ли пользователь режим показа окна браузера
                bool showBrowser = Convert.ToBoolean(parameters["show_browser_form"]);
                // Приводим объект браузера к соответствующему типу в зависимости от параметра show_browser_form
                CefBrowserWrapperBase cefBrowserWrapper;
                if (showBrowser) cefBrowserWrapper = (CefScreenBrowserWrapper)parameters["cef_browser_wrapper"];
                else cefBrowserWrapper = (CefOffscreenBrowserWrapper)parameters["cef_browser_wrapper"];

                OlxClickNumber(cefBrowserWrapper, true).Wait();

                // BasicScenario(cefBrowserWrapper, devMode);

                // DownloadByClickScenario(cefBrowserWrapper, ct, 50, "//a[@id='click_to_download']");//TODO: замените последний параметр на реальный xpath

                // retVal = GetFrameContent(cefBrowserWrapper, ct);

            }
            else if (String.Compare(parameters["type"].ToString(), "before_load_page_plugin") == 0)
            {
                // Проверяем условие, например: если страница (кеш ее кода) есть в нашей базе данных
                // То возвращаем ее код. Datacol проверить возвращаемое значение плагина.
                // Если оно окажется не пустым - он не будет осуществлять загрузку с помощью Cefsharp.
                // Вместо этого будет использовать код, возвращенный плагином.
                // Аналогично мог бы работать альтернативный сценарий. Когда мы проверяем URL страницы,
                // например, на предмет того, что она является страницей товара. 
                // Если так, то мы грузим ее через HttpClient прямо в плагине и возвращаем ее код. Если нет - то
                // возвращаем пустое значение, и в таком случае Datacol самостоятельно ее загружает.
                if (true)
                {
                    // Получить код страницы из базы
                    retVal = "SAVED_PAGECODE";
                }
                else
                {
                    retVal = "";
                }
            }
            else
            {
                throw new Exception("Вы используете неверный тип плагина");
            }
            
            return retVal;
        }


        public class ClickOptions
        {
            /// <summary>
            /// Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0
            /// </summary>
            public int Delay { get; set; } = 0;

            /// <summary>
            /// Defaults to 1. See https://developer.mozilla.org/en-US/docs/Web/API/UIEvent/detail
            /// </summary>
            public int ClickCount { get; set; } = 1;

            /// <summary>
            /// The button to use for the click. Defaults to <see cref="MouseButton.Left"/>
            /// </summary>
            public MouseButton Button { get; set; } = MouseButton.Left;
        }


        private async Task OlxClickNumber(CefBrowserWrapperBase cefBrowserWrapper, bool devMode)
        {
            try
            {////button[@data-testid="show-phone"]
                cefBrowserWrapper.ScrollToElement("//button[@data-testid='show-phone']");
                await Task.Delay(300).ConfigureAwait(false);

                //Scroll up
                cefBrowserWrapper.Scroll();

                Point? p1 = await GetCordEl(cefBrowserWrapper,
                    "button[data-testid*=\"show-phone\"]");
                //p1 = await GetCordEl(cefBrowserWrapper,
                //    "button[data-testid=\"show-phone\"]");

                Point newp = new Point(p1.Value.X, p1.Value.Y);
                var inprt = cefBrowserWrapper.GetBrowser().GetBrowser().GetHost().GetWindowHandle();

                // This click method through cpd chrome
                await ClickAsync(cefBrowserWrapper, newp.X, newp.Y,
                    new ClickOptions { Button = MouseButton.Left, ClickCount = 1, Delay = 200 });
                await Task.Delay(3000).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception details, including the message and stack trace
                Console.WriteLine("Error in Cloudflare method: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw; // Re-throw the exception to propagate it
            }

        }


        private int LastMessageId = 6000;

        public async Task SendDevtoolEvent(CefBrowserWrapperBase cefBrowserWrapper, string evnt, object eventnameAndParam)
        {
            Dictionary<string, object> param = new Dictionary<string, object>();

            if (eventnameAndParam != null)
            {
                var json = JsonConvert.SerializeObject(eventnameAndParam, Newtonsoft.Json.Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new LowercaseContractResolver()
                    });
                param = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }

            IBrowserHost host = cefBrowserWrapper.GetBrowser().GetBrowser().GetHost();
            if (host == null || host.IsDisposed)
            {
                throw new Exception("BrowserHost is Null or Disposed");
            }


            int msgId = Interlocked.Increment(ref LastMessageId);

            TaskMethodDevToolsMessageObserver observer = new TaskMethodDevToolsMessageObserver(msgId);

            //Make sure to dispose of our observer registration when done
            //TODO: Create a single observer that maps tasks to Id's
            //Or at least create one for each type, events and method
            using (IRegistration observerRegistration = host.AddDevToolsMessageObserver(observer))
            {
                //Page.captureScreenshot defaults to PNG, all params are optional
                //for this DevTools method
                int id = 0;

                //TODO: Simplify this, we can use an Func to reduce code duplication
                if (Cef.CurrentlyOnThread(CefThreadIds.TID_UI))
                {
                    id = host.ExecuteDevToolsMethod(msgId, evnt, param);
                }
                else
                {
                    id = await Cef.UIThreadTaskFactory.StartNew(() =>
                    {
                        var json = JsonConvert.SerializeObject(param);
                        //	return host.ExecuteDevToolsMethod(msgId, evnt, param);
                        return host.ExecuteDevToolsMethod(msgId, evnt, json);
                    });
                }

                if (id != msgId)
                {
                    throw new Exception("Message Id doesn't match the provided Id");
                }

                Tuple<bool, byte[]> result = await observer.Task.ConfigureAwait(false);

                bool success = result.Item1;

                dynamic response = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(result.Item2));
                string res = JsonConvert.SerializeObject(response);

                //Success
                if (success)
                {
                }

                string code = (string)response.code;
                string message = (string)response.message;
            }
        }

        private int msgId;

        public async Task MoveAsync(CefBrowserWrapperBase cefBrowserWrapper, decimal x, decimal y, MoveOptions options = null)
        {
            options = options ?? new MoveOptions();
            var fromX = _x;
            var fromY = _y;
            _x = x;
            _y = y;
            var steps = options.Steps;

            for (var i = 1; i <= steps; i++)
            {
                await SendDevtoolEvent(cefBrowserWrapper, "Input.dispatchMouseEvent", new InputDispatchMouseEventRequest
                {
                    Type = MouseEventType.MouseMoved,
                    Button = "left",
                    X = fromX + ((_x - fromX) * ((decimal)i / steps)),
                    Y = fromY + ((_y - fromY) * ((decimal)i / steps)),
                    Modifiers = 0
                }).ConfigureAwait(false);
            }
        }

        private decimal _x = 0;
        private decimal _y = 0;
        private MouseButton _button = MouseButton.None;

        public Task DownAsync(CefBrowserWrapperBase cefBrowserWrapper, ClickOptions options = null)
        {
            options = options ?? new ClickOptions();

            var _button = options.Button;

            return SendDevtoolEvent(cefBrowserWrapper, "Input.dispatchMouseEvent", new InputDispatchMouseEventRequest
            {
                Type = MouseEventType.MousePressed,
                Button = "left",
                X = _x,
                Y = _y,
                Modifiers = 0,
                ClickCount = options.ClickCount
            });
        }

        public Task UpAsync(CefBrowserWrapperBase cefBrowserWrapper, ClickOptions options = null)
        {
            options = options ?? new ClickOptions();

            _button = MouseButton.None;

            return SendDevtoolEvent(cefBrowserWrapper, "Input.dispatchMouseEvent", new InputDispatchMouseEventRequest
            {
                Type = MouseEventType.MouseReleased,
                Button = "left",
                X = _x,
                Y = _y,
                Modifiers = 0,
                ClickCount = options.ClickCount
            });
        }
        public async Task ClickAsync(CefBrowserWrapperBase cefBrowserWrapper, decimal x, decimal y, ClickOptions options = null)
        {
            options = options ?? new ClickOptions();

            if (options.Delay > 0)
            {
                await Task.WhenAll(
                    MoveAsync(cefBrowserWrapper, x, y),
                    DownAsync(cefBrowserWrapper, options));

                await Task.Delay(options.Delay);
                await UpAsync(cefBrowserWrapper);
            }
            else
            {
                await Task.WhenAll(
                    DownAsync(cefBrowserWrapper, options),
                    UpAsync(cefBrowserWrapper));
            }
        }

        public async Task<Point?> GetCordEl(CefBrowserWrapperBase cefBrowserWrapper, string attr)
        {
            Point point = Point.Empty;

            var script = @"(function () {
			    var bnt = document.querySelector('" + attr + @"');
			   
			    var bntRect = bnt.getBoundingClientRect();
			    return JSON.stringify({ x: bntRect.left, y: bntRect.top });
			})();";
            JavascriptResponse jsReponse = await cefBrowserWrapper.GetBrowser().EvaluateScriptAsync(
                script
            );
            if (jsReponse.Success && jsReponse.Result != null)
            {
                string jsonString = (string)jsReponse.Result;

                if (jsonString != null)
                {
                    JObject jsonObject = JObject.Parse(jsonString);
                    int xPosition = (int)jsonObject["x"] + 1; // add +1 pixel to the click position
                    int yPosition = (int)jsonObject["y"] + 1; // add +1 pixel to the click position

                    point = new Point(xPosition, yPosition);
                }
            }

            return point;
        }

        #region Examples
        private void BasicScenario(CefBrowserWrapperBase cefBrowserWrapper, bool devMode)
        {
            // Скролл
            cefBrowserWrapper.Scroll();
            DebugAlert("Push Enter to continue", cefBrowserWrapper, devMode);

            // Скролл к элементу
            cefBrowserWrapper.ScrollToElement("//input[@name='search_name']");
            DebugAlert("Push Enter to continue", cefBrowserWrapper, devMode);

            // Установка текстового значения вебэлементу
            cefBrowserWrapper.SetValue("//input[@name='search_name']", "test1");
            // Отправка текста вебэлементу с имитацией "живого" пользователя
            cefBrowserWrapper.SendTextToElement("//input[@name='search_price_from']", "test2");

            // Клик по вебэлементу
            cefBrowserWrapper.Click("//input[@name='advanced_search_in_category']");

            // Подождем загрузки элемента
            cefBrowserWrapper.WaitElement("//input[@id='cat_search_in_subcategory']", 5);

            // Задержка для наглядности - чтобы было видно эффект последующего клика по чекбоксу
            Thread.Sleep(1500);

            // Клик по вебэлементу с имитацией "живого" пользователя
            cefBrowserWrapper.SendMouseClickToElement("//input[@id='cat_search_in_subcategory']");

            // Так можно получить исходный код страниц для каких либо нужд
            // Возвращать его нет смысла, поскольку Datacol все равно будет получать 
            // Исходный код страницы после выполнения этого плагина
            cefBrowserWrapper.GetHtml();

            DebugAlert("Нажми ОК для выхода", cefBrowserWrapper, devMode);
        }

        private void DownloadByClickScenario(
            CefBrowserWrapperBase cefBrowserWrapper, 
            CancellationToken ct,
            int maxSecondsToWaitForDownload,
            string xpathOfElementToClickOn)
        {
            ManualResetEventSlim downloadReadyEvent = new ManualResetEventSlim(false);

            // To Stop showing download form
            cefBrowserWrapper.DownloadHandler =
                DownloadHandler.UseFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    (chromiumBrowser, browser, downloadItem, callback) =>
                    {
                        if (downloadItem.IsComplete || downloadItem.IsCancelled)
                        {
                            if (browser.IsPopup && !browser.HasDocument)
                            {
                                browser.GetHost().CloseBrowser(true);
                                downloadReadyEvent.Set();
                            }
                        }
            //TODO: You may wish to customise this condition to better suite your
            //requirements. 
            else if (downloadItem.ReceivedBytes < 100)
                        {
                            var popupHwnd = browser.GetHost().GetWindowHandle();

                            var visible = IsWindowVisible(popupHwnd);
                            if (visible)
                            {
                                const int SW_HIDE = 0;
                                ShowWindow(popupHwnd, SW_HIDE);
                            }
                        }
                    });

            cefBrowserWrapper.Click(xpathOfElementToClickOn);

            downloadReadyEvent.Wait(maxSecondsToWaitForDownload * 1000, ct);
        }

        private string GetFrameContentScenario(CefBrowserWrapperBase cefBrowserWrapper,
            CancellationToken ct)
        {
            // Ожидаем загрузки вебэлемента, содержащего фрейм
            if(!cefBrowserWrapper.WaitElement("//iframe[@id='FRAME_ID']", 5))
            { 
                throw new Exception("Вебэлемент, содержащий фрейм, не найден на странице");
            }

            IWebBrowser myIWebBrowser = cefBrowserWrapper.GetBrowser();
            // Получаем внутренние идентификаторы всех фреймов
            // Они могут отличаться от значений, которые задаются в параметра ID элемента фрейма
            var identifiers = myIWebBrowser.GetBrowser().GetFrameIdentifiers();
            // Получаем доступ к конкретному фрейму по индексу
            // Если мы не знаем, какой индекс фрейма, можно пройти в цикле for по всем фреймам
            // И каждый проверять на наличие нужного кода
            IFrame frame = myIWebBrowser.GetBrowser().GetFrame(identifiers[1]);
            // Ожидаем загрузки конкретного элемента во фрейме
            // Это нужно в ситуациях, когда содержимое фрейма подгружается динамично, не сразу
            cefBrowserWrapper.WaitElement("//TAG[@class='CLASS']", 10, frame);
            // Получаем исходный код фрейма
            Task<string> task = frame.GetSourceAsync();

            task.Wait(5000, ct);

            if (!task.IsCompleted) throw new TimeoutException("Время ожидания загрузки фрейма истекло");
            // Возвращаем исходный код фрейма
            // Предполагается, что плагин его вернет Datacol,
            // и исходный код фрейма будет помещен под исходным кодом загруженной изначально страницы
            return task.Result;
        }


        #endregion

        #region Utilities
        /// <summary>
        /// Функция выдает алерт в браузере. Отрабатывает только при запуске кода плагина
        /// из тестового консольного приложения
        /// </summary>
        void DebugAlert(string message, CefBrowserWrapperBase cefBrowserWrapper, bool devMode)
        {
            if (devMode) cefBrowserWrapper.EvaluateScript("alert('"+ message +"');");
        }
        #endregion
        #region Методы и свойства необходимые, для соответствия PluginInterface (обычно не используются при создании плагина)

        public void Init()
        {
            //инициализация пока не нужна
        }

        public void Destroy()
        {
            //это тоже пока не надо
        }

        public string Name
        {
            get { return "PluginName"; }
        }

        public string Description
        {
            get { return "Описание текущего плагина"; }
        }

        #endregion
    }

}
