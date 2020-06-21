using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading;

namespace FakeMW2SA
{
    class HttpClient
    {
        public static void Run()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            try
            {
                var localhostURI = "http://localhost:" + Program.port + "/";

                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(localhostURI);
                listener.Prefixes.Add("http://127.0.0.1:" + Program.port + "/");
                listener.Start();
                Console.WriteLine("Listening on http://localhost:" + Program.port + "/" + "and http://127.0.0.1:" + Program.port + "/");
                while (true)
                {
                    string responseString = String.Format(Utils.ReadEmbeddedResrourceAsString("ResponseTemplate.html"), Program.csrf);
                    responseString = responseString.Replace("#URL#", localhostURI);
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    string clientIP = context.Request.RemoteEndPoint.ToString();

                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    if (request.Url?.Segments.Length > 2 && request.Url?.Segments[1] == "assets/")
                    {
                        var assetName = request.Url?.Segments[1] + request.Url?.Segments[2];
                        responseString = Utils.ReadEmbeddedResrourceAsString(assetName.Replace("/", "."));
                    }
                    else if (request.QueryString.GetValues("action") != null && request.QueryString.GetValues("csrf") != null && request.QueryString.GetValues("csrf")[0] == Program.csrf.ToString())
                    {
                        var action = request.QueryString.GetValues("action")[0];
                        switch (action)
                        {
                            case "players":
                                response.ContentType = "application/json";
                                responseString = JsonConvert.SerializeObject(new JsonOutput());
                                break;
                            case "ban":
                                Utils.Ban(request.QueryString.GetValues("ip")[0]);
                                break;
                            case "unban":
                                Utils.Unban(request.QueryString.GetValues("ip")[0]);
                                break;
                            case "clearbans":
                                Utils.Clearfirewall();
                                break;
                            case "host":
                                responseString = JsonConvert.SerializeObject(new JsonOutput());
                                break;
                            default:
                                break;
                        }
                    }
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);

                }
            }
            catch (HttpListenerException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Unable to open the application on port " + Program.port + "/" + ". Is the application already running?");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public static void Start()
        {
            Thread a = new Thread(Run);
            a.Start();
        }
    }
}
