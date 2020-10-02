using HtmlAgilityPack;
using System;

namespace ParseLib
{
    public class Test
    {
        private static readonly string _siteUrl = @"https://rozetka.com.ua/";

        public string GetPageResult()
        {
            var site = new HtmlWeb();
            var result = site.Load(_siteUrl);

            return result.DocumentNode.InnerHtml;
        }
    }
}
