using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web;

namespace ArmaServerManager
{
    public class HttpServer
    {
        private Settings settings;

        public HttpServer(Settings s)
        {
            settings = s;
        }
        public void Listen()
        {

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://*:{0}/", settings.ManagerPort));
            listener.Start();



            //srv1.serverData.Schedules.ServerEvents.Add(new ScheduledEvent("Test Event", DateTime.Now, EventType.START, ScheduleType.Interval, 10));
            //srv1.serverData.Schedules.ServerEvents.Add(new ScheduledEvent("Test Event2", DateTime.Now.AddSeconds(5), EventType.START, ScheduleType.Once));

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                StreamReader r = new StreamReader(request.InputStream);

                string val = r.ReadToEnd();
                r.Close();

                Console.WriteLine(val);
                string responseString = HandleRequest(val);

                var responseData = Encoding.ASCII.GetBytes(responseString);


                response.ContentLength64 = responseData.Length;
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");
                response.ContentEncoding = Encoding.UTF8;


                Stream outputStream = response.OutputStream;
                outputStream.Write(responseData, 0, responseData.Length);
                outputStream.Close();
                ServerManager.SaveServerList();
            }
        }

        private string HandleRequest(string req)
        {
            bool passwordOK = false;
            List<string[]> requests = new List<string[]>();
            foreach (var item in req.Split('&'))
            {
                string[] parts = item.Split('=');

                if (parts == null)
                    continue;

                if (parts.Length < 2)
                    continue;

                requests.Add(new string[]{HttpUtility.UrlDecode(parts[0]), HttpUtility.UrlDecode(parts[1])});

                if (parts[0] == "password" && parts[1] == settings.Password)
                    passwordOK = true;

            }

            if (!passwordOK)
                return "ACCESS_DENIED";

            return ServerManager.HandleRequest(requests);
        }
    }
}
