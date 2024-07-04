// SPDX-License-Identifier: AGPL-3.0-only
/**
* Digital Voice Modem - Fixed Network Equipment Core Library
* AGPLv3 Open Source. Use is subject to license terms.
* DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
*
* @package DVM / Fixed Network Equipment Core Library
* @license AGPLv3 License (https://opensource.org/licenses/AGPL-3.0)
*
*   Copyright (C) 2022-2024 Bryan Biedenkapp, N2PLL
*
*/

using System;

namespace fnecore.P25
{
    /// <summary>
    /// P25 DFSI Frame Types
    /// </summary>
    public class P25DFSI
    {
        public const byte P25_RTP_PAYLOAD_TYPE = 100;

        public const uint P25_DFSI_LDU1_VOICE1_FRAME_LENGTH_BYTES = 22;
        public const uint P25_DFSI_LDU1_VOICE2_FRAME_LENGTH_BYTES = 14;
        public const uint P25_DFSI_LDU1_VOICE3_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE4_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE5_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE6_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE7_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE8_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU1_VOICE9_FRAME_LENGTH_BYTES = 16;

        public const uint P25_DFSI_LDU2_VOICE10_FRAME_LENGTH_BYTES = 22;
        public const uint P25_DFSI_LDU2_VOICE11_FRAME_LENGTH_BYTES = 14;
        public const uint P25_DFSI_LDU2_VOICE12_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE13_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE14_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE15_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE16_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE17_FRAME_LENGTH_BYTES = 17;
        public const uint P25_DFSI_LDU2_VOICE18_FRAME_LENGTH_BYTES = 16;

        public const uint P25_DFSI_VHDR_RAW_LEN = 36;
        public const uint P25_DFSI_VHDR_LEN = 27;

        public const byte P25_DFSI_STATUS_NO_ERROR = 0x00;   //
        public const byte P25_DFSI_STATUS_ERASE = 0x02;      //

        public const byte P25_DFSI_RT_ENABLED = 0x02;        //
        public const byte P25_DFSI_RT_DISABLED = 0x04;       //

        public const byte P25_DFSI_START_FLAG = 0x0C;        //
        public const byte P25_DFSI_STOP_FLAG = 0x25;         //

        public const byte P25_DFSI_TYPE_DATA_PAYLOAD = 0x06; //
        public const byte P25_DFSI_TYPE_VOICE = 0x0B;        //

        public const byte P25_DFSI_DEF_ICW_SOURCE = 0x00;    // Infrastructure Source - Default Source
        public const byte P25_DFSI_DEF_SOURCE = 0x00;        //

        public const byte P25_DFSI_MOT_START_STOP = 0x00;    // Motorola Start/Stop Frame
        public const byte P25_DFSI_MOT_VHDR_1 = 0x60;        // Motorola Voice Header 1
        public const byte P25_DFSI_MOT_VHDR_2 = 0x61;        // Motorola Voice Header 2

        public const byte P25_DFSI_LDU1_VOICE1 = 0x62;       // IMBE LDU1 - Voice 1
        public const byte P25_DFSI_LDU1_VOICE2 = 0x63;       // IMBE LDU1 - Voice 2
        public const byte P25_DFSI_LDU1_VOICE3 = 0x64;       // IMBE LDU1 - Voice 3 + Link Control
        public const byte P25_DFSI_LDU1_VOICE4 = 0x65;       // IMBE LDU1 - Voice 4 + Link Control
        public const byte P25_DFSI_LDU1_VOICE5 = 0x66;       // IMBE LDU1 - Voice 5 + Link Control
        public const byte P25_DFSI_LDU1_VOICE6 = 0x67;       // IMBE LDU1 - Voice 6 + Link Control
        public const byte P25_DFSI_LDU1_VOICE7 = 0x68;       // IMBE LDU1 - Voice 7 + Link Control
        public const byte P25_DFSI_LDU1_VOICE8 = 0x69;       // IMBE LDU1 - Voice 8 + Link Control
        public const byte P25_DFSI_LDU1_VOICE9 = 0x6A;       // IMBE LDU1 - Voice 9 + Low Speed Data

