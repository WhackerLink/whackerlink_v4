using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WhackerLinkBridge.Models;
using WhackerLinkLib.Network;
using WhackerLinkLib.Models.IOSP;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;
using static WhackerLinkLib.Models.Radio.Codeplug;
using System.Threading.Channels;

#nullable disable

namespace WhackerLinkBridge
{
    public class WhackerLinkBridgeApp
    {
        public static string Talkgroup = "1"; 

        internal readonly Config _config;
        internal readonly IPeer _webSocketHandler;
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
            _webSocketHandler = new Peer();
            _udpAudioHandler = new UdpAudioHandler(
                _config.dvmBridge.Address,
                _config.dvmBridge.TxPort,
                "0.0.0.0",
                _config.dvmBridge.RxPort);

            Talkgroup = _config.system.dstId;

            _webSocketHandler.OnOpen += () =>
            {
                Console.WriteLine("Connection to whackerlink master established");


                // fake aff to receive aff restricted traffic
                GRP_AFF_REQ aff = new GRP_AFF_REQ
                {
                    SrcId = "1",
                    DstId = _config.system.dstId,
                };

                _webSocketHandler.SendMessage(aff.GetData());
            };

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
            Task.Run(() => {
                try
                {
                    ListenToUdpAudio(_config);
                } catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
             });
        }

        internal void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (response.Status == (int)ResponseType.GRANT)
            {
                currentVoiceChannel = new VoiceChannel
                {
                    Frequency = response.Channel,
                    SrcId = response.SrcId,
                    DstId = response.DstId,
                    Site = _config.system.Site
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
            // fake aff to send aff restricted traffic
            GRP_AFF_REQ aff = new GRP_AFF_REQ
            {
                SrcId = srcId,
                DstId = dstId,
            };

            _webSocketHandler.SendMessage(aff.GetData());

            GRP_VCH_REQ request = new GRP_VCH_REQ
            {
                SrcId = srcId,
                DstId = dstId,
                Site = _config.system.Site
            };

            _webSocketHandler.SendMessage(request.GetData());
        }

        internal void SendVoiceChannelRelease(VoiceChannel voiceChannel)
        {
            GRP_VCH_RLS release = new GRP_VCH_RLS
            {
                SrcId = voiceChannel.SrcId,
                DstId = voiceChannel.DstId,
                Channel = voiceChannel.Frequency,
                Site = _config.system.Site
            };

            _webSocketHandler.SendMessage(release.GetData());
        }

        internal async Task ListenToUdpAudio(Config config)
        {
            switch (_config.system.Mode)
            {
                case BridgeModes.DVM:
                    await DvmUtils.HandleInboundDvm(this, config);
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