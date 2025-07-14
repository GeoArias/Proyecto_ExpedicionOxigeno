using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZXing;
using ZXing.Common;
using Newtonsoft.Json;
using Proyecto_ExpedicionOxigeno.Models;

namespace Proyecto_ExpedicionOxigeno.Services
{
    public class SelloService
    {
        private readonly ApplicationDbContext _context;
        private const int SELLOS_PARA_PASE = 5;

        public SelloService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Generar código QR para una reserva
        public async Task<string> GenerarCodigoQRAsync(string userId, string reservaId, string servicio)
        {
            var codigoQR = GenerarCodigoUnico();
            var fechaExpiracion = DateTime.Now.AddDays(30); // QR válido por 30 días

            var qr = new CodigoQR
            {
                Codigo = codigoQR,
                UserId = userId,
                ReservaId = reservaId,
                Servicio = servicio,
                FechaGeneracion = DateTime.Now,
                FechaExpiracion = fechaExpiracion,
                Validado = false
            };

            _context.CodigosQR.Add(qr);
            await _context.SaveChangesAsync();

            return codigoQR;
        }

        // Generar imagen QR
        public byte[] GenerarImagenQR(string codigo, int width = 300, int height = 300)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Height = height,
                    Width = width,
                    Margin = 1
                }
            };

            using (var bitmap = writer.Write(codigo))
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        // Validar código QR y otorgar sello
        public async Task<ResultadoValidacion> ValidarQRAsync(string codigoQR, string validadoPor, string ip)
        {
            var resultado = new ResultadoValidacion();

            try
            {
                // Buscar el código QR
                var qr = await _context.CodigosQR
                    .FirstOrDefaultAsync(q => q.Codigo == codigoQR);

                // Registrar el intento de validación
                var validacion = new ValidacionQR
                {
                    CodigoQR = codigoQR,
                    FechaIntento = DateTime.Now,
                    DireccionIP = ip,
                    ValidadoPor = validadoPor,
                    Exitoso = false
                };

                if (qr == null)
                {
                    validacion.MotivoFallo = "Código QR no encontrado";
                    resultado.Exito = false;
                    resultado.Mensaje = "El código QR no es válido";
                }
                else if (qr.Validado)
                {
                    validacion.MotivoFallo = "Código QR ya validado";
                    resultado.Exito = false;
                    resultado.Mensaje = "Este código QR ya ha sido utilizado";
                }
                else if (qr.FechaExpiracion < DateTime.Now)
                {
                    validacion.MotivoFallo = "Código QR expirado";
                    resultado.Exito = false;
                    resultado.Mensaje = "El código QR ha expirado";
                }
                else
                {
                    // Validación exitosa
                    qr.Validado = true;
                    qr.FechaValidacion = DateTime.Now;
                    qr.ValidadoPor = validadoPor;

                    // Crear el sello
                    var sello = new Sello
                    {
                        UserId = qr.UserId,
                        CodigoQR = codigoQR,
                        Servicio = qr.Servicio,
                        FechaObtencion = DateTime.Now,
                        ReservaId = qr.ReservaId,
                        UsadoEnPase = false
                    };

                    _context.Sellos.Add(sello);
                    validacion.Exitoso = true;

                    resultado.Exito = true;
                    resultado.Mensaje = "¡Sello otorgado exitosamente!";
                    resultado.SelloId = sello.Id;

                    // Verificar si el usuario ya tiene 5 sellos para generar pase
                    var sellosDisponibles = await _context.Sellos
                        .Where(s => s.UserId == qr.UserId && !s.UsadoEnPase)
                        .CountAsync();

                    if (sellosDisponibles >= SELLOS_PARA_PASE)
                    {
                        var paseGenerado = await GenerarPaseExpedicionAsync(qr.UserId);
                        resultado.PaseGenerado = true;
                        resultado.CodigoPase = paseGenerado.CodigoPase;
                        resultado.Mensaje += " ¡Felicidades! Has obtenido un pase a expedición.";
                    }
                    else
                    {
                        resultado.SellosRestantes = SELLOS_PARA_PASE - sellosDisponibles;
                        resultado.Mensaje += $" Te faltan {resultado.SellosRestantes} sellos para obtener un pase.";
                    }
                }

                _context.ValidacionesQR.Add(validacion);
                await _context.SaveChangesAsync();

                return resultado;
            }
            catch (Exception ex)
            {
                resultado.Exito = false;
                resultado.Mensaje = "Error interno del servidor";
                return resultado;
            }
        }

        // Generar pase de expedición
        private async Task<PaseExpedicion> GenerarPaseExpedicionAsync(string userId)
        {
            // Obtener los 5 sellos más antiguos no utilizados
            var sellosParaPase = await _context.Sellos
                .Where(s => s.UserId == userId && !s.UsadoEnPase)
                .OrderBy(s => s.FechaObtencion)
                .Take(SELLOS_PARA_PASE)
                .ToListAsync();

            // Marcar los sellos como utilizados
            foreach (var sello in sellosParaPase)
            {
                sello.UsadoEnPase = true;
            }

            // Crear el pase
            var pase = new PaseExpedicion
            {
                UserId = userId,
                CodigoPase = GenerarCodigoUnico(),
                FechaGeneracion = DateTime.Now,
                FechaExpiracion = DateTime.Now.AddDays(90), // Válido por 90 días
                Utilizado = false,
                SellosUsados = JsonConvert.SerializeObject(sellosParaPase.Select(s => s.Id).ToList())
            };

            _context.PasesExpedicion.Add(pase);
            await _context.SaveChangesAsync();

            return pase;
        }

        // Obtener estadísticas de sellos de un usuario
        public async Task<EstadisticasSellos> ObtenerEstadisticasAsync(string userId)
        {
            var sellosActivos = await _context.Sellos
                .Where(s => s.UserId == userId && !s.UsadoEnPase)
                .CountAsync();

            var sellosTotal = await _context.Sellos
                .Where(s => s.UserId == userId)
                .CountAsync();

            var pasesDisponibles = await _context.PasesExpedicion
                .Where(p => p.UserId == userId && !p.Utilizado && p.FechaExpiracion > DateTime.Now)
                .CountAsync();

            return new EstadisticasSellos
            {
                SellosActivos = sellosActivos,
                SellosTotal = sellosTotal,
                PasesDisponibles = pasesDisponibles,
                SellosRestantes = Math.Max(0, SELLOS_PARA_PASE - sellosActivos)
            };
        }

        private string GenerarCodigoUnico()
        {
            return Guid.NewGuid().ToString("N").ToUpper();
        }
    }

    // Clases auxiliares
    public class ResultadoValidacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public int SelloId { get; set; }
        public bool PaseGenerado { get; set; }
        public string CodigoPase { get; set; }
        public int SellosRestantes { get; set; }
    }

    public class EstadisticasSellos
    {
        public int SellosActivos { get; set; }
        public int SellosTotal { get; set; }
        public int PasesDisponibles { get; set; }
        public int SellosRestantes { get; set; }
    }

  
}