using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ParseLib.Service
{
    public class RozetkaParser : ISiteParser
    {

        private static readonly string _siteUrl = @"https://rozetka.com.ua/";
        private static readonly HtmlWeb _htmlWeb = new HtmlWeb();
        private static Semaphore _semaphore = new Semaphore(5, 5);

        public void ParseSite()
        {
            var categoriesUrls = GetAllCategories();
            var productsUrls = new List<string>();
            var result = new List<Dictionary<string, string>>();

            foreach (var categoryUrl in categoriesUrls)
            {
                productsUrls = GetProductsUrlsFromCategory(categoryUrl);

                result = GetItemsData(productsUrls);    
            }
        }

        public List<string> GetSiteCategoriesUrls()
        {
            var selector = "//a[@class='menu-categories__link']";
            var attribute = "href";
            var urls = GetPageDataByUrlAndSelector(_siteUrl, selector, attribute);

            return urls;
        }

        public List<string> GetSubCategoriesUrls(string mainCategoryUrl)
        {
            var splitSymol = ',';
            var splitIndex = 0;
            var selector = @"(https://rozetka.com.ua/\w*\S?\w*?\S?\w*?/\w*/\Stitle)|(https://rozetka.com.ua/\w*\S?\w*?\S?\w*?\Stitle)|(https://hard.rozetka.com.ua/\w*\S?\w*?\S?\w*?/\w*/\Stitle)|(https://hard.rozetka.com.ua/\w*\S?\w*?\S?\w*?\Stitle)";
            var urls = GetPageDataByUrlAndRegex(mainCategoryUrl, selector, splitSymol, splitIndex);

            return urls;
        }

        public List<string> GetProductsUrlsFromCategory(string url, bool isHaveNextPage = true)
        {
            var splitSymol = ',';
            var splitIndex = 0;
            var nextPageSelector = @"pagination__direction_type_forward\S\shref=\Shttps://rozetka.com.ua/\w*\S?\w*?\S?\w*?\S?\w*?/\w*\S?\w*?\S?\w*?\S?\w*?/page=\d*/";
            var regexSelector = @"(https://rozetka.com.ua/\w*\S?\w*?\S?\w*?\S?\w*?/\w*\Ssell_status)|(https://rozetka.com.ua/\w*\S?\w*?\S?\w*?\S?\w*?/\w*\Ssell_status)";
            var result = new List<string>();

            if (!isHaveNextPage)
            {
                return result;
            }

            var urls = GetPageDataByUrlAndRegex(url, regexSelector, splitSymol, splitIndex);

            result.AddRange(urls);

            var nextPageUrl = GetNextPage(url, nextPageSelector, out isHaveNextPage);
            result.AddRange(GetProductsUrlsFromCategory(nextPageUrl, isHaveNextPage));

            return result;
        }

        public List<Dictionary<string, string>> GetItemsData(List<string> productsUrls)
        {
            var result = new List<Dictionary<string, string>>();

            foreach (var productUrl in productsUrls)
            {
                _semaphore.WaitOne();
            
                Thread thread = new Thread(() => {
                    var productData = GetItemCharacteristic(productUrl);
                    
                    result.Add(productData);
                    _semaphore.Release();
                });

                thread.Start();
            }

            return result;
        }

        public Dictionary<string, string> GetItemCharacteristic(string prodctUrl)
        {
            var splitSymol = ',';
            var splitSymolValue = ':';
            var splitIndexTitle = 3;
            var splitIndexValue = 2;
            var result = new Dictionary<string, string>();
            prodctUrl += "characteristics/";
            
            var regexSelectorTitle = @"(id\S\d*\Stype\S\w*\Sname\S((\w*\S?){1,10}|(\w*))\Stitle\S((\w*(\s?\S?\d*?\w*?)){1,10}|(\w*\s?){1,10}|(\w*))\S?,)";
            var regexSelectorValue = @"values(\S){3}title\S((\w*(\s?\S?\d*?\w*?)){1,10}|(\w*\s?){1,10}|(\w*))\S\w*";

            var titles = GetPageDataByUrlAndRegex(prodctUrl, regexSelectorTitle, splitSymol, splitIndexTitle);
            var values = GetPageDataByUrlAndRegex(prodctUrl, regexSelectorValue, splitSymolValue, splitIndexValue);

            for(int i = 0; i < titles.Count; i++)
            {
                var key = titles[i].Split(":")[1] ?? titles[i];
                var value = values[i].Split("href")[0] ?? values[i];
                result.Add(key, value);
            }

            return result;
        }

        private List<string> GetPageDataByUrlAndSelector(string url, string selector, string attributeName)
        {
            var document = _htmlWeb.Load(url);
            var result = document.DocumentNode.SelectNodes(selector)
                .Select(node => node.Attributes.Where(attribute => attribute.Name == attributeName).FirstOrDefault().Value).ToList();
            
            return result;
        }

        private List<string> GetPageDataByUrlAndRegex(string url, string selector, char splitSymbol, int splitIndex)
        {
            var regex = new Regex(selector);
            var document = _htmlWeb.Load(url);
            var data = regex.Matches(document.DocumentNode.OuterHtml.Replace("&q;", ""));
            var result = data.Select(url => url.Value.Split(splitSymbol)[splitIndex]).ToList();

            return result;
        }

        private string GetNextPage(string url, string selector, out bool isHaveNextPage)
        {
            var regex = new Regex(selector);
            var document = _htmlWeb.Load(url);
            var nextPageUrl = regex.Match(document.DocumentNode.OuterHtml.Replace("&q;", ""));
            var result = "";

            isHaveNextPage = false;

            if (nextPageUrl.Length > 0)
            {
                result = nextPageUrl.Value.Split("\"")[2].Trim();
                isHaveNextPage = true;
            }

            return result;
        }

        private List<string> GetAllCategories()
        {
            var mainCategoriesUrls = GetSiteCategoriesUrls();
            var allCategoriesUrls = new List<string>();

            foreach (var categoryUrl in mainCategoriesUrls)
            {
                var categoriesUrls = GetSubCategoriesUrls(categoryUrl);
                allCategoriesUrls.AddRange(categoriesUrls);
            }

            return allCategoriesUrls;
        }
    }
}
