using System;

namespace Proyecto_ExpedicionOxigeno.Models
{
    public static class GraphTokenCache
    {
        private static string _token;
        private static DateTime _expiresOn = DateTime.MinValue;
        private static readonly object _lock = new object();

        public static string Token
        {
            get
            {
                lock (_lock)
                {
                    return _token;
                }
            }
            set
            {
                lock (_lock)
                {
                    _token = value;
                }
            }
        }

        public static DateTime ExpiresOn
        {
            get
            {
                lock (_lock)
                {
                    return _expiresOn;
                }
            }
            set
            {
                lock (_lock)
                {
                    _expiresOn = value;
                }
            }
        }

        public static bool IsTokenValid()
        {
            lock (_lock)
            {
                // Consider token valid if it expires in more than 2 minutes
                return !string.IsNullOrEmpty(_token) && _expiresOn > DateTime.UtcNow.AddMinutes(2);
            }
        }
    }
}