        public const byte P25_DFSI_LDU2_VOICE10 = 0x6B;      // IMBE LDU2 - Voice 10
        public const byte P25_DFSI_LDU2_VOICE11 = 0x6C;      // IMBE LDU2 - Voice 11
        public const byte P25_DFSI_LDU2_VOICE12 = 0x6D;      // IMBE LDU2 - Voice 12 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE13 = 0x6E;      // IMBE LDU2 - Voice 13 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE14 = 0x6F;      // IMBE LDU2 - Voice 14 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE15 = 0x70;      // IMBE LDU2 - Voice 15 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE16 = 0x71;      // IMBE LDU2 - Voice 16 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE17 = 0x72;      // IMBE LDU2 - Voice 17 + Encryption Sync
        public const byte P25_DFSI_LDU2_VOICE18 = 0x73;      // IMBE LDU2 - Voice 18 + Low Speed Data
    } // public class P25DSFI_FT

    /// <summary>
    /// P25 Data Unit ID
    /// </summary>
    public enum P25DUID : byte
    {
        /// <summary>
        /// Header Data Unit
        /// </summary>
        HDU = 0x00,
        /// <summary>
        /// Terminator Data Unit
        /// </summary>
        TDU = 0x03,
        /// <summary>
        /// Logical Data Unit 1
        /// </summary>
        LDU1 = 0x05,
        /// <summary>
        /// Trunking Signalling Data Unit
        /// </summary>
        TSDU = 0x07,
        /// <summary>
        /// Logical Data Unit 2
        /// </summary>
        LDU2 = 0x0A,
        /// <summary>
        /// Packet Data Unit
        /// </summary>
        PDU = 0x0C,
        /// <summary>
        /// Terminator Data Unit with Link Control
        /// </summary>
        TDULC = 0x0F
    } // public enum P25DUID : byte

    /// <summary>
    /// P25 Constants
    /// </summary>
    public class P25Defines
    {
        public const byte P25_FT_HDU_VALID = 0x01;
        public const byte P25_FT_HDU_LATE_ENTRY = 0x02;
        public const byte P25_FT_TERMINATOR = 0x03;
        public const byte P25_FT_DATA_UNIT = 0x00;

        public const byte P25_MFG_STANDARD = 0x00;

        public const byte P25_ALGO_UNENCRYPT = 0x80;

        public const byte P25_MI_LENGTH = 9;

        public const byte P25_TSDU_FRAME_LENGTH_BYTES = 45;

        public const byte P25_LDU_FRAME_LENGTH_BYTES = 216;

        public const byte P25_TSBK_FEC_LENGTH_BYTES = 25;
        public const byte P25_TSBK_FEC_LENGTH_BITS = P25_TSBK_FEC_LENGTH_BYTES * 8 - 4; // Trellis is actually 196 bits
        public const byte P25_TSBK_LENGTH_BYTES = 12;

        public const byte P25_MAX_PDU_COUNT = 32;
        public const uint P25_MAX_PDU_LENGTH = 512;

        public const byte P25_PDU_HEADER_LENGTH_BYTES = 12;
        public const byte P25_PDU_CONFIRMED_LENGTH_BYTES = 18;
        public const byte P25_PDU_CONFIRMED_DATA_LENGTH_BYTES = 16;
        public const byte P25_PDU_UNCONFIRMED_LENGTH_BYTES = 12;

        public const byte P25_PDU_FEC_LENGTH_BYTES = 25;
        public const byte P25_PDU_FEC_LENGTH_BITS = (byte)(P25_PDU_FEC_LENGTH_BYTES * 8U - 4U); // Trellis is actually 196 bits

