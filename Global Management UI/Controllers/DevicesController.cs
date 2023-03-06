using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Device_UI.Models;
using Global_Management_UI.Data;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http;

namespace Global_Management_UI.Controllers
{
    public class DevicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Devices
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {

            //update status for each device in the database
            var deviceDateChecker = await _context.Device.ToListAsync();
             for (int i = 0; i < deviceDateChecker.Count; i++)
            {
                string deviceDate = deviceDateChecker[i].End;
                var parsedDate = DateTime.Parse(deviceDateChecker[i].End.ToString());
                DateTime today = DateTime.Today;
                double finalTime = (parsedDate - today).TotalDays;

                if(finalTime <= 0)
                {
                    deviceDateChecker[i].Status = "Bad";
                    
                }
                else if(finalTime >= 0)
                    deviceDateChecker[i].Status = "Good";
            }
            await _context.SaveChangesAsync();


            if (searchString != null)
            {
                pageNumber = 1;
            }

            var devices = from s in _context.Device
                          select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                devices = devices.Where(s => s.Signature.Contains(searchString));
            }



            devices = devices.OrderBy(s => s.Status);
            int pageSize = 10;
            return View(await Paging<Device>.CreateAsync(devices.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Devices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Device
                .FirstOrDefaultAsync(m => m.ID == id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // GET: Devices/Create
        public IActionResult Create()
        {
            return View();
        }

 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Signature,Beginning,End,Status")] Device device)
        {
            if (ModelState.IsValid)
            {
                _context.Add(device);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // GET: Devices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Device.FindAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        // POST: Devices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Signature,Beginning,End,Status")] Device device)
        {
            if (id != device.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(device);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeviceExists(device.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(device);
        }

        // GET: Devices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var device = await _context.Device
                .FirstOrDefaultAsync(m => m.ID == id);
            if (device == null)
            {
                return NotFound();
            }

            return View(device);
        }

        // POST: Devices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var device = await _context.Device.FindAsync(id);
            _context.Device.Remove(device);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Generate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateReport(int id)
        {
            //get the device we are viewing
            var device = await _context.Device.FindAsync(id);

            //calculate the remianing duration on the device certificate
            var parsedDate = DateTime.Parse(device.End.ToString());
            DateTime today = DateTime.Today;
            double finalTime = (parsedDate - today).TotalDays;

            //allocate memory to create the download
            using (MemoryStream stream = new MemoryStream())
            {
                using var file = new StreamWriter(stream);

                //write data to file
                file.WriteLine("Device: " + device.Signature.ToString() + ".");
                file.WriteLine("Certificate good after  " + device.Beginning.ToString() + ".");
                file.WriteLine("Certificate good before " + device.End.ToString() + ".");
                file.Write(finalTime.ToString() + " days until expiration");
                file.Flush();
                file.Close();
                return File(stream.ToArray(), "text/plain", "Details_" + device.Signature.ToString() + ".txt");
            }
        }
        private bool DeviceExists(int id)
        {
            return _context.Device.Any(e => e.ID == id);
        }
    }
}
