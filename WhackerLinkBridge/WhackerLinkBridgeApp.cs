using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WhackerLinkBridge.Models;
using WhackerLinkLib.Handlers;
using WhackerLinkLib.Models.IOSP;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;

#nullable disable

namespace WhackerLinkBridge
{
    public class WhackerLinkBridgeApp
    {
        internal readonly Config _config;
        internal readonly IWebSocketHandler _webSocketHandler;
        internal readonly UdpAudioHandler _udpAudioHandler;

        internal VoiceChannel currentVoiceChannel;
        internal bool isGranted;
        internal bool _callInProgress;
        internal bool _txInProgress;
        internal System.Timers.Timer _rxHangTimer;
        internal System.Timers.Timer _txHangTimer;

        public WhackerLinkBridgeApp(string configPath)
        {
            _config = ConfigLoader.LoadConfig(configPath);
            _webSocketHandler = new WebSocketHandler();
            _udpAudioHandler = new UdpAudioHandler(
                _config.dvmBridge.Address,
                _config.dvmBridge.TxPort,
                "0.0.0.0",
                _config.dvmBridge.RxPort);

            _webSocketHandler.OnAudioData += HandleWebSocketAudioData;
            _webSocketHandler.OnVoiceChannelResponse += HandleVoiceChannelResponse;

            _rxHangTimer = new System.Timers.Timer(500);
            _rxHangTimer.Elapsed += OnRxHangTimeElapsed;
            _rxHangTimer.AutoReset = false;

            _txHangTimer = new System.Timers.Timer(500);
            _txHangTimer.Elapsed += OnTxHangTimeElapsed;
            _txHangTimer.AutoReset = false;
        }

        public void Start()
        {
            _webSocketHandler.Connect(_config.master.Address, _config.master.Port);
            Task.Run(() => ListenToUdpAudio());
        }

        internal void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (response.Status == (int)ResponseType.GRANT)
            {
                currentVoiceChannel = new VoiceChannel
                {
                    Frequency = response.Channel,
                    SrcId = response.SrcId,
                    DstId = response.DstId
                };

                isGranted = true;
            }
            else
            {
                isGranted = false;
            }
        }

        internal void SendVoiceChannelRequest(string srcId, string dstId)
        {
            var request = new
            {
                type = (int)PacketType.GRP_VCH_REQ,
                data = new GRP_VCH_REQ
                {
                    SrcId = srcId,
                    DstId = dstId,
                }
            };

            Console.WriteLine(request.ToString());
            _webSocketHandler.SendMessage(request);
        }

        internal void SendVoiceChannelRelease(VoiceChannel voiceChannel)
        {
            var request = new
            {
                type = (int)PacketType.GRP_VCH_RLS,
                data = new GRP_VCH_RLS
                {
                    SrcId = voiceChannel.SrcId,
                    DstId = voiceChannel.DstId,
                    Channel = voiceChannel.Frequency
                }
            };

            _webSocketHandler.SendMessage(request);
        }

        internal async Task ListenToUdpAudio()
        {
            switch (_config.system.Mode)
            {
                case BridgeModes.DVM:
                    await DvmUtils.HandleInboundDvm(this);
                    break;
                case BridgeModes.ALLSTARLINK:
                case BridgeModes.NONE:
                default:
                    Console.WriteLine("Current set mode is not support and will not be handled");
                    break;
            }
        }

        internal void OnRxHangTimeElapsed(object sender, ElapsedEventArgs e)
        {
            if (_callInProgress)
            {
                Console.WriteLine("Call End Detected");
                isGranted = false;
                SendVoiceChannelRelease(currentVoiceChannel);
                _callInProgress = false;
            }
        }

        internal async void HandleWebSocketAudioData(AudioPacket audioPacket)
        {
            switch (_config.system.Mode)
            {
                case BridgeModes.DVM:
                    await DvmUtils.SendToDvm(this, audioPacket.Data, audioPacket.VoiceChannel);
                    break;
                case BridgeModes.ALLSTARLINK:
                case BridgeModes.NONE:
                default:
                    Console.WriteLine("Current set mode is not support and will not be handled");
                    break;
            }
        }

        internal void OnTxHangTimeElapsed(object sender, ElapsedEventArgs e)
        {
            if (_txInProgress)
            {
                Console.WriteLine("TX Call End Detected");
                _txInProgress = false;
            }
        }
    }
}