using System;

namespace V5.ConsoleTest
{
    public class WebRequest
    {
        public DateTime EventTime { get; set; }

        public int ClientIP { get; set; }
        public string UserName { get; set; }

        public string ServerName { get; set; }
        public ushort ServerPort { get; set; }
        public string HttpMethod { get; set; }
        public string UriStem { get; set; }
        //public string UriQuery { get; set; }

        public ushort HttpStatus { get; set; }
        public int? RequestBytes { get; set; }
        public int ResponseBytes { get; set; }
        public float TimeTakenMs { get; set; }
        public string Protocol { get; set; }
        public bool WasEncrypted { get; set; }
        public bool WasCachedResponse { get; set; }

        public string ClientRegion { get; set; }
        public string ClientBrowser { get; set; }
        public string ClientOs { get; set; }
        public string DataCenter { get; set; }

        public Guid? UserGuid { get; set; }
        public bool? IsPremiumUser { get; set; }
        public ushort? DaysSinceJoined { get; set; }

        //public string ErrorStack { get; set; }
    }
}
