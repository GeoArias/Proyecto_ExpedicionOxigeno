using System;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime ToCostaRicaTime(this DateTime utcDateTime)
        {
            // TimeZone de Costa Rica en Windows
            var crTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, crTimeZone);
        }
    }
}
