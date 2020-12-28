using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using GreekHoaxes.Models;
using Newtonsoft.Json;
using RestSharp;

namespace GreekHoaxes.ArticleExtractor
{
    class Program
    {
        public static readonly string GreekHoaxesArticlesUrl = "https://www.ellinikahoaxes.gr/category/kathgories";

        [STAThread]
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            var firstPageNumber = await GetPaginationFirstPage();
            var lastPageNumber = await GetPaginationLastPage();

            IList<Article> articles = null;

            if (!File.Exists("articles.json"))
            {
                // Read articles titles and short description
                articles = await ReadAllArticles(firstPageNumber, lastPageNumber);
                var resultString = JsonConvert.SerializeObject(articles);
                File.WriteAllText(@"articles.json", resultString);
            }
            else
            {
                var articlesFromFile = File.ReadAllText("articles.json");
                articles = JsonConvert.DeserializeObject<List<Article>>(articlesFromFile);
            }

            if (!File.Exists("articles_decorated.json"))
            {
                // Read article body, image and full description
                articles = await DecorateArticles(articles);
                var decoratedResultString = JsonConvert.SerializeObject(articles);
                File.WriteAllText(@"articles_decorated.json", decoratedResultString);
            }
            else
            {
                var decoratedArticlesFromFile = File.ReadAllText("articles_decorated.json");
                articles = JsonConvert.DeserializeObject<List<Article>>(decoratedArticlesFromFile);
            }
        }

        public static async Task<int> GetPaginationFirstPage()
        {
            var config = Configuration.Default.WithDefaultLoader();

            var document = await BrowsingContext.New(config).OpenAsync(GreekHoaxesArticlesUrl);

            var paginationCssSelector = ".pagination-list .page-numbers.current";

            var cells = document.QuerySelectorAll(paginationCssSelector);

            return Int32.Parse(cells.FirstOrDefault()?.InnerHtml);
        }

        public static async Task<int> GetPaginationLastPage()
        {
            var config = Configuration.Default.WithDefaultLoader();

            var document = await BrowsingContext.New(config).OpenAsync(GreekHoaxesArticlesUrl);

            var paginationCssSelector = ".pagination-list .page-numbers";

            var cells = document.QuerySelectorAll(paginationCssSelector);

            return Int32.Parse(cells[cells.Length -2]?.InnerHtml);
        }

        public static async Task<IList<Article>> ReadAllArticles(int start, int end)
        {
            var articles = new List<Article>();
            Random random = new Random();

            for (int i = start; i < end; i++)
            {
                var currentPageUrl = $"{GreekHoaxesArticlesUrl}/page/{i}";
                articles.AddRange(await ReadPageArticles(currentPageUrl));
                var randomWait = random.Next(600, 900);
                Thread.Sleep(randomWait);
            }

            return articles;
        }

        public static async Task<IList<Article>> ReadPageArticles(string url)
        {
            var articles = new List<Article>();

            var config = Configuration.Default.WithDefaultLoader();

            var document = await BrowsingContext.New(config).OpenAsync(url);

            var articleSelector = ".white-row div.post";

            var cells = document.QuerySelectorAll(articleSelector);

            foreach(var cell in cells)
            {
                articles.Add(ReadArticle(cell));
            }

            return articles;
        }

        public static Article ReadArticle(IElement element)
        {
            var article = new Article();

            var id = element.Id;
            var tags = GetPartialWordMatch("tag", element.ClassList);
            var categories = GetPartialWordMatch("category", element.ClassList);
            var url = element.QuerySelector("a");
            var hasPostThumbnail = GetPartialWordMatch("has-post-thumbnail", element.ClassList).Count > 0;

            article.PostId = id;
            article.Tags = tags.ToList();
            article.Categories = categories.ToList();
            article.Url = url.GetAttribute("href");
            article.HasPostThumbnail = hasPostThumbnail;

            return article;
        }

        public static IList<string> GetPartialWordMatch(string wordToPartiallyMatch, ITokenList tokenList)
        {
            var strings = new List<string>();

            var rx = new Regex(@$"{wordToPartiallyMatch}.");

            foreach (var x in tokenList) {
                var matches = rx.Matches(x);
                if (matches != null && matches.Count > 0)
                {
                    strings.Add(x);
                }
            }

            return strings;
        }

