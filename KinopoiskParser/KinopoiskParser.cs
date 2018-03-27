using HtmlAgilityPack;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace KinopoiskParser
{
    public class KinopoiskParser
    {
        public string websiteRoot { get; set; }
        public string websiteSection { get; set; }
        public string startPage { get; set; }
        public string endPage { get; set; }
        public string directoryPath { get; set; }
        private IWebDriver chromeDriver;

        public KinopoiskParser(string websiteRoot, string websiteSection, string startPage, string endPage, string directoryPath)
        {
            this.websiteRoot = websiteRoot;
            this.websiteSection = websiteSection;
            this.startPage = startPage;
            this.endPage = endPage;
            this.directoryPath = directoryPath;
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments(new List<string>() {"headless"});
            chromeDriver = new ChromeDriver(chromeOptions);
        }

        private void clearfix(ref string valueTemp)
        {
            valueTemp = valueTemp.Trim();
            valueTemp = Regex.Replace(valueTemp, @"\s+", " ");
            List<string> htmlGarbage = new List<string>() { "&laquo;", ", &laquo;", "&nbsp;", ", &nbsp;", "&raquo;", ", &raquo;", ", ...", "&#151;" };
            foreach (string waste in htmlGarbage)
            {
                if (valueTemp.LastIndexOf(waste) > -1)
                {
                    valueTemp = valueTemp.Replace(waste, " ");
                }
            }
            valueTemp = Regex.Replace(valueTemp, @"\s+", " ");
        }

        private string get_page_url(string page)
        {
            string endPageUrl = "page/" + page + "/#results";
            string url = websiteSection.Replace("#results", endPageUrl);
            return url;
        }

        private string clear_film_name(string name)
        {
            string filmName = name;
            if (filmName.IndexOf(": ") > -1) { filmName = filmName.Replace(": ", " "); }
            if (filmName.IndexOf(", ") > -1) { filmName = filmName.Replace(", ", " "); }
            if (filmName.IndexOf("?") > -1) { filmName = filmName.Replace("?", " "); }
            return filmName;
        }

        private void load_data_to_file(string filmPathTemp, string filePathTemp)
        {
            string filmPage = websiteRoot + filmPathTemp;
            string filePath = filePathTemp;

            chromeDriver.Url = filmPage;
            Thread.Sleep(60000);
            string pageText = chromeDriver.PageSource;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(pageText);

            HtmlNode tableNode = htmlDoc.DocumentNode.SelectSingleNode("//body/div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]//div[contains(@id,'viewFilmInfoWrapper')]//div[contains(@id,'photoInfoTable')]/div[contains(@id,'infoTable')]/table[@class='info']");

            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.Default))
            {
                // основная таблица
                writer.WriteLine("ДАННЫЕ");
                foreach (HtmlNode trNode in tableNode.Descendants("tr"))
                {
                    if (trNode.NodeType == HtmlNodeType.Element)
                    {
                        List<HtmlNode> tdCollection = new List<HtmlNode>();
                        tdCollection = trNode.Descendants("td").ToList();
                        if (tdCollection[1].SelectSingleNode(".//a[@class='wordLinks']") != null)
                        {
                            HtmlNode toDelete = tdCollection[1].SelectSingleNode(".//a[@class='wordLinks']");
                            toDelete.Remove();
                        }
                        if (tdCollection[1].SelectSingleNode(".//script") != null)
                        {
                            HtmlNode toDelete = tdCollection[1].SelectSingleNode(".//script");
                            toDelete.Remove();
                        }
                        string characteristic = tdCollection[0].InnerText;
                        string value = tdCollection[1].InnerText;
                        clearfix(ref characteristic);
                        clearfix(ref value);
                        writer.WriteLine("{0}: {1}", characteristic, value);
                    }
                }
                tableNode = htmlDoc.DocumentNode.SelectSingleNode("//body/div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]/table//table//table//tr/td/form/div[contains(@id,'block_rating')]/div[@class='block_2']/div[2]");
                string imdB = tableNode.InnerText;
                clearfix(ref imdB);
                writer.WriteLine("{0}", imdB);
                // сценарий фильма
                writer.WriteLine();
                writer.WriteLine("СЮЖЕТ");
                HtmlNode descriptionNode = htmlDoc.DocumentNode.SelectSingleNode("//body/div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]/table/tbody//table/tbody//table/tbody/tr/td/span/div[contains(concat(' ', @class, ' '), 'brand_words') and contains(@class, 'film-synopsys')]");
                string description = descriptionNode.InnerText;
                clearfix(ref description);
                writer.WriteLine("{0}", description);
                // список актеров
                writer.WriteLine();
                writer.WriteLine("AКТЕР - ДУБЛЕР");
                string actorsPage = websiteRoot + htmlDoc.DocumentNode.SelectSingleNode("//body/div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]//div[contains(@id,'viewFilmInfoWrapper')]//div[contains(@id,'photoInfoTable')]/div[contains(@id,'actorList')]/h4/a").Attributes["href"].Value;                
                chromeDriver.Url = actorsPage;
                Thread.Sleep(60000);                
                htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(chromeDriver.PageSource);
                HtmlNodeCollection actorNodes = htmlDoc.DocumentNode.SelectNodes("//body/div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]//div[@class='block_left']/div[contains(concat(' ', @class, ' '), 'dub')][position() > 1][position() <= 77]");
                foreach (HtmlNode actorNode in actorNodes)
                {
                    string actor = actorNode.SelectSingleNode(".//div[@class='actorInfo']//div[@class='info']/div[@class='name']/a").InnerText;
                    writer.Write("{0} - ", actor);
                    if (actorNode.SelectSingleNode(".//div[@class='dubInfo']") != null)
                    {
                        string actorDubler = actorNode.SelectSingleNode(".//div[@class='dubInfo']//div[@class='name']/a").InnerText;
                        writer.WriteLine("{0}", actorDubler);
                    }
                    else { writer.WriteLine(); }
                }
            }
        }

        public void parse()
        {
            for (int page = int.Parse(startPage); page < int.Parse(endPage); page++)
            {
                chromeDriver.Url = get_page_url(page.ToString());
                string pageText = chromeDriver.PageSource;
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(pageText);

                HtmlNodeCollection filmsNodes = htmlDoc.DocumentNode.SelectNodes("//body//div[contains(concat(' ', @class, ' '), 'shadow') and contains(@class, 'content')]/div[contains(@id,'content_block')]/table/tbody/tr/td[contains(@id,'block_left')]/div[contains(@id,'results')]/table/tbody/tr[3]/td/div[contains(@id,'itemList')]/div[@class='item _NO_HIGHLIGHT_']");
                foreach (HtmlNode filmNode in filmsNodes)
                {
                    string filmName = filmNode.SelectSingleNode("./div[@class = 'info']/div[@class = 'name']/a").InnerText;
                    filmName = clear_film_name(filmName);

                    string filmPathOnSite = filmNode.SelectSingleNode("./div[@class = 'info']/div[@class = 'name']/a").Attributes["href"].Value;
                    string filePath = directoryPath + "\\" + filmName + ".txt";
                    load_data_to_file(filmPathOnSite, filePath);
                }
            }
        }
    }
}