        // PDU Format Type(s)
        public const byte PDU_FMT_RSP = 0x03;
        public const byte PDU_FMT_UNCONFIRMED = 0x15;
        public const byte PDU_FMT_CONFIRMED = 0x16;
        public const byte PDU_FMT_AMBT = 0x17;

        // PDU SAP
        public const byte PDU_SAP_USER_DATA = 0x00;
        public const byte PDU_SAP_ENC_USER_DATA = 0x01;

        public const byte PDU_SAP_PACKET_DATA = 0x04;

        public const byte PDU_SAP_ARP = 0x05;

        public const byte PDU_SAP_SNDCP_CTRL_DATA = 0x06;

        public const byte PDU_SAP_EXT_ADDR = 0x1F;

        public const byte PDU_SAP_REG = 0x20;

        public const byte PDU_SAP_UNENC_KMM = 0x28;
        public const byte PDU_SAP_ENC_KMM = 0x29;

        public const byte PDU_SAP_TRUNK_CTRL = 0x3D;

        // PDU ACK Class
        public const byte PDU_ACK_CLASS_ACK = 0x00;
        public const byte PDU_ACK_CLASS_NACK = 0x01;
        public const byte PDU_ACK_CLASS_ACK_RETRY = 0x02;

        // PDU ACK Type(s)
        public const byte PDU_ACK_TYPE_RETRY = 0x00;

        public const byte PDU_ACK_TYPE_ACK = 0x01;

        public const byte PDU_ACK_TYPE_NACK_ILLEGAL = 0x00;      // Illegal Format
        public const byte PDU_ACK_TYPE_NACK_PACKET_CRC = 0x01;   // Packet CRC
        public const byte PDU_ACK_TYPE_NACK_MEMORY_FULL = 0x02;  // Memory Full
        public const byte PDU_ACK_TYPE_NACK_SEQ = 0x03;          // Out of logical sequence FSN
        public const byte PDU_ACK_TYPE_NACK_UNDELIVERABLE = 0x04;// Undeliverable
        public const byte PDU_ACK_TYPE_NACK_OUT_OF_SEQ = 0x05;   // Out of sequence, N(S) != V(R) or V(R) + 1
        public const byte PDU_ACK_TYPE_NACK_INVL_USER = 0x06;    // Invalid User disallowed by the system

        // LDUx/TDULC Link Control Opcode(s)
        public const byte LC_GROUP = 0x00;                   // GRP VCH USER - Group Voice Channel User
        public const byte LC_GROUP_UPDT = 0x02;              // GRP VCH UPDT - Group Voice Channel Update
        public const byte LC_PRIVATE = 0x03;                 // UU VCH USER - Unit-to-Unit Voice Channel User
        public const byte LC_UU_ANS_REQ = 0x05;              // UU ANS REQ - Unit to Unit Answer Request 
        public const byte LC_TEL_INT_VCH_USER = 0x06;        // TEL INT VCH USER - Telephone Interconnect Voice Channel User 
        public const byte LC_TEL_INT_ANS_RQST = 0x07;        // TEL INT ANS RQST - Telephone Interconnect Answer Request 
        public const byte LC_CALL_TERM = 0x0F;               // CALL TERM - Call Termination or Cancellation
        public const byte LC_IDEN_UP = 0x18;                 // IDEN UP - Channel Identifier Update
        public const byte LC_SYS_SRV_BCAST = 0x20;           // SYS SRV BCAST - System Service Broadcast
        public const byte LC_ADJ_STS_BCAST = 0x22;           // ADJ STS BCAST - Adjacent Site Status Broadcast
        public const byte LC_RFSS_STS_BCAST = 0x23;          // RFSS STS BCAST - RFSS Status Broadcast
        public const byte LC_NET_STS_BCAST = 0x24;           // NET STS BCAST - Network Status Broadcast
        public const byte LC_CONV_FALLBACK = 0x2A;           // CONV FALLBACK - Conventional Fallback

