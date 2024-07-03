using Nancy;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
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
            var bootstrapper = new CustomBootstrapper(masterService);
            _nancyHost = new NancyHost(new Uri(url), bootstrapper, config);
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
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IMasterService _masterService;

        public CustomBootstrapper(IMasterService masterService)
        {
            _masterService = masterService;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(_masterService);
        }
    }
}