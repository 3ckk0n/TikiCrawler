using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;

namespace TikiCrawler2024
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create an instance of Chrome driver with Brave browser
            var options = new ChromeOptions();
            options.BinaryLocation = @"C:\\Program Files\\BraveSoftware\\Brave-Browser\\Application\\brave.exe";
            options.AddArgument("--remote-debugging-port=9222");
            IWebDriver browser = new ChromeDriver(options);

            // Navigate to website
            browser.Navigate().GoToUrl("https://tiki.vn/laptop-may-vi-tinh-linh-kien/c1846");
            Thread.Sleep(5000);

            // Dynamically load more products by clicking "Show more"
            int targetProductCount = 100;
            int previousProductCount = 0;
            int retries = 0;

            while (true)
            {
                var products = browser.FindElements(By.CssSelector(".product-item"));
                Console.WriteLine($"Loaded: {products.Count}");

                if (products.Count >= targetProductCount)
                    break;

                if (products.Count == previousProductCount)
                {
                    retries++;
                    if (retries > 3)
                    {
                        Console.WriteLine("No new products loading. Breaking out.");
                        break;
                    }
                }
                else
                {
                    retries = 0;
                    previousProductCount = products.Count;
                }

                try
                {
                    var showMoreButton = browser.FindElement(By.CssSelector("div[data-view-id='category_infinity_view.more']"));
                    ((IJavaScriptExecutor)browser).ExecuteScript("arguments[0].scrollIntoView(true);", showMoreButton);
                    Thread.Sleep(500);
                    showMoreButton.Click();
                    Console.WriteLine("Clicked 'Show more'");
                    Thread.Sleep(3000);
                }
                catch (NoSuchElementException)
                {
                    Console.WriteLine("'Show more' button not found.");
                    break;
                }
                catch (ElementClickInterceptedException)
                {
                    Console.WriteLine("Click blocked. Retrying...");
                    Thread.Sleep(2000);
                }
            }

            // Extract product links
            List<string> listProductLink = new List<string>();
            var loadedProducts = browser.FindElements(By.CssSelector(".product-item"));

            foreach (var product in loadedProducts)
            {
                if (listProductLink.Count >= targetProductCount)
                    break;

                string outerHtml = product.GetAttribute("outerHTML");
                string productLink = Regex.Match(outerHtml, "href=\"(.*?)\"").Groups[1].Value;
                productLink = "https://tiki.vn" + productLink;

                if (productLink.Contains("pixel"))
                    continue;

                if (!listProductLink.Contains(productLink))
                    listProductLink.Add(productLink);
            }

            // Crawl product details
            List<Product> listProduct = new List<Product>();
            foreach (string link in listProductLink)
            {
                Product product = new Product();
                browser.Navigate().GoToUrl(link);
                Thread.Sleep(2000);

                try
                {
                    string productName = browser.FindElement(By.CssSelector("h1.sc-c0f8c612-0")).Text;
                    product.Name = productName;
                }
                catch { product.Name = ""; }

                try
                {
                    string productBrand = browser.FindElement(By.CssSelector(".brand-and-author")).GetAttribute("outerHTML");
                    productBrand = Regex.Match(productBrand, "pdp_details_view_brand.*?>(.*?)</a>").Groups[1].Value;
                    product.Brand = productBrand;
                }
                catch { product.Brand = ""; }

                try
                {
                    string productPrice = browser.FindElement(By.CssSelector(".product-price__current-price")).GetAttribute("outerHTML");
                    productPrice = Regex.Match(productPrice, ">(.*?)<sup>").Groups[1].Value;
                    product.Price = productPrice;
                }
                catch { product.Price = ""; }

                product.Link = link;
                listProduct.Add(product);
            }

            // Write to file
            Console.WriteLine($"Total products crawled: {listProduct.Count}");
            System.IO.StreamWriter writer = new System.IO.StreamWriter("D:\\tiki.csv", false, System.Text.Encoding.UTF8);
            writer.WriteLine("ProductName\tProductBrand\tProductPrice\tProductLink");
            foreach (Product product in listProduct)
            {
                writer.WriteLine(product.Name + "\t" + product.Brand + "\t" + product.Price + "\t" + product.Link);
            }
            writer.Close();
        }
    }
}