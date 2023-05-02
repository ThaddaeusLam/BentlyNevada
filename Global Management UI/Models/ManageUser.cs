//This code was modified by Caleb Stickler
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
        public DateTime LastAccessed { get; set; }
        public string CreationDate { get; set; }
        public string LastPasswordChange { get; set; }

        public ManageUser()
        {

        }
    }
}
