using Microsoft.AspNet.Identity;
using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno.Controllers
{
    [Authorize]
    public class SelloController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly SelloService _selloService;

        public SelloController()
        {
            _context = new ApplicationDbContext();
            _selloService = new SelloService(_context);
        }

        // Vista principal de sellos del usuario
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId(); // <-- SIEMPRE usa el ID aquí

            var estadisticas = await _selloService.ObtenerEstadisticasAsync(userId);

            var sellos = await _context.Sellos
                .Where(s => s.UserId == userId) // <-- Aquí también
                .OrderByDescending(s => s.FechaObtencion)
                .ToListAsync();

            ViewBag.Estadisticas = estadisticas;
            return View(sellos);
        }

        // Validar código QR (endpoint para staff)
        [HttpPost]
        public async Task<ActionResult> ValidarQR(string codigoQR)
        {
            var validadoPor = User.Identity.GetUserId();
            var ip = Request.UserHostAddress;

            var resultado = await _selloService.ValidarQRAsync(codigoQR, validadoPor, ip);

            return Json(resultado);
        }

        // Generar imagen QR
        public async Task<ActionResult> GenerarQR(string codigo)
        {
            var imagen = _selloService.GenerarImagenQR(codigo);
            return File(imagen, "image/png");
        }

        // Vista para escanear QR (para staff)
        public ActionResult EscanearQR()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}