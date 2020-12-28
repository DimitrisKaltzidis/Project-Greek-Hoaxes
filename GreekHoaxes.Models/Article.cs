using System;
using System.Collections.Generic;

namespace GreekHoaxes.Models
{
    public class Article
    {
        public Article()
        {
        }

        public string PostId { get; set; }
        public bool HasPostThumbnail { get; set; }
        public string GreekHoaxTitle { get; set; }
        public string GreekHoaxDescription { get; set; }
        public string RawText { get; set; }
        public string RawArticleHtml { get; set; }
        public DateTime ExactPublishDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ImageUrl { get; set; }
        public string PublishDate { get; set; }
        public string Author { get; set; }
        public IList<string> Claim { get; set; }
        public IList<string> Result { get; set; }
        public string Url { get; set; }
        public IList<string> Tags { get; set; }
        public IList<string> Categories { get; set; }
        public IList<string> HoaxSourceLinks { get; set; }
        public IList<string> ProofLinks { get; set; }
    }
}
