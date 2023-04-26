//code by Nathaniel McFadden
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Device_UI.Models
{
    public class DeviceHistory
    {
        public int ID { get; set; }
        public string DeviceSignature { get; set; }
        public string actionTaken { get; set; }
        public DateTime AlterationDate {get; set;}
        public string UserWhoEdited { get; set; }

        public DeviceHistory()
        {

        }
    }
}