        // TSBK ISP/OSP Shared Opcode(s)
        public const byte TSBK_IOSP_GRP_VCH = 0x00;          // GRP VCH REQ - Group Voice Channel Request (ISP), GRP VCH GRANT - Group Voice Channel Grant (OSP)
        public const byte TSBK_IOSP_UU_VCH = 0x04;           // UU VCH REQ - Unit-to-Unit Voice Channel Request (ISP), UU VCH GRANT - Unit-to-Unit Voice Channel Grant (OSP)
        public const byte TSBK_IOSP_UU_ANS = 0x05;           // UU ANS RSP - Unit-to-Unit Answer Response (ISP), UU ANS REQ - Unit-to-Unit Answer Request (OSP)
        public const byte TSBK_IOSP_TELE_INT_DIAL = 0x08;    // TELE INT DIAL REQ - Telephone Interconnect Request - Explicit (ISP), TELE INT DIAL GRANT - Telephone Interconnect Grant (OSP)
        public const byte TSBK_IOSP_TELE_INT_ANS = 0x0A;     // TELE INT ANS RSP - Telephone Interconnect Answer Response (ISP), TELE INT ANS REQ - Telephone Interconnect Answer Request (OSP)
        public const byte TSBK_IOSP_STS_UPDT = 0x18;         // STS UPDT REQ - Status Update Request (ISP), STS UPDT - Status Update (OSP)
        public const byte TSBK_IOSP_STS_Q = 0x1A;            // STS Q REQ - Status Query Request (ISP), STS Q - Status Query (OSP)
        public const byte TSBK_IOSP_MSG_UPDT = 0x1C;         // MSG UPDT REQ - Message Update Request (ISP), MSG UPDT - Message Update (OSP)
        public const byte TSBK_IOSP_CALL_ALRT = 0x1F;        // CALL ALRT REQ - Call Alert Request (ISP), CALL ALRT - Call Alert (OSP)
        public const byte TSBK_IOSP_ACK_RSP = 0x20;          // ACK RSP U - Acknowledge Response - Unit (ISP), ACK RSP FNE - Acknowledge Response - FNE (OSP)
        public const byte TSBK_IOSP_EXT_FNCT = 0x24;         // EXT FNCT RSP - Extended Function Response (ISP), EXT FNCT CMD - Extended Function Command (OSP)
        public const byte TSBK_IOSP_GRP_AFF = 0x28;          // GRP AFF REQ - Group Affiliation Request (ISP), GRP AFF RSP - Group Affiliation Response (OSP)
        public const byte TSBK_IOSP_U_REG = 0x2C;            // U REG REQ - Unit Registration Request (ISP), U REG RSP - Unit Registration Response (OSP)

        // TSBK Inbound Signalling Packet (ISP) Opcode(s)
        public const byte TSBK_ISP_TELE_INT_PSTN_REQ = 0x09; // TELE INT PSTN REQ - Telephone Interconnect Request - Implicit
        public const byte TSBK_ISP_SNDCP_CH_REQ = 0x12;      // SNDCP CH REQ - SNDCP Data Channel Request
        public const byte TSBK_ISP_STS_Q_RSP = 0x19;         // STS Q RSP - Status Query Response
        public const byte TSBK_ISP_CAN_SRV_REQ = 0x23;       // CAN SRV REQ - Cancel Service Request
        public const byte TSBK_ISP_EMERG_ALRM_REQ = 0x27;    // EMERG ALRM REQ - Emergency Alarm Request
        public const byte TSBK_ISP_GRP_AFF_Q_RSP = 0x29;     // GRP AFF Q RSP - Group Affiliation Query Response
        public const byte TSBK_ISP_U_DEREG_REQ = 0x2B;       // U DE REG REQ - Unit De-Registration Request
        public const byte TSBK_ISP_LOC_REG_REQ = 0x2D;       // LOC REG REQ - Location Registration Request

