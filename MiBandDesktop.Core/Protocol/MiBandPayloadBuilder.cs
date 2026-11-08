using System;
using System.Collections.Generic;
using System.Text.Json;
using MiBandDesktop.Core.Entities;

namespace MiBandDesktop.Core.Protocol;

public class MiBandPayloadBuilder
{
    private static readonly byte[] CompatPrefix = [0x00, 0x00, 0xC1, 0x00];

    public static byte[] BuildTimePayload(DateTime dt)
    {
        // 11 bytes: Year(uint16LE), Month, Day, Hour, Min, Sec, IsoWeekday, 0, 0, TzQuarters(sbyte)
        byte[] payload = new byte[11];
        BitConverter.TryWriteBytes(payload.AsSpan(0, 2), (ushort)dt.Year);
        payload[2] = (byte)dt.Month;
        payload[3] = (byte)dt.Day;
        payload[4] = (byte)dt.Hour;
        payload[5] = (byte)dt.Minute;
        payload[6] = (byte)dt.Second;
        payload[7] = (byte)(dt.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)dt.DayOfWeek);
        payload[8] = 0;
        payload[9] = 0;
        int tzOffsetQuarters = (int)(TimeZoneInfo.Local.GetUtcOffset(dt).TotalSeconds / 900.0);
        payload[10] = unchecked((byte)(sbyte)tzOffsetQuarters);
        return payload;
    }

    public static byte[] BuildFitnessGoals(ushort stepGoal, ushort calorieGoal)
    {
        byte[] payload = new byte[7];
        payload[0] = 0x06;
        payload[1] = 0x01;
        payload[2] = 0x00;
        BitConverter.TryWriteBytes(payload.AsSpan(3, 2), stepGoal);
        BitConverter.TryWriteBytes(payload.AsSpan(5, 2), calorieGoal);
        return payload;
    }

    public static byte[] BuildPaiScore(ushort totalPai, ushort todayPai)
    {
        byte[] payload = new byte[6];
        payload[0] = 0x01;
        payload[1] = 0x18;
        BitConverter.TryWriteBytes(payload.AsSpan(2, 2), totalPai);
        BitConverter.TryWriteBytes(payload.AsSpan(4, 2), todayPai);
        return payload;
    }

    public static byte[] BuildStressMonitoring(bool enabled, byte intervalMinutes)
    {
        return [0x06, 0x12, (byte)(enabled ? 1 : 0), intervalMinutes];
    }

    public static byte[] BuildWearLocation(bool isRightWrist)
    {
        return [0x06, 0x02, 0x00, (byte)(isRightWrist ? 1 : 0)];
    }

    public static byte[] BuildLiftWrist(string mode, string startTime, string endTime, string speed)
    {
        byte modeVal = mode switch
        {
            "all_day" => 0x01,
            "scheduled" => 0x02,
            _ => 0x00
        };

        byte sHr = 7, sMi = 0, eHr = 22, eMi = 0;
        var sParts = startTime.Split(':');
        if (sParts.Length >= 2 && byte.TryParse(sParts[0], out var sh) && byte.TryParse(sParts[1], out var sm))
        {
            sHr = sh; sMi = sm;
        }
        var eParts = endTime.Split(':');
        if (eParts.Length >= 2 && byte.TryParse(eParts[0], out var eh) && byte.TryParse(eParts[1], out var em))
        {
            eHr = eh; eMi = em;
        }

        byte speedVal = (byte)(speed == "fast" ? 0x01 : 0x00);
        return [0x06, 0x05, modeVal, sHr, sMi, eHr, eMi, speedVal];
    }

    public static (byte[] nightPacket, byte[] dndPacket) BuildNightModeAndDnd(
        string nightMode, string nightStart, string nightEnd, string dndMode)
    {
        byte nightVal = nightMode switch
        {
            "sunset" => 0x01,
            "scheduled" => 0x02,
            _ => 0x00
        };

        byte sHr = 22, sMi = 0, eHr = 7, eMi = 0;
        var sParts = nightStart.Split(':');
        if (sParts.Length >= 2 && byte.TryParse(sParts[0], out var sh) && byte.TryParse(sParts[1], out var sm))
        {
            sHr = sh; sMi = sm;
        }
        var eParts = nightEnd.Split(':');
        if (eParts.Length >= 2 && byte.TryParse(eParts[0], out var eh) && byte.TryParse(eParts[1], out var em))
        {
            eHr = eh; eMi = em;
        }

        byte[] night = [0x06, 0x0a, nightVal, sHr, sMi, eHr, eMi];

        byte dndVal = dndMode switch
        {
            "always" or "all_day" => 0x01,
            "smart" => 0x02,
            "scheduled" => 0x03,
            _ => 0x00
        };
        byte[] dnd = [0x06, 0x0b, dndVal, sHr, sMi, eHr, eMi];

        return (night, dnd);
    }

    public static byte[] BuildLanguage(string langCode)
    {
        byte[] langBytes = System.Text.Encoding.UTF8.GetBytes(langCode);
        if (langBytes.Length > 16)
        {
            Array.Resize(ref langBytes, 16);
        }
        byte[] payload = new byte[2 + langBytes.Length];
        payload[0] = 0x06;
        payload[1] = 0x08;
        Array.Copy(langBytes, 0, payload, 2, langBytes.Length);
        return payload;
    }

    public static byte[] BuildAlarmPacket(int index, bool enabled, bool snooze, byte hour, byte minute, byte repetitionMask)
    {
        byte state = (byte)(index & 0x0F);
        if (enabled) state |= 0x80;
        if (enabled && !snooze) state |= 0x40;

        return [0x01, 0x02, state, hour, minute, repetitionMask];
    }

    public static byte[] BuildGhostAlarmPacket(int index)
    {
        return [0x01, 0x02, (byte)(index & 0x0F), 0x00, 0x00, 0x00];
    }
}

