//Auto-generated based on code written by Caleb & Nathaniel
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Device_UI.Models;
using Global_Management_UI.Models;


namespace Global_Management_UI.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        
        }
        public DbSet<Device_UI.Models.Device> Device { get; set; }
        public DbSet<Global_Management_UI.Models.ManageUser> ManageUser { get; set; }
        public DbSet<Device_UI.Models.DeviceHistory> DeviceHistory { get; set; }
    }
}
