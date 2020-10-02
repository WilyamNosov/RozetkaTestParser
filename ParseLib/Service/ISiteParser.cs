using System;
using System.Collections.Generic;
using System.Text;

namespace ParseLib.Service
{
    public interface ISiteParser
    {
        List<string> GetSiteCategoriesUrls();
        List<string> GetSubCategoriesUrls(string mainCategoryUrl);
        List<string> GetProductsUrlsFromCategory(string url, bool isHaveNextPage = true);
        List<Dictionary<string, string>> GetItemsData(List<string> productsUrls);
        Dictionary<string, string> GetItemCharacteristic(string prudctUrl);
    }
}
