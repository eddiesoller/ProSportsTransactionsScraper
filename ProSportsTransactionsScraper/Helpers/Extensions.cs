using System;
using System.Net;
using System.Data;
using System.Linq;
using HtmlAgilityPack;

namespace ProSportsTransactionsScraper.Helpers
{
    /// <summary>
    /// Extension helper methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds a collection of transaction HtmlNodes as DataRows.
        /// </summary>
        /// <param name="rows">the DataRowCollection to add to</param>
        /// <param name="htmlNodes">the transaction nodes to add</param>
        public static void AddRange(this DataRowCollection rows, HtmlNodeCollection htmlNodes)
        {
            foreach (HtmlNode htmlNode in htmlNodes)
            {
                string[] data = htmlNode.Descendants("td")
                    .Select(n => string.Join('|', WebUtility.HtmlDecode(n.InnerText)
                        .Trim()
                        .Split('•', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())))
                    .ToArray();
                rows.Add(data);
            }
        }
    }
}
