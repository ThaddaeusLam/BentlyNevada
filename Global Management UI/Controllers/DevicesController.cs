//Code created by Nathaniel McFadden

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Device_UI.Models;
using Global_Management_UI.Data;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using OrbitProtocol.Infrastructure;
using OrbitSeries.Protocol;
using System.Security.Cryptography.X509Certificates;
using System.Security;
using System.Net.Sockets;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Global_Management_UI.Data.Migrations;

namespace Global_Management_UI.Controllers
{
    public class DevicesController : Controller
    {

        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }
        private static ISFClient CreateClient()
        {
            return new SFDeviceClient();
        }

        public bool UpdateDeviceHistory(string deviceSignature, DateTime today, string userName, string action)
        {
            var DHistory = new Device_UI.Models.DeviceHistory();

            DHistory.UserWhoEdited = userName;
            DHistory.DeviceSignature = deviceSignature;
            DHistory.actionTaken = action;
            DHistory.AlterationDate = today;
            _context.DeviceHistory.Add(DHistory);
            _context.SaveChanges();
            return true;
        }
        private static (string[], string[], string[], string[]) QueryDevice(ISFClient client, string ipAdress, int port)
        {
            //markers to see if we found the data we need for storage
            bool sawSig = false;
            bool sawInitial = false;
            bool sawEnd = false;
            bool sawCreator = false;

            //strings to hold our data
            //markers to see if we found the data we need for storage

            //strings to hold our data
            string[] initial = new string[1];
            string[] end = new string[1];
            string[] signiture = new string[1];
            string[] creator = new string[1];
            var deviceInfo = client.QueryDeviceInfo(ipAdress, port);
            string values = deviceInfo.DeviceCertificate.ToString();
            Console.WriteLine(values);
            //Search for the data we need
            using (var reader = new StringReader(values))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    if (line == "[Serial Number]")
                    {
                        sawSig = true;
                        continue;
                    }

                    if (line == "[Not Before]")
                    {
                        sawInitial = true;
                        continue;
                    }

                    if (line == "[Not After]")
                    {
                        sawEnd = true;
                        continue;
                    }

                    if (line == "[Issuer]")
                    {
                        sawCreator = true;
                        continue;
                    }
                    if (sawSig == true)
                    {
                        signiture[0] = line;
                        sawSig = false;
                    }

                    if (sawInitial == true)
                    {
                        initial[0] = line;
                        sawInitial = false;
                    }

                    if (sawCreator == true)
                    {
                        creator[0] = line;
                        sawCreator = false;
                    }
                    if (sawEnd == true)
                    {
                        end[0] = line;
                        sawEnd = false;
                    }

                }
            }
            return (signiture, initial, creator, end);
        }

        private static SecureString CreateSucureString(string password)
        {
            var securestring = new SecureString();

            foreach (var charater in password)
            {
                securestring.AppendChar(charater);
            }

            return securestring;
        }
        private static bool ConnectWithCredentials(ISFClient client, string ipAdress, int port, string username, SecureString password)
        {

            var options = new ClientOptions() { PortNumber = port, ServerAddress = ipAdress, TimeOut = 0 };

            X509Certificate2 x509Certificate = null;

            var clientType = new Guid("94C51BAC-3AD8-4768-941A-CD39B7C36CD8"); //JFM
                                                                               //try
                                                                               //{
            var conectionResult = client.Connect(options, new ClientSecurityOptions(), username, password, clientType, (cert) =>
            {
                x509Certificate = cert;
                return true;
            });
            if (conectionResult.ConnectionStatus == ClientConnectionStatus.Success)
                Console.WriteLine("Poleas");
            else
            {
                Console.WriteLine("Failed");
                return false;
            }
            return true;
        }
        private static bool UpdateCertificate(ISFClient sFClient, byte[] certFile, byte[] keyFile)
        {
            var result = sFClient.UpdateCertificate(certFile, keyFile);
            //sFClient.IsConnected
            return result.MessageType != CommandIds.StandardErrorResponse;
        }

        // GET: Devices
        public async Task<IActionResult> Index(string searchString, string currentFilter, int? pageNumber, string sortOrder)
        {
            //<a asp-action="Delete" asp-route-id="@item.ID">Delete</a>
            var history = await _context.DeviceHistory.ToListAsync();
            //update status for each device in the database
            ViewBag.CurrentSort = sortOrder;
            ViewData["Currentsort"] = sortOrder; 
            ViewData["CurrentFilter"] = searchString;
            
            var deviceDateChecker = await _context.Device.ToListAsync();
            for (int i = 0; i < deviceDateChecker.Count; i++)
            {
                DateTime deviceDate = deviceDateChecker[i].End;
                var parsedDate = DateTime.Parse(deviceDateChecker[i].End.ToString());
                DateTime today = DateTime.Today;
                double finalTime = (parsedDate - today).TotalDays;

                if (finalTime <= 0)
                {
                    deviceDateChecker[i].Status = "Expired";

                }
                else if (finalTime <= 90 && finalTime > 0)
                    deviceDateChecker[i].Status = "Close To Expiration";
                else if (finalTime > 0)
                {
                    deviceDateChecker[i].Status = "Valid";
                }

            }
            await _context.SaveChangesAsync();

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;
            var devices = from s in _context.Device
                          select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                devices = devices.Where(s => s.Signature.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "Sig":
                    devices = devices.OrderBy(s => s.Signature);
                    break;
                case "Beg":
                    devices = devices.OrderBy(s => s.Beginning);
                    break;
                case "End":
                    devices = devices.OrderBy(s => s.End);
                    break;
                case "Status":
                    devices = devices.OrderBy(s => s.Status);
                    break;
                default:
                    devices = devices.OrderBy(s => s.End);
                    break;
            }

            int pageSize = 10;
            return View(await Paging<Device>.CreateAsync(devices.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        public async Task<IActionResult> History(string searchString, string currentFilter, int? pageNumber, string sortOrder)
        {
            //update status for each device in the database
            ViewBag.CurrentSort = sortOrder;
            ViewData["Currentsort"] = sortOrder;
            ViewData["CurrentFilter"] = searchString;

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;
            var deviceHistory = from s in _context.DeviceHistory
                          select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                deviceHistory = deviceHistory.Where(s => s.DeviceSignature.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "Sig":
                    deviceHistory = deviceHistory.OrderBy(s => s.DeviceSignature);
                    break;
                case "Action":
                    deviceHistory = deviceHistory.OrderBy(s => s.actionTaken);
                    break;
                case "Time":
                    deviceHistory = deviceHistory.OrderByDescending(s => s.AlterationDate);
                    break;
                case "User":
                    deviceHistory = deviceHistory.OrderBy(s => s.UserWhoEdited);
                    break;
                default:
                    deviceHistory = deviceHistory.OrderByDescending(s => s.AlterationDate);
                    break;
            }

            int pageSize = 10;
            return View(await Paging< Device_UI.Models.DeviceHistory>.CreateAsync(deviceHistory.AsNoTracking(), pageNumber ?? 1, pageSize));
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
            var deviceHistory = from s in _context.DeviceHistory
                                select s;

            deviceHistory = deviceHistory.Where(s => s.DeviceSignature.Contains(device.Signature));
            deviceHistory = deviceHistory.OrderByDescending(s => s.AlterationDate).Take(5);
            ViewData["History"] = deviceHistory;

            return View(device);
        }

        [Authorize(Roles = "Admin,Sysadmin")]
        // GET: Devices/Create
        public IActionResult Create()
        {
            return View();

        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Signature,Beginning,End,Status, IP, Port")] Device device)
        {
            bool noError = true;
            bool duplicate = false;
            var deviceNames = from s in _context.Device
                                select s;
            if (ModelState.IsValid)
            {
                deviceNames = deviceNames.Where(s => s.Signature.Contains(device.Signature.ToString()));
                if (deviceNames.ToList().Count >= 1)
                {
                    duplicate = true;
                }
                //attempt connection to the port specified
                int connectionPort = device.Port;
                string IP = device.IP;
                var client = CreateClient();
                var password = CreateSucureString("Lets go Pack!");
                try
                {
                    var data = QueryDevice(client, "127.0.0.1", connectionPort);

                }
                catch (SocketException e)
                {
                    noError = false;
                }

                //bool noError = ConnectWithCredentials(client, "127.0.0.1", connectionPort, "SFAdmin", password);
                if (noError == true && duplicate == false)
                {
                    var certificateData = QueryDevice(client, "127.0.0.1", connectionPort);
                    device.Beginning = DateTime.Parse(certificateData.Item2[0]);
                    device.creator = certificateData.Item3[0];
                    device.End = DateTime.Parse(certificateData.Item4[0]);
                    _context.Add(device);
                    await _context.SaveChangesAsync();

                    string userName = User.Identity.Name;
                    string action = "Created Device";
                    string deviceSignature = device.Signature.ToString();
                    DateTime today = DateTime.Now;
                    UpdateDeviceHistory(deviceSignature, today, userName, action);
                    return RedirectToAction(nameof(Index));
                }
                else if(duplicate == true)
                {
                    await Response.WriteAsync("That device signature already exists on the Network. Please enter another.");
                }
                else if (!noError)
                {
                    await Response.WriteAsync("The system failed to connect to the specified device. " +
                        "Please make sure the device is properly connected");
                }



            }

            return View(device);
        }
        [Authorize(Roles = "Admin,Sysadmin")]
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
        public async Task<IActionResult> Edit(int id, Device device, List<IFormFile> files)
        {
            var currentPerson = _context.Device.FirstOrDefault(p => p.ID == device.ID);

            //files for uploading to the simulator
            long size = files.Sum(f => f.Length);
            var bothFiles = new List<string>();
            byte[] keyBytes;
            byte[] certBytes;
            bool noError = true;
            bool correctExtensions = true;

            //files for establishing a conneciton to the simulator
            int connectionPort = currentPerson.Port;
            Console.WriteLine(currentPerson.Port);
            string IP = currentPerson.IP;
            var client = CreateClient();
            var password = CreateSucureString("Lets go Pack!");
            try
            {
                var data = QueryDevice(client, IP, connectionPort);
            }
            catch (SocketException e)
            {
                noError = false;
            }
            if (id != device.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                //populate file paths and upload to the web storage 
                string certExt = System.IO.Path.GetExtension(files[0].FileName);
                string keyExt = System.IO.Path.GetExtension(files[1].FileName);
                Console.WriteLine(certExt);
                if(certExt != ".pem" || keyExt != ".pem")
                {
                    correctExtensions = false;
                }
                var filePaths = new List<string>();


                if (noError == true && correctExtensions == true)
                {
                   
                    foreach (var formFile in files)
                    {
                        if (formFile.Length > 0)
                        {

                            // full path to file in temp location
                            var filePath = Path.Combine(Directory.GetCurrentDirectory() + "/Certificates/", formFile.FileName); //we are using Temp file name just for the example. Add your own file path.
                            filePaths.Add(filePath);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await formFile.CopyToAsync(stream);
                            }
                        }
                    }

                    //convert to byte to upload to the certificate
                    using (var cert = new MemoryStream())
                    {
                       
                        await files[0].CopyToAsync(cert);
                        certBytes = cert.ToArray();

                    }
                    using (var key = new MemoryStream())
                    {
                       
                        await files[1].CopyToAsync(key);
                        keyBytes = key.ToArray();
                    }

                    //update certificate on device
                    ConnectWithCredentials(client, IP, connectionPort, "SFAdmin", password);
                    getConfigLock(client);
                    bool certificateUpdated = UpdateCertificate(client, certBytes, keyBytes);
                    Console.WriteLine(certificateUpdated);

                    if(certificateUpdated == false)
                    {
                        await Response.WriteAsync("The system failed to upload the files provided, " +
                            "please ensure that they are certificate files (pem) and that the first file is the certificate and the second file the certificate key.");
                    }
                    else
                    {
                        client.Disconnect();
                        var certificateData = QueryDevice(client, IP, connectionPort);
                        currentPerson.Beginning = DateTime.Parse(certificateData.Item2[0]);
                        currentPerson.creator = certificateData.Item3[0];
                        currentPerson.End = DateTime.Parse(certificateData.Item4[0]);
                 
                        await _context.SaveChangesAsync();
                        await Response.WriteAsync("The Device has successfully updated the certificate.");
                        
                    }

                }
                else if (correctExtensions == false)
                {
                    await Response.WriteAsync("The certificate files are not in the correct format. Please submit PEM or pem files.");
                }
                else if (noError == false)
                {
                    await Response.WriteAsync("The system failed to connect to the specified device.\n " +
                        "Please make sure the device is properly connected");

                }
                

            }


            return View(device);
        }
        public bool getConfigLock(ISFClient sFClient)
        {
            var result = sFClient.LockConfiguration(1);
            return true;
        }

        [Authorize(Roles = "Admin,Sysadmin")]
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

            string userName = User.Identity.Name;
            string action = "Deleted Device";
            string deviceSignature = device.Signature.ToString();
            DateTime today = DateTime.Now;
            UpdateDeviceHistory(deviceSignature, today, userName, action);
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
                file.WriteLine("Creator: " + device.creator + ".");
                file.WriteLine("IP Adress: " + device.IP + ".");
                file.WriteLine("Port Number " + device.Port + ".");
                file.WriteLine("Certificate good after  " + device.Beginning.ToString() + ".");
                file.WriteLine("Certificate good before " + device.End.ToString() + ".");
                if (finalTime > 0)
                    file.Write(finalTime.ToString() + " days until expiration");
                else
                    file.Write("This Device's certificate has expired.");
                file.Flush();
                file.Close();

                //add this generated report to the history table
                string userName = User.Identity.Name;
                string action = "Generated Details Report";
                string deviceSignature = device.Signature.ToString();

                today = DateTime.Now;
                UpdateDeviceHistory(deviceSignature, today, userName, action);

                return File(stream.ToArray(), "text/plain", "Details_" + device.Signature.ToString() + "_" + today + ".txt");

            }
        }

        [HttpPost, ActionName("Mass Generate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMReport()
        {
            //get the device we are viewing
            DateTime today = DateTime.Now;
            var deviceIterator = await _context.Device.ToListAsync();
            var csv = new StringBuilder();
            //allocate memory to create the download
            using (MemoryStream stream = new MemoryStream())
            {
                using var file = new StreamWriter(stream);
                file.WriteLine("Device,Action,Time,User");
                for (int i = 0; i < deviceIterator.Count; i++)
                {
                    var first = deviceIterator[i].Signature;
                    var second = deviceIterator[i].Port;
                    var third = deviceIterator[i].IP;
                    var fourth = deviceIterator[i].Beginning;
                    var fifth = deviceIterator[i].End;
                    var sixth = deviceIterator[i].creator;
                    var line = string.Format("{0},{1},{2},{3},{4},{5}", first, second, third, fourth, fifth, sixth);
                    file.WriteLine(line);
                    file.Flush();
                }
                return File(stream.ToArray(), "text/plain", "Mass_Report_" + today +".csv");
            }
        }
        [HttpPost, ActionName("DHReport")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDHReport(int id)
        {
            //get the device we are viewing
            var device = await _context.Device.FindAsync(id);
            var deviceHistory = from s in _context.DeviceHistory
                                select s;
            deviceHistory = deviceHistory.Where(s => s.DeviceSignature.Contains(device.Signature));
            deviceHistory = deviceHistory.OrderByDescending(s => s.AlterationDate);
            var reportData = deviceHistory.ToList();
            var csv = new StringBuilder();
            //allocate memory to create the download
            using (MemoryStream stream = new MemoryStream())
            {
                using var file = new StreamWriter(stream);
                file.WriteLine("Action,Date,User");
                for (int i = 0; i < reportData.Count; i++)
                {
                    var first = reportData[i].actionTaken;
                    var second = reportData[i].AlterationDate;
                    var third = reportData[i].UserWhoEdited;
                    var line = string.Format("{0},{1},{2}", first, second, third);
                    file.WriteLine(line);
                    file.Flush();
                }
                //add this generated report to the history table
                string userName = User.Identity.Name;
                string action = "Generated History Report";
                string deviceSignature = device.Signature.ToString();
                DateTime today = DateTime.Now;
                UpdateDeviceHistory(deviceSignature, today, userName, action);
                return File(stream.ToArray(), "text/plain", "History_" + device.Signature.ToString() + "_" + today + ".csv");
            }
        }
        [HttpPost, ActionName("Generate History")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateHReport()
        {
            //get the device we are viewing
            DateTime today = DateTime.Now;
            var deviceIterator = await _context.DeviceHistory.ToListAsync();
            var csv = new StringBuilder();
            //allocate memory to create the download
            using (MemoryStream stream = new MemoryStream())
            {
                using var file = new StreamWriter(stream);
                file.WriteLine("Device,Action,Time,User");
                for (int i = 0; i < deviceIterator.Count; i++)
                {
                    var first = deviceIterator[i].DeviceSignature;
                    var second = deviceIterator[i].actionTaken;
                    var third = deviceIterator[i].AlterationDate;
                    var fourth = deviceIterator[i].UserWhoEdited;

                    var line = string.Format("{0},{1},{2},{3}", first, second, third, fourth);
                    file.WriteLine(line);
                    file.Flush();
                }
                return File(stream.ToArray(), "text/plain", "History_Mass_Report_" + today +".csv");
            }
        }

        [HttpPost, ActionName("Query")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QueryUpload(int id)
        {
            bool noError = true;
            //get the device we are viewing
            var device = await _context.Device.FindAsync(id);

            //connect to simulator
            int connectionPort = device.Port;
            Console.WriteLine(device.Port);
            string IP = device.IP;
            var client = CreateClient();
            try
            {
                var test = QueryDevice(client, IP, connectionPort);
            }
            catch (SocketException e)
            {
                noError = false;
            }
            if (noError)
            {
                var data = QueryDevice(client, IP, connectionPort);
                //add the data
                device.Beginning = DateTime.Parse(data.Item2[0]);
                device.creator = data.Item3[0];
                device.End = DateTime.Parse(data.Item4[0]);
                await Response.WriteAsync("The data has been successfuly copied to the applicaiton.");
                await _context.SaveChangesAsync();

                string userName = User.Identity.Name;
                string action = "Renewed Certificate. Good before: " + device.End.ToString();
                string deviceSignature = device.Signature.ToString();
                DateTime today = DateTime.Now;
                UpdateDeviceHistory(deviceSignature, today, userName, action);
            }
            else
            {
                await Response.WriteAsync("The system failed to connect to the database.\n " +
                       "Please retry the connection with the device still connect to the system.");
            }

            //return view
            return RedirectToAction(nameof(Index));

        }

        private bool DeviceExists(int id)
        {
            return _context.Device.Any(e => e.ID == id);
        }
    }
}
