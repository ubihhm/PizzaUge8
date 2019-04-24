using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pizza.Models;
using Pizza.Models.View;
using Pizza.Services;

namespace Pizza.Controllers
{
    public class BestillingController : Controller
    {
        readonly IDataBasen _dataBasen;
        readonly IEmailService _emailService;

        public BestillingController(IDataBasen dataBasen, IEmailService emailService)
        {
            _dataBasen = dataBasen;
            _emailService = emailService;
        }

        public ActionResult Bestilt()
        {
            return View();
        }

        // GET: Bestilling/Create
        public ActionResult Create()
        {
            BrugerModel bruger = new BrugerModel();
            if (User.Identity.IsAuthenticated)
            {
                try
                {
                    bruger = _dataBasen.HentBruger(User.Identity.Name);
                }
                catch 
                {
                    bruger.Brugernavn = "DetHerKommerTilAtGåGalt";
                }


                BestillingViewModel bestillingView = new BestillingViewModel { Brugernavn = bruger.Brugernavn, Meddelelse = "Velkommen " + bruger.Navn };
                return View(bestillingView);
            }
            else
                return RedirectToAction("Login", "Bruger");

        }

        // POST: Bestilling/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BestillingViewModel bestillingView)
        {
            bestillingView.Meddelelse = "";
            BrugerModel bruger = new BrugerModel();

            try
            {
                bruger = _dataBasen.HentBruger(User.Identity.Name);
            }
            catch
            {
                bruger.Brugernavn = "DetHerKommerTilAtGåGalt";
            }

            BestillingModel bestilling = new BestillingModel
            {
                Brugernavn = bruger.Brugernavn,
                AntalMargherita = bestillingView.AntalMargherita,
                AntalCapricciosa = bestillingView.AntalCapricciosa,
                AntalQuattroStagioni = bestillingView.AntalQuattroStagioni
            };

            try
            {
                _dataBasen.OpretBestilling(bestilling);
            }
            catch (DataBasenException dbe)
            {
                switch (dbe.Message)
                {
                    case "Brugernavn ukendt":
                        bestillingView.Meddelelse = "Brugernavnet findes ikke. Opret brugeren før du bestiller.";
                        break;
                    case "Brugernavn mangler":
                        bestillingView.Meddelelse = "Husk at udfylde Brugernavn.";
                        break;
                    default:
                        bestillingView.Meddelelse = dbe.Message;
                        break;
                }
                return View(bestillingView);
            }
            catch (Exception e)
            {
                bestillingView.Meddelelse = e.Message;
                return View(bestillingView);
            }

            StringBuilder tekst = new StringBuilder();
            tekst.AppendLine("Ny bestilling fra kl " + DateTime.Now.ToShortTimeString());
            tekst.AppendFormat("Antal Margherita: {0}", bestilling.AntalMargherita).AppendLine();
            tekst.AppendFormat("Antal Capricciosa: {0}", bestilling.AntalCapricciosa).AppendLine();
            tekst.AppendFormat("Antal Quattro Stagioni: {0}", bestilling.AntalQuattroStagioni).AppendLine();
            tekst.AppendLine();
            tekst.AppendLine("Bestillingen skal sendes til");
            tekst.AppendLine(bruger.Navn);
            tekst.AppendLine(bruger.Gade);
            tekst.AppendLine(bruger.Postnummer.ToString() + " " + bruger.Bynavn );

            try
            {
                _emailService.SendEmail("kokken@pizza.dk", "Ny bestilling", tekst.ToString()).Wait();
      
            }
            catch (Exception)
            {
                bestillingView.Meddelelse = "kokken har ikke modtaget din bestiling.";
            }

            return RedirectToAction(nameof(Bestilt));

        }

    }
}