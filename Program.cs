using System;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.IO;
using System.Globalization;
using HtmlAgilityPack;

namespace MyApp // Note: actual namespace depends on the project name.
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Please enter csv path");
            var filePath = Console.ReadLine();
            while (filePath == null || filePath.Equals(""))
            {
                Console.WriteLine("Please enter correct path");
                filePath = Console.ReadLine();
            }

            var items = loadItemsFromCsv(filePath);

            Parallel.ForEach(items, item =>
            {
                var url = String.Format("https://www.emag.bg/search/{0}?ref=trending", item);
                foreach (var res in fetchItemFromEmag(url))
                {
                    Console.WriteLine(String.Format("Product: {0} || Price: {1:0.##}", res.Key, res.Value));
                }
                Console.WriteLine();
            });
        }

        public static List<string> loadItemsFromCsv(string path)
        {
            var records = new List<ProductInfo>();

            try
            {
                using (var streamReader = new StreamReader(String.Format(@"{0}\product_list.csv", path)))
                {

                    using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
                    {
                        records = csvReader.GetRecords<ProductInfo>().ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return records.Select(r => r.Name).ToList();
        }

        public static Dictionary<string, string> fetchItemFromEmag(string url)
        {
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(url);
            var productCards = htmlDoc.DocumentNode.SelectNodes("//*[@id=\"card_grid\"]/div");

            var products = productCards
                .Select(product => product.Attributes["data-name"]?.Value)
                .Where(product => product != null)
                .ToList();

            var prices = productCards[0].SelectNodes("//p[@class='product-new-price']")
                .Select(p => p.InnerText.Trim().Replace("&#46;", "").Replace("лв.", ""))
                .Where(p => !p.Equals("") && p != null)
                .Select(p => String.Format("{0},{1}BGN", p.Substring(0, p.Length - 3), p.Substring(p.Length - 3)))
                .ToList();

            var items = new Dictionary<string, string>();

            for (int i = 0; i < products.Count(); i++)
            {
                if (!items.ContainsKey(products[i] ??= ""))
                {
                    items.Add(products[i] ??= "", prices[i]);
                }

            }
            return items;
        }

    }

    public class ProductInfo
    {
        [Name("Product Name")]
        public string Name { get; set; }
    }
}