using Microsoft.AspNet.Identity;
using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Services;
using System;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
            var userId = User.Identity.GetUserId();

            var estadisticas = await _selloService.ObtenerEstadisticasAsync(userId);

            var sellos = await _context.Sellos
                .Where(s => s.UserId == userId)
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
        public ActionResult GenerarQR(string codigo)
        {
            var imagen = _selloService.GenerarImagenQR(codigo);
            return File(imagen, "image/png");
        }

        // Vista para escanear QR (para staff)
        public ActionResult EscanearQR()
        {
            return View();
        }

        // Eliminar el último sello de un usuario (solo para administradores)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarUltimoSelloUsuario(string userId)
        {
            if (!User.IsInRole("Administrador"))
                return new HttpUnauthorizedResult();

            using (var db = new ApplicationDbContext())
            {
                var usuario = db.Users.FirstOrDefault(u => u.Id == userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                var ultimoSello = db.Sellos
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.FechaObtencion)
                    .FirstOrDefault();

                if (ultimoSello == null)
                {
                    TempData["Error"] = "El usuario no tiene sellos para eliminar.";
                    return RedirectToAction("Index");
                }

                db.Sellos.Remove(ultimoSello);
                await db.SaveChangesAsync();
                TempData["Mensaje"] = "Último sello eliminado correctamente.";
                return RedirectToAction("Index");
            }
        }

        // Eliminar sello por ID (solo para administradores) - se mantiene para historial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EliminarSello(int id)
        {
            if (!User.IsInRole("Administrador"))
                return new HttpUnauthorizedResult();

            using (var db = new ApplicationDbContext())
            {
                var sello = db.Sellos.FirstOrDefault(s => s.Id == id);
                if (sello == null)
                {
                    TempData["Error"] = "Sello no encontrado.";
                    return RedirectToAction("Index");
                }
                db.Sellos.Remove(sello);
                await db.SaveChangesAsync();
                TempData["Mensaje"] = "Sello eliminado correctamente.";
                return RedirectToAction("Index");
            }
        }

        // Otorgar sello manualmente (solo para administradores)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> OtorgarSelloManual(string userId, string servicio = "Manual", string reservaId = "Manual")
        {
            if (!User.IsInRole("Administrador"))
                return new HttpUnauthorizedResult();

            using (var db = new ApplicationDbContext())
            {
                var usuario = db.Users.FirstOrDefault(u => u.Id == userId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                var sello = new Sello
                {
                    UserId = userId,
                    Servicio = string.IsNullOrWhiteSpace(servicio) ? "Manual" : servicio,
                    FechaObtencion = DateTime.Now,
                    ReservaId = string.IsNullOrWhiteSpace(reservaId) ? "Manual" : reservaId,
                    UsadoEnPase = false,
                    CodigoQR = Guid.NewGuid().ToString("N")
                };
                db.Sellos.Add(sello);
                try
                {
                    await db.SaveChangesAsync();
                    TempData["Mensaje"] = "Sello manual asignado satisfactoriamente al usuario elegido.";
                }
                catch (DbEntityValidationException ex)
                {
                    var errors = ex.EntityValidationErrors
                        .SelectMany(e => e.ValidationErrors)
                        .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                        .ToList();
                    TempData["Error"] = "Error de validación: " + string.Join(", ", errors);
                }
                return RedirectToAction("Index");
            }
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