using System;
using System.Text.RegularExpressions;

namespace BLComponentTemplate.Others
{
    public static class ExcelCellHelper
    {
        public static (string Column, int Row) ParseCellAddress(string cellAddress)
        {
            if (string.IsNullOrWhiteSpace(cellAddress))
                throw new ArgumentException("La celda no es válida.", nameof(cellAddress));

            Match match = Regex.Match(cellAddress.ToUpperInvariant(), @"^([A-Z]+)(\d+)$");

            if (!match.Success)
                throw new ArgumentException($"La dirección de celda '{cellAddress}' no es válida.");

            string column = match.Groups[1].Value;
            int row = int.Parse(match.Groups[2].Value);

            return (column, row);
        }

        public static string BuildCellAddress(string column, int row)
        {
            return $"{column}{row}";
        }
    }
}