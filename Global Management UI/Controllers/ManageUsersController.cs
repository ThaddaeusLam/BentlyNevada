//This code was modified by Caleb Stickler

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Global_Management_UI.Data;
using Global_Management_UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Global_Management_UI.Controllers
{
    [Authorize(Roles = "Sysadmin,Admin")]
    public class ManageUsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userMgr;
        private readonly SignInManager<IdentityUser> _signInMgr;
        public ManageUsersController(ApplicationDbContext context, UserManager<IdentityUser> userMgr, SignInManager<IdentityUser> signInMgr)
        {
            _context = context;
            _userMgr = userMgr;
            _signInMgr = signInMgr;
        }

        public async Task<IActionResult> Index(string sortOrder, int? page)
        {
            var tableData = _context.ManageUser;

            if (tableData.Count() == 0)
            {
                var usersTable = _context.Users
                    .Join(_context.UserRoles, c => c.Id, cd => cd.UserId, (c, cd) => new { c, cd })
                    .Join(_context.Roles, cd => cd.cd.RoleId, d => d.Id, (cd, d) => new { cd, d })
                    .Select(a => new ManageUser()
                    {
                        Username = a.cd.c.UserName,
                        Role = a.d.Name
                    });

                int j = 1; //primary key set to 1

                foreach (var i in usersTable)
                {
                    var ur = new ManageUser()
                    {
                        Username = i.Username,
                        Role = i.Role,
                    };

                    var urol = _context.ManageUser.FirstOrDefault(u => u.Id == j);
                    if (urol != null)
                    {
                        urol.Username = ur.Username;
                        urol.Role = ur.Role;
                        _context.ManageUser.Update(urol);
                        j++; //increment primary key
                    }
                    else
                        _context.ManageUser.Add(ur);
                }
                _context.SaveChanges();
            }

            var usersList = from u in _context.ManageUser select u;

            switch (sortOrder)
            {
                case "Role":
                    usersList = usersList.OrderBy(u => u.Role);
                    break;
                case "Username":
                    usersList = usersList.OrderBy(u => u.Username);
                    break;
                case "Date":
                    usersList = usersList.OrderByDescending(u => u.LastAccessed);
                    break;
                default:
                    usersList = usersList.OrderBy(u => u.Id);
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(await Paging<ManageUser>.CreateAsync(usersList.AsNoTracking(), pageNumber, pageSize));
        }

        // GET: ManageUsers/Create
        public IActionResult Create()
        {
            return Redirect("~/Identity/Account/Register");
        }

        // GET: ManageUsers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manageUser = await _context.ManageUser.FindAsync(id);

            if (manageUser == null)
            {
                return NotFound();
            }

            if ((manageUser.Role == "Admin" || manageUser.Role == "Sysadmin") && User.IsInRole("Admin"))
            {
                return Redirect("~/Identity/Account/AccessDenied");
            }

            return View(manageUser);
        }

        // POST: ManageUsers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Role,LastAccessed,CreationDate,LastPasswordChange")] ManageUser manageUser)
        {
            if (id != manageUser.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(manageUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ManageUserExists(manageUser.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                //user manager to modify role
                if (manageUser.Role == "Admin")
                {
                    await _userMgr.AddToRoleAsync(await _userMgr.FindByNameAsync(manageUser.Username), "Admin");
                    await _userMgr.RemoveFromRoleAsync(await _userMgr.FindByNameAsync(manageUser.Username), "Display");
                }
                else if(manageUser.Role == "Display")
                {
                    await _userMgr.AddToRoleAsync(await _userMgr.FindByNameAsync(manageUser.Username), "Display");
                    await _userMgr.RemoveFromRoleAsync(await _userMgr.FindByNameAsync(manageUser.Username), "Admin");
                }

                return RedirectToAction(nameof(Index));
            }
            return View(manageUser);
        }

        // GET: ManageUsers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var manageUser = await _context.ManageUser
                .FirstOrDefaultAsync(m => m.Id == id);

            if (manageUser == null)
            {
                return NotFound();
            }

            if ((manageUser.Role == "Admin" || manageUser.Role == "Sysadmin") && User.IsInRole("Admin"))
            {
                return Redirect("~/Identity/Account/AccessDenied");
            }

            return View(manageUser);
        }

        // POST: ManageUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manageUser = await _context.ManageUser.FindAsync(id);
            _context.ManageUser.Remove(manageUser);
            await _context.SaveChangesAsync();
            await _userMgr.DeleteAsync(await _userMgr.FindByNameAsync(manageUser.Username));
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UserInfo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.ManageUser
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        private bool ManageUserExists(int id)
        {
            return _context.ManageUser.Any(e => e.Id == id);
        }
    }
}
