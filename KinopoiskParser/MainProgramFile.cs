using System;

namespace KinopoiskParser
{
    class MainProgramFile
    {
        static void Main()
        {
            string websiteRoot = "https://www.kinopoisk.ru";
            string websiteSection = "https://www.kinopoisk.ru/top/navigator/m_act%5Bgenre%5D/3/m_act%5Bis_film%5D/on/order/rating/#results";
            string startPage = "1";
            string endPage = "3";
            string directoryPath = "";

            KinopoiskParser myParser = new KinopoiskParser(websiteRoot, websiteSection, startPage, endPage, directoryPath);
            myParser.parse();

            Console.WriteLine("Готово!");
            Console.ReadLine();
        }
    }
}
