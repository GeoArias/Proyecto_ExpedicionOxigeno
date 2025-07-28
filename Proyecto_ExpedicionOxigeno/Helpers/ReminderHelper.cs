using Proyecto_ExpedicionOxigeno.Models;
using Proyecto_ExpedicionOxigeno.Controllers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Proyecto_ExpedicionOxigeno.Helpers
{
    public class ReminderHelper
    {
        public static async Task EnviarRecordatoriosAsync()
        {
            var db = new ApplicationDbContext();
            var ahora = DateTime.Now;
            var desde = ahora.AddHours(47);
            var hasta = ahora.AddHours(49);

            // Buscar sellos/reservas que ocurren en 48h
            var sellos = await db.Sellos
                .Where(s => DbFunctions.DiffHours(ahora, s.FechaObtencion) >= 47 &&
                            DbFunctions.DiffHours(ahora, s.FechaObtencion) <= 49 &&
                            !s.UsadoEnPase)
                .ToListAsync();

            foreach (var sello in sellos)
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == sello.UserId);
                if (user == null) continue;

                // Obtener la reserva desde Bookings
                var reservas = await MSBookings_Actions.GetAppointmentsByEmail(user.Email);
                var reserva = reservas.FirstOrDefault(r => r.Id == sello.ReservaId);
                if (reserva == null) continue;

                await ReservasController.EnviarCorreoRecordatorio(user.Email, reserva);
            }
        }
    }
}