using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class CollisionHelper
{
    public static Color GetColor(int colId)
    {
        return new Color(
            ((colId & 0x03) << 6) / 255f,
            ((colId & 0x0C) << 4) / 255f,
            ((colId & 0xF0) << 0) / 255f,
            1
            );
    }

    public static int ParseId(string colStr, int? defaultColId = null)
    {
        if (int.TryParse(colStr, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id))
            return id;

        return defaultColId ?? 0x2f; // default
    }
}
