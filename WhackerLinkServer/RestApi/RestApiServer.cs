using Nancy;
using Nancy.Hosting.Self;
using WhackerLinkCommonLib.Interfaces;

namespace WhackerLinkServer
{
    public class RestApiServer
    {
        private readonly NancyHost _nancyHost;
        private string url;

        public RestApiServer(IMasterService masterService, string address, int port)
        {
            url = $"http://{address}:{port}";

            var config = new HostConfiguration { UrlReservations = new UrlReservations { CreateAutomatically = true } };
            _nancyHost = new NancyHost(new Uri(url), new DefaultNancyBootstrapper(), config);
        }

        public void Start()
        {
            _nancyHost.Start();
            Console.WriteLine($"REST server started at {url}");
        }

        public void Stop()
        {
            _nancyHost?.Stop();
            Console.WriteLine($"REST server ${url} stopped.");
        }
    }
}