using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Pizza.Models;
using Pizza.Models.View;
using Pizza.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Pizza.Controllers
{
    
    public class BrugerController : Controller
    {
        readonly IDataBasen _dataBasen;

        public BrugerController(IDataBasen dataBasen)
        {
            _dataBasen = dataBasen;
        }

        public ActionResult Login()
        {
            BrugerViewModel brugerView = new BrugerViewModel { visLink = false };
            return View(brugerView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(BrugerViewModel brugerView)
        {

            try
            {
                BrugerModel bruger = _dataBasen.HentBruger(brugerView.Brugernavn);

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
                    {
                    new Claim(ClaimTypes.Name,bruger.Brugernavn),
                    }, CookieAuthenticationDefaults.AuthenticationScheme);

                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                return RedirectToAction("Create", "Bestilling");
            }
            catch (DataBasenException dbe)
            {
                switch (dbe.Message)
                {
                    case "Brugernavn ukendt":
                        brugerView.Meddelelse = "Brugernavnet findes ikke.";
                        brugerView.visLink = true;
                        return View(brugerView);
                    default:
                        brugerView.Meddelelse = dbe.Message;
                        break;
                }
                return View(brugerView);
            }
            catch (Exception e)
            {
                brugerView.Meddelelse = e.Message;
                return View(brugerView);
            }
        }


        // GET: Bruger/Create
        public ActionResult Create()
        {
            BrugerViewModel brugerView = new BrugerViewModel();
            return View(brugerView);
        }

        // POST: Bruger/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(BrugerViewModel brugerView)
        {
            BrugerModel bruger = new BrugerModel
            {
                Brugernavn = brugerView.Brugernavn,
                Navn = brugerView.Navn,
                Gade = brugerView.Gade,
                Postnummer = brugerView.Postnummer,
                Bynavn = brugerView.Bynavn
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name,brugerView.Brugernavn),
                }, CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            try
            {
                _dataBasen.OpretBruger(bruger);
                return RedirectToAction("Create", "Bestilling");
            }
            catch (ArgumentException)
            {
                brugerView.Meddelelse = "Brugernavn findes allerede";
                return View(brugerView);
            }
            catch (Exception e)
            {
                brugerView.Meddelelse = e.Message;
                return View(brugerView);
            }
        }
    }
}