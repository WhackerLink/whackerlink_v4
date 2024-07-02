using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WhackerLinkBridge
{
    public class UdpAudioHandler
    {
        private readonly UdpClient _txClient;
        private readonly UdpClient _rxClient;
        private readonly IPEndPoint _txEndPoint;
        private readonly IPEndPoint _rxEndPoint;

        public UdpAudioHandler(string txAddress, int txPort, string rxAddress, int rxPort)
        {
            _txClient = new UdpClient();
            _txEndPoint = new IPEndPoint(IPAddress.Parse(txAddress), txPort);

            _rxClient = new UdpClient(rxPort);
            _rxEndPoint = new IPEndPoint(IPAddress.Parse(rxAddress), rxPort);
        }

        public async Task SendAudio(byte[] audioData)
        {
            await _txClient.SendAsync(audioData, audioData.Length, _txEndPoint);
        }

        public async Task<byte[]> ReceiveAudio()
        {
            var result = await _rxClient.ReceiveAsync();
            return result.Buffer;
        }
    }
}
