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

                BasicScenario(cefBrowserWrapper, devMode);

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

        #region Examples
        private void BasicScenario(CefBrowserWrapperBase cefBrowserWrapper, bool devMode)
        {
            // Скролл
            cefBrowserWrapper.Scroll(100);
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

            // Клик по вебэлементу с имитацией "живого" пользователя
            cefBrowserWrapper.SendMouseClickToElement("//input[@id='ctrl-prd-cmp-3942']");

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
