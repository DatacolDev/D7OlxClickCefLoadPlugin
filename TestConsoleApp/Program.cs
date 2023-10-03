﻿using CefBrowserWrapper;
using Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // BeforeLoadPluginTest();

            AfterLoadPluginTest();
        }

        static void BeforeLoadPluginTest()
        {
            string url = "http://webasyst.synoparser.ru/index.php?categoryID=564";
            HandlerClass hc = new HandlerClass();

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string error = string.Empty;
            parameters.Add("campaignname", "Test");
            parameters.Add("dev", "");
            parameters.Add("type", "before_load_page_plugin");
            parameters.Add("url", url);

            parameters.Add("cancellation_token", CancellationToken.None);
            parameters.Add("nestinglevel", 0);
            parameters.Add("referer", "");

            try
            {
                hc.pluginHandler(parameters, out error);
            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
        }
        static void AfterLoadPluginTest()
        {
            // Флаг, обозначающий нужно ли отображать окно браузера (аналогичная настройка есть в Datacol)
            bool showBrowser = true;
            string url = "https://www.olx.ua/uk/nedvizhimost/kvartiry/prodazha-kvartir/cherkassy/";
                //"https://www.olx.ua/d/uk/obyavlenie/termnoviy-prodazh-1-kmnatno-kvartiri-IDSSttc.html";
            HandlerClass hc = new HandlerClass();

            Dictionary<string, object> parameters = new Dictionary<string, object>();
            string error = string.Empty;
            parameters.Add("campaignname", "Test");
            parameters.Add("dev", "");
            parameters.Add("type", "after_load_page_plugin");
            parameters.Add("url", url);
            parameters.Add("show_browser_form", showBrowser);
            parameters.Add("cancellation_token", CancellationToken.None);
            parameters.Add("nestinglevel", 0);
            parameters.Add("referer", "");

            CefBrowserWrapperFactoryBase factory = new UniCefBrowserWrapperFactory(showBrowser, false);
            CefBrowserWrapperBase cefBrowserWrapper = factory.Create(true, true, true, 50000, false,
                new SingleBrowserInfo("", "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36"
                ));
            parameters.Add("cef_browser_wrapper", cefBrowserWrapper);
            try
            {

                cefBrowserWrapper.LoadUrl(url);

                hc.pluginHandler(parameters, out error);

                cefBrowserWrapper.Dispose();
                factory.Dispose();

            }
            catch (Exception exp)
            {
                Console.WriteLine(exp.Message);
            }
        }
    }
}
