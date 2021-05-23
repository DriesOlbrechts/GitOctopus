using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Threading;
using System.IO;


namespace Octo.Domain
{
    public class WebHandler
    {
        static HttpListener _httpListener = new HttpListener();
        static bool Running = true;
        static string token;

        public static void Run()
        {
            Running = true;
            _httpListener.Prefixes.Add("http://localhost:8080/OAuth/"); // add prefix "http://localhost:8080/"    
            _httpListener.Start(); 
            Thread _responseThread = new Thread(ResponseThread);
            _responseThread.Start(); // start the response thread       
        }

        static void ResponseThread()
        {
            while (Running)
            {
                HttpListenerContext context = _httpListener.GetContext(); // get a context
                byte[] _responseArray;           
                _responseArray = Encoding.UTF8.GetBytes($"<html><head><title>Localhost server -- port 8080</title>" +
                "" +
                "</head>" +
                "<body>" +
                "You can safely close this page" +
                "</body></html>"); 

                context.Response.OutputStream.Write(_responseArray, 0, _responseArray.Length); // write bytes to the output stream
                context.Response.KeepAlive = false; // set the KeepAlive bool to false
                context.Response.Close(); // close the connection
                string hey = context.Request.Url.Query;
                token = hey.Replace("?code=", "");
                Running = !context.Request.Url.Query.Contains("code");
                Console.WriteLine("Respone given to a request.");
                Console.WriteLine(hey);
                Console.WriteLine(token);
            }
           
            string path = Path.Combine(Directory.GetCurrentDirectory(),  "ClientSecret.json");
            string clientSecrets = System.IO.File.ReadAllText(path);
            ClientSecret clientsecret = JsonSerializer.Deserialize<ClientSecret>(clientSecrets);

            string sURL = String.Format(@"https://github.com/login/oauth/access_token?client_id={0}&client_secret={1}&code={2}", clientsecret.client_id ,  clientsecret.client_secret, token);
            HttpWebRequest wrGETURL;

            // create a get request to github to get the token
            wrGETURL = (HttpWebRequest) WebRequest.Create(sURL);
            wrGETURL.Headers.Add("20", "application/json");

            //get the response
            Stream response = wrGETURL.GetResponse().GetResponseStream();
            StreamReader readStream = new StreamReader(response, Encoding.UTF8);

            // parse the response to only get the token
            string access_token = readStream.ReadToEnd().Split('&')[0].Replace("access_token=", "");
            Controller.Instance._TokenHandler.SaveEncryptedToken(access_token);
            Controller.Instance._TokenHandler.SaveTokenToDisc();

            Controller.Instance.OnLoggedIn(true);
            _httpListener.Stop();
        }
    }
}   
