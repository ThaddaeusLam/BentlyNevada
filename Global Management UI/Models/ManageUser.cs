using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Global_Management_UI.Models
{
    public class ManageUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }

        public ManageUser()
        {

        }
    }
}
