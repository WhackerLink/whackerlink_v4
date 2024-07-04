// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore
{
    /// <summary>
    /// Used internally to identify the logging level.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// Informational
        /// </summary>
        INFO = 0x00,
        /// <summary>
        /// Warning
        /// </summary>
        WARNING = 0x01,
        /// <summary>
        /// Error
        /// </summary>
        ERROR = 0x02,
        /// <summary>
        /// Debug
        /// </summary>
        DEBUG = 0x04,
        /// <summary>
        /// Fatal
        /// </summary>
        FATAL = 0x08
    } // public enum LogLevel : byte

    /// <summary>
    /// Peer Connection State
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>
        /// Waiting on Login - Received the repeater login request
        /// </summary>
        WAITING_LOGIN,
        /// <summary>
        /// Waiting on Authorization - Sent the connection challenge to peer
        /// </summary>
        WAITING_AUTHORISATION,
        /// <summary>
        /// Waiting on Configuration
        /// </summary>
        WAITING_CONFIG,
        /// <summary>
        /// Running
        /// </summary>
        RUNNING,
    } // public enum ConnectionState

    /// <summary>
    /// 
    /// </summary>
    public enum CallType : byte
    {
        /// <summary>
        /// Group Call
        /// </summary>
        GROUP = 0x00,
        /// <summary>
        /// Private Call
        /// </summary>
        PRIVATE = 0x01,
    } // public enum CallType : byte

    /// <summary>
    /// 
    /// </summary>
    public enum FrameType : byte
    {
        /// <summary>
        /// 
        /// </summary>
        VOICE = 0x00,
        /// <summary>
        /// 
        /// </summary>
        VOICE_SYNC = 0x01,
        /// <summary>
        /// 
        /// </summary>
        DATA_SYNC = 0x02,

        /// <summary>
        /// 
        /// </summary>
        TERMINATOR = 0xFF
    } // public enum FrameType : byte

    /// <summary>
    /// 
    /// </summary>
    public enum DVMState : byte
    {
        /// <summary>
        /// Idle
        /// </summary>
        IDLE = 0,
        /// <summary>
        /// DMR
        /// </summary>
        DMR = 1,
        /// <summary>
        /// P25
        /// </summary>
        P25 = 2,
        /// <summary>
        /// NXDN
        /// </summary>
        NXDN = 3
    } // public enum DVMState : byte

    /// <summary>
    /// This class defines commonly used protocol and internal constants.
    /// </summary>
    public sealed class Constants
    {
        public const uint InvalidTS = uint.MaxValue;

        public const uint RtpHeaderLengthBytes = 12;
        public const uint RtpExtensionHeaderLengthBytes = 4;
        public const uint RtpFNEHeaderLengthBytes = 16;
        public const ushort RtpFNEHeaderExtLength = 4; // length of FNE header in 32-bit units
        public const uint RtpGenericClockRate = 8000;

        public const ushort RtpCallEndSeq = 65535;

        public const byte DVMRtpPayloadType = 0x56;
        public const byte DVMFrameStart = 0xFE;

        /*
        ** Protocol Functions and Sub-Functions
        */
        public const byte NET_SUBFUNC_NOP = 0xFF;                               // No Operation Sub-Function

        public const byte NET_FUNC_PROTOCOL = 0x00;                             // Network Protocol Function
        public const byte NET_PROTOCOL_SUBFUNC_DMR = 0x00;                      // DMR
        public const byte NET_PROTOCOL_SUBFUNC_P25 = 0x01;                      // P25
        public const byte NET_PROTOCOL_SUBFUNC_NXDN = 0x02;                     // NXDN

        public const byte NET_FUNC_MASTER = 0x01;                               // Network Master Function
        public const byte NET_MASTER_SUBFUNC_WL_RID = 0x00;                     // Whitelist RIDs
        public const byte NET_MASTER_SUBFUNC_BL_RID = 0x01;                     // Blacklist RIDs
        public const byte NET_MASTER_SUBFUNC_ACTIVE_TGS = 0x02;                 // Active TGIDs
        public const byte NET_MASTER_SUBFUNC_DEACTIVE_TGS = 0x03;               // Deactive TGIDs

        public const byte NET_FUNC_RPTL = 0x60;                                 // Repeater Login
        public const byte NET_FUNC_RPTK = 0x61;                                 // Repeater Authorisation
        public const byte NET_FUNC_RPTC = 0x62;                                 // Repeater Configuration

        public const byte NET_FUNC_RPT_CLOSING = 0x70;                          // Repeater Closing
        public const byte NET_FUNC_MST_CLOSING = 0x71;                          // Master Closing

        public const byte NET_FUNC_PING = 0x74;                                 // Ping
        public const byte NET_FUNC_PONG = 0x75;                                 // Pong

        public const byte NET_FUNC_GRANT = 0x7A;                                // Grant Request

        public const byte NET_FUNC_ACK = 0x7E;                                  // Packet Acknowledge
        public const byte NET_FUNC_NAK = 0x7F;                                  // Packet Negative Acknowledge

        public const byte NET_FUNC_TRANSFER = 0x90;                             // Network Transfer Function
        public const byte NET_TRANSFER_SUBFUNC_ACTIVITY = 0x01;                 // Activity Log Transfer
        public const byte NET_TRANSFER_SUBFUNC_DIAG = 0x02;                     // Diagnostic Log Transfer

        public const byte NET_FUNC_ANNOUNCE = 0x91;                             // Network Announce Function
        public const byte NET_ANNC_SUBFUNC_GRP_AFFIL = 0x00;                    // Announce Group Affiliation
        public const byte NET_ANNC_SUBFUNC_UNIT_REG = 0x01;                     // Announce Unit Registration
        public const byte NET_ANNC_SUBFUNC_UNIT_DEREG = 0x02;                   // Announce Unit Deregistration
        public const byte NET_ANNC_SUBFUNC_AFFILS = 0x90;                       // Update All Affiliations

        /*
        ** Protocol Tags (as strings)
        */
        public const string TAG_DMR_DATA = "DMRD";
        public const string TAG_P25_DATA = "P25D";
        public const string TAG_NXDN_DATA = "NXDD";

        public const string TAG_REPEATER_LOGIN = "RPTL";
        public const string TAG_REPEATER_AUTH = "RPTK";
        public const string TAG_REPEATER_CONFIG = "RPTC";

        public const string TAG_REPEATER_PING = "RPTP";
        public const string TAG_REPEATER_GRANT = "RPTG";

        /*
        ** Timers
        */
        public const double STREAM_TO = 0.360d;
    } // public sealed class Constants
} // namespace fnecore
