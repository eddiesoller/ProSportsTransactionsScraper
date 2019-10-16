using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using NLog;
using HtmlAgilityPack;
using ProSportsTransactionsScraper.Helpers;

namespace ProSportsTransactionsScraper
{
    /// <summary>
    /// Main program.
    /// </summary>
    public static class Program
    {
        private static readonly Logger m_Logger = LogManager.GetCurrentClassLogger();
        private static readonly Version m_Version = Assembly.GetExecutingAssembly().GetName().Version;
        private static HtmlWeb m_Web = new HtmlWeb();
        private static HtmlDocument m_Doc;
        private static DataTable m_DataTable = new DataTable();
        private const string BASE_URL = "http://prosportstransactions.com/basketball/Search/SearchResults.php?Player=&Team=&BeginDate=&EndDate=&PlayerMovementChkBx=yes&Submit=Search&start=";
        private const string TRANSACTION_TABLE_HEADER_XPATH = "//table[@class='datatable center']/tr";
        private const string TRANSACTION_NODES_XPATH = "//table[@class='datatable center']/tr[@align='left']";
        private const int TRANSACTIONS_PER_PAGE = 25;
        private static readonly TimeSpan CRAWL_DELAY = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Entry point.
        /// </summary>
        private static void Main()
        {
            try
            {
                m_Logger.Info($"BEGIN - Version: {m_Version}");
                PrepareDataTable();
                GetTransactions();
            }
            catch (Exception ex)
            {
                m_Logger.Error(ex);
            }
            finally
            {
                m_Logger.Info("END");
            }
        }

        /// <summary>
        /// Prepares the DataTable member with columns matching the scraping source.
        /// </summary>
        private static void PrepareDataTable()
        {
            m_Doc = m_Web.Load(BASE_URL);
            HtmlNode headerRowNode = m_Doc.DocumentNode.SelectSingleNode(TRANSACTION_TABLE_HEADER_XPATH);

            foreach (HtmlNode columnNode in headerRowNode.Descendants("td"))
            {
                m_DataTable.Columns.Add(WebUtility.HtmlDecode(columnNode.InnerText).Trim());
            }
        }

        /// <summary>
        /// Scrapes and parses transactions.
        /// </summary>
        private static void GetTransactions()
        {
            Stopwatch crawlStopwatch = new Stopwatch();
            HtmlNodeCollection pageTransactionNodes;
            int startIndex = 0;

            while (true)
            {
                try
                {
                    crawlStopwatch.Reset();
                    crawlStopwatch.Start();
                    pageTransactionNodes = ScrapePageTransactions(startIndex);

                    if (pageTransactionNodes?.Count > 0)
                    {
                        m_DataTable.Rows.AddRange(pageTransactionNodes);
                        WriteCsv();

                        if (pageTransactionNodes.Count < TRANSACTIONS_PER_PAGE)
                        {
                            break;
                        }
                        else
                        {
                            startIndex += TRANSACTIONS_PER_PAGE;
                            crawlStopwatch.Stop();

                            if (crawlStopwatch.Elapsed < CRAWL_DELAY)
                            {
                                Thread.Sleep(CRAWL_DELAY - crawlStopwatch.Elapsed);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    m_Logger.Error(ex);
                }
            }
        }

        /// <summary>
        /// Scrapes the transactions from a page.
        /// </summary>
        /// <param name="startIndex">the starting index used to target the web page containing transactions</param>
        /// <returns>the scraped transactions as a HtmlNodeCollection</returns>
        private static HtmlNodeCollection ScrapePageTransactions(int startIndex)
        {
            HtmlNodeCollection pageTransactionNodes;

            m_Logger.Info($"Scraping transactions from start index {startIndex}");

            m_Doc = m_Web.Load(BASE_URL + startIndex);
            pageTransactionNodes = m_Doc.DocumentNode.SelectNodes(TRANSACTION_NODES_XPATH);

            m_Logger.Info("Transactions scraped");

            return pageTransactionNodes;
        }

        /// <summary>
        /// Writes the CSV output file with the contents of the DataTable member.
        /// </summary>
        private static void WriteCsv()
        {
            StringBuilder sb = new StringBuilder();

            m_Logger.Info("Writing output CSV");

            IEnumerable<string> columnNames = m_DataTable
                .Columns
                .Cast<DataColumn>()
                .Select(column => column.ColumnName);

            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in m_DataTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray
                    .Select(field => field.ToString().Contains(",") ? $"\"{field}\"" : field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText("Output.csv", sb.ToString());

            m_Logger.Info("CSV file written");
        }
    }
}