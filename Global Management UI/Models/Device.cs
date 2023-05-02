using System;
//Code created by Nathaniel McFadden
namespace Device_UI.Models
{
    public class Device
    {
        public int  ID { get; set; }
        public string Signature { get; set;}
        public DateTime Beginning { get; set;}
        public DateTime End { get; set;}
        public string Status { get; set;}
        public string IP { get; set; }
        public int Port { get; set; }
        public string creator { get; set; }
        public string lastUpdatedID { get; set; }
        
        public Device()
        {

        }
    }
}