        public static bool FindPartialWordMatch(string wordToPartiallyMatch, string holeText)
        {
            var strings = new List<string>();

            var rx = new Regex(@$"{wordToPartiallyMatch}.");
          
            var matches = rx.Matches(holeText);

            if (matches != null && matches.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static async Task<IList<Article>> DecorateArticles(IList<Article> articles)
        {
            Random random = new Random();
            int currentPosition = 0;
            int articlesCount = articles.Count;

            foreach (var article in articles) {
                Console.WriteLine($"Processing article {currentPosition} of {articlesCount}.");
                await DecorateSingleArticle(article);
                var randomWait = random.Next(200, 400);
                Thread.Sleep(randomWait);
                currentPosition++;
            }

            return articles;
        }

        public static async Task<Article> DecorateSingleArticle(Article article)
        {
            var config = Configuration.Default.WithDefaultLoader();

            IDocument document = await BrowsingContext.New(config).OpenAsync(article.Url);

            try
            {
                string pattern = @"\d{4}\/\d{2}\/\d{2}";
                Match match = Regex.Match(article.Url, pattern, RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    string dateString = match.Groups[0].Value;
                    DateTime date = DateTime.Parse(dateString);
                    article.ExactPublishDate = date;
                }
            }
            catch (Exception) { }



            var claimCssSelector = ".Claim >p";
            var authorCssSelector = "a.author";
            var blogDateCssSelector = ".blog-date";
            var conclusionCssSelector = ".conclusion >p";
            var articleCssSelector = ".main-content-body .type-post";
            var allLinksSelector = ".main-content-body .type-post a";

            var claimsResults = document.QuerySelectorAll(claimCssSelector);
            var conclusionResults = document.QuerySelectorAll(conclusionCssSelector);
            var articleResult = document.QuerySelector(articleCssSelector);
            var authorResult = document.QuerySelector(authorCssSelector);
            var dateResult = document.QuerySelector(authorCssSelector);
            var blogDateResult = document.QuerySelector(blogDateCssSelector);
            var modifiedDateTime  = document.Head.QuerySelectorAll("meta").FirstOrDefault(x=> FindPartialWordMatch("article:modified_time", x.OuterHtml));
            var publishedDateTime  = document.Head.QuerySelectorAll("meta").FirstOrDefault(x=> FindPartialWordMatch("article:published_time", x.OuterHtml));
            var articleTitleResult = document.Head.QuerySelectorAll("meta").FirstOrDefault(x => FindPartialWordMatch("og:title", x.OuterHtml));
            var imageUrlResult = document.Head.QuerySelectorAll("meta").FirstOrDefault(x => FindPartialWordMatch("og:image", x.OuterHtml));
            var descriptionUrlResult = document.Head.QuerySelectorAll("meta").FirstOrDefault(x => FindPartialWordMatch("og:description", x.OuterHtml));
            var allLinks = document.QuerySelectorAll(allLinksSelector);
            var sourceLinks = allLinks.Where(x => x.InnerHtml?.ToLower() == "πηγή");
            var restOfTheLinks = allLinks.Where(x => x.InnerHtml?.ToLower() != "πηγή");

            article.Author = authorResult?.InnerHtml;
            article.Claim = claimsResults?.Select(x => x.InnerHtml).ToList();
            article.Result = conclusionResults?.Select(x => x.InnerHtml).ToList();
            article.RawArticleHtml = articleResult?.InnerHtml;
            article.RawText = articleResult?.Text();
            article.PublishDate= blogDateResult?.InnerHtml;
            DateTime.TryParse(modifiedDateTime?.GetAttribute("Content"), out var modifiedDateValue);
            DateTime.TryParse(publishedDateTime?.GetAttribute("Content"), out var publishedDateValue);
            article.ModifiedDate = modifiedDateValue;
            article.ExactPublishDate = publishedDateValue;
            article.GreekHoaxTitle = articleTitleResult?.GetAttribute("Content");
            article.ImageUrl = imageUrlResult?.GetAttribute("Content");
            article.GreekHoaxDescription = descriptionUrlResult?.GetAttribute("Content");
            article.ProofLinks = sourceLinks?.Select(x => x.GetAttribute("href")).ToList();
            article.HoaxSourceLinks = restOfTheLinks?.Select(x => x.GetAttribute("href")).ToList();
            return article;
        }
    }  
}
