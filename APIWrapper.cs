using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows;

namespace ForexTradesimulator
{
    public class APIWrapper
    {

        public  Dictionary<DateTime, CandleStick> outputDict = new Dictionary<DateTime, CandleStick>();
        public FetcherParams workingParams = new FetcherParams();
        /// <summary>
        /// This simplefetcher takes a few inputs as fetcherParams, creates the right unique url for the params.
        /// Then it processes the web-request and obtains the response via a streamreader
        /// the json will be processed by our own personal Deserializer :D
        /// </summary>
        /// <param name="fetcherParams"></param>
        /// <returns>A Dictionairy filled with candlestick objects according to the fetcherParams</returns>
        public Dictionary<DateTime, CandleStick> DictionaryFetcher(FetcherParams fetcherParams)
        {
                string url = "";
            try
            {
                workingParams = fetcherParams;

                
                string startString = workingParams.start.Replace(":","%3A");
                string endString = workingParams.end.Replace(":", "%3A");
                // We have a few unique situations
                // Situation where we have no end date, but a start date.
                if ((workingParams.end == "") && (workingParams.start != ""))
                {
                    url = String.Format("https://api-fxtrade.oanda.com/v1/candles?instrument=" + workingParams.instrument+"&count=" + workingParams.count + "&start=" + startString
                        + "&granularity=" + workingParams.granularity + "&dailyAlignment=" + workingParams.dailyAlignment);
                }

                // Situation where we have end-date but no start date
                if ((workingParams.end != "") && (workingParams.start == ""))
                {
                    url = String.Format("https://api-fxtrade.oanda.com/v1/candles?instrument=" + workingParams.instrument + "&count=" + workingParams.count + "&end=" + endString
                        + "&granularity=" + workingParams.granularity + "&dailyAlignment=" + workingParams.dailyAlignment);
                }

                // Situation where we have a start and end date (so no count is possible)
                if ((workingParams.end != "") && (workingParams.start != ""))
                {
                    url = String.Format("https://api-fxtrade.oanda.com/v1/candles?instrument=" + workingParams.instrument + "&start=" + startString + "&end=" + endString
                        + "&granularity=" + workingParams.granularity + "&dailyAlignment=" + workingParams.dailyAlignment);
                }
                // Situation where have no start date and end date (so count from last candle with a possible non completion).
                if ((workingParams.end == "") && (workingParams.start == ""))
                {
                    url = String.Format("https://api-fxtrade.oanda.com/v1/candles?instrument=" + workingParams.instrument + "&count=" + workingParams.count
                        + "&granularity=" + workingParams.granularity + "&dailyAlignment=" + workingParams.dailyAlignment );
                }
                // + "&weeklyAlignment=" + workingParams.weeklyAlignment
                // + "&dailyAlignment=" + workingParams.dailyAlignment + "&alignmentTimezone=" + workingParams.alignmentTimezone
                        
                var request = (HttpWebRequest)WebRequest.Create(url);

                string json = "";
                string credentialHeader = String.Format("Bearer **PERSONAL TOKEN**"); // insert your personal token here 
                request.Method = "GET";
                request.ContentType = "application/json";
                request.Headers.Add("Authorization", credentialHeader);

                HttpWebResponse webresponse = (HttpWebResponse)request.GetResponse();

                var sw = new StreamReader(webresponse.GetResponseStream(), System.Text.Encoding.ASCII);
                json = sw.ReadToEnd();
                sw.Close();
                var response = CandlestickDeserializer(json);
                return response;

            }
            catch (Exception e) 
            {
                
                if (e.Message.Contains("Bad Request"))
                {
                    MessageBox.Show("Exception: " + e.Message + "\n" +
                    "Request url: " +url+ "\n" + "Possible causes: \n" +
                        "More candles requested than possible for the timespan." + "\n" +
                        "1. {If start + candles*count date is greater than curr date. }\n" +
                        "Invalid combination of parameters" + "\n" +
                        "1. { }");
                    // try to properly catch as many exceptions within the {}.

                }
                else
                {

                }
                return null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"> this parameter will contain the json format from the SimpleFetcher method</param>
        /// <returns>the dictionary with candlestick objects that is later passed through SimpleFetcher method</returns>
        public Dictionary<DateTime, CandleStick> CandlestickDeserializer(string json)
        {
            DateTime fetchedDateTime = new DateTime();
            string output = json;
            
            //  MessageBox.Show("string length = " + output.Length);
            output = output.Replace("\t", String.Empty);
            output = output.Replace(" ", String.Empty);
            output = output.Replace(",", String.Empty);
            output = output.Replace("\"", String.Empty);
            output = output.Replace("time:", String.Empty);
            output = output.Replace("openBid:", String.Empty);
            output = output.Replace("openAsk:", String.Empty);
            output = output.Replace("highBid:", String.Empty);
            output = output.Replace("highAsk:", String.Empty);
            output = output.Replace("lowBid:", String.Empty);
            output = output.Replace("lowAsk:", String.Empty);
            output = output.Replace("closeBid:", String.Empty);
            output = output.Replace("closeAsk:", String.Empty);
            output = output.Replace("volume:", String.Empty);
            output = output.Replace("complete:", String.Empty);
            //output = output.Replace("{", String.Empty);
            //output = output.Replace("}", String.Empty);

            List<string> outputList = output.Split('\n').ToList();
            //for (int i = 0; i < 4; i++)
            //{
            //    outputList.RemoveAt(i);
            //}
            int arrayOffset = 13;
            outputDict.Clear();
            for (int i = 0; i < workingParams.count; i++) // the i max must be dynamically programmed, to correlated to the size of the output array (length is (outputarray.length - 6)/13
            {
                CandleStick currCandle = new CandleStick();
                
                currCandle.time = DateTime.Parse(outputList[5 + i * arrayOffset]);
                currCandle.openBid = double.Parse(outputList[6 + i * arrayOffset]);
                currCandle.openAsk = double.Parse(outputList[7 + i * arrayOffset]);
                currCandle.highBid = double.Parse(outputList[8 + i * arrayOffset]);
                currCandle.highAsk = double.Parse(outputList[9 + i * arrayOffset]);
                currCandle.lowBid = double.Parse(outputList[10 + i * arrayOffset]);
                currCandle.lowAsk = double.Parse(outputList[11 + i * arrayOffset]);
                currCandle.closeBid = double.Parse(outputList[12 + i * arrayOffset]);
                currCandle.closeAsk = double.Parse(outputList[13 + i * arrayOffset]);
                currCandle.volume = double.Parse(outputList[14 + i * arrayOffset]);
                currCandle.complete = bool.Parse(outputList[15 + i * arrayOffset]);

                fetchedDateTime = currCandle.time;

                outputDict.Add(fetchedDateTime,currCandle);

            }


            return outputDict;
        }

    }
    public class CandleStick
    {
        public DateTime time { get; set; }
        public double openBid { get; set; }
        public double openAsk { get; set; }
        public double highBid { get; set; }
        public double highAsk { get; set; }
        public double lowBid { get; set; }
        public double lowAsk { get; set; }
        public double closeBid { get; set; }
        public double closeAsk { get; set; }
        public double volume { get; set; }
        public bool complete { get; set; }
    }
    public class FetcherParams
    {
        public string instrument { get; set; }
        public string granularity { get; set; }
        // Create a hoover-over functionality to show which input query's are allowed. If you input a wrong one, it will throw an exception with the message so.
        public int count { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public int dailyAlignment { get; set; }
        // The hour of day used to align candles with hourly, daily, weekly, or monthly granularity. 
        // The value specified is interpretted as an hour in the timezone set through the alignmentTimezone parameter and must be an integer between 0 and 23.
        // The default for dailyAlignment is 21 when Eastern Daylight Time is in effect and 22 when Eastern Standard Time is in effect. This corresponds to 17:00 local time in New York.
        public string alignmentTimezone { get; set; }
        // The timezone to be used for the dailyAlignment parameter. 
        // This parameter does NOT affect the returned timestamp, the start or end parameters, these will always be in UTC. 
        // http://developer.oanda.com/docs/timezones.txt for list of possible time zones. "America/New_York" is standard
        public string weeklyAlignment { get; set; }
        public int numCandleSets { get; set; }




    }
            
}