        // TSBK Outbound Signalling Packet (OSP) Opcode(s)
        public const byte TSBK_OSP_GRP_VCH_GRANT_UPD = 0x02; // GRP VCH GRANT UPD - Group Voice Channel Grant Update
        public const byte TSBK_OSP_UU_VCH_GRANT_UPD = 0x06;  // UU VCH GRANT UPD - Unit-to-Unit Voice Channel Grant Update
        public const byte TSBK_OSP_SNDCP_CH_GNT = 0x14;      // SNDCP CH GNT - SNDCP Data Channel Grant
        public const byte TSBK_OSP_SNDCP_CH_ANN = 0x16;      // SNDCP CH ANN - SNDCP Data Channel Announcement
        public const byte TSBK_OSP_DENY_RSP = 0x27;          // DENY RSP - Deny Response
        public const byte TSBK_OSP_SCCB_EXP = 0x29;          // SCCB - Secondary Control Channel Broadcast - Explicit 
        public const byte TSBK_OSP_GRP_AFF_Q = 0x2A;         // GRP AFF Q - Group Affiliation Query
        public const byte TSBK_OSP_LOC_REG_RSP = 0x2B;       // LOC REG RSP - Location Registration Response
        public const byte TSBK_OSP_U_REG_CMD = 0x2D;         // U REG CMD - Unit Registration Command
        public const byte TSBK_OSP_U_DEREG_ACK = 0x2F;       // U DE REG ACK - Unit De-Registration Acknowledge
        public const byte TSBK_OSP_QUE_RSP = 0x33;           // QUE RSP - Queued Response
        public const byte TSBK_OSP_IDEN_UP_VU = 0x34;        // IDEN UP VU - Channel Identifier Update for VHF/UHF Bands
        public const byte TSBK_OSP_SYS_SRV_BCAST = 0x38;     // SYS SRV BCAST - System Service Broadcast
        public const byte TSBK_OSP_SCCB = 0x39;              // SCCB - Secondary Control Channel Broadcast
        public const byte TSBK_OSP_RFSS_STS_BCAST = 0x3A;    // RFSS STS BCAST - RFSS Status Broadcast
        public const byte TSBK_OSP_NET_STS_BCAST = 0x3B;     // NET STS BCAST - Network Status Broadcast
        public const byte TSBK_OSP_ADJ_STS_BCAST = 0x3C;     // ADJ STS BCAST - Adjacent Site Status Broadcast
        public const byte TSBK_OSP_IDEN_UP = 0x3D;           // IDEN UP - Channel Identifier Update

        // TSBK Motorola Outbound Signalling Packet (OSP) Opcode(s)
        public const byte TSBK_OSP_MOT_GRG_ADD = 0x00;       // MOT GRG ADD - Motorola / Group Regroup Add (Patch Supergroup)
        public const byte TSBK_OSP_MOT_GRG_DEL = 0x01;       // MOT GRG DEL - Motorola / Group Regroup Delete (Unpatch Supergroup)
        public const byte TSBK_OSP_MOT_GRG_VCH_GRANT = 0x02; // MOT GRG GROUP VCH GRANT / Group Regroup Voice Channel Grant
        public const byte TSBK_OSP_MOT_GRG_VCH_UPD = 0x03;   // MOT GRG GROUP VCH GRANT UPD / Group Regroup Voice Channel Grant Update
        public const byte TSBK_OSP_MOT_CC_BSI = 0x0B;        // MOT CC BSI - Motorola / Control Channel Base Station Identifier
        public const byte TSBK_OSP_MOT_PSH_CCH = 0x0E;       // MOT PSH CCH - Motorola / Planned Control Channel Shutdown

        // TSBK Motorola Outbound Signalling Packet (OSP) Opcode(s)
        public const byte TSBK_OSP_DVM_GIT_HASH = 0xFB;      //
    } // public class P25Defines
} // namespace fnecore.P25
