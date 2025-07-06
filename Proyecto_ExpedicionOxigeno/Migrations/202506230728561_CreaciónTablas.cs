namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class CreaciónTablas1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Contactos",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Consulta = c.String(nullable: false, maxLength: 1000),
                    Nombre = c.String(nullable: false, maxLength: 100),
                    Telefono = c.String(nullable: false, maxLength: 20),
                    Email = c.String(nullable: false, maxLength: 100),
                    Fecha = c.DateTime(nullable: false),
                    Respondida = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.PuntosFidelidads",
                c => new
                {
                    IdPunto = c.Int(nullable: false, identity: true),
                    IdUsuario = c.String(nullable: false, maxLength: 128),
                    FechaAsignacion = c.DateTime(nullable: false),
                    CantidadPuntos = c.Int(nullable: false),
                    Motivo = c.String(nullable: false, maxLength: 255),
                    IdEstado = c.Int(nullable: false),
                })
                .PrimaryKey(t => t.IdPunto)
                .ForeignKey("dbo.Estadoes", t => t.IdEstado, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.IdUsuario, cascadeDelete: true)
                .Index(t => t.IdUsuario)
                .Index(t => t.IdEstado);

            CreateTable(
                "dbo.Reviews",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Nombre = c.String(nullable: false),
                    Comentario = c.String(nullable: false),
                    Calificacion = c.Int(nullable: false),
                    Fecha = c.DateTime(nullable: false),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropForeignKey("dbo.PuntosFidelidads", "IdUsuario", "dbo.AspNetUsers");
            DropForeignKey("dbo.PuntosFidelidads", "IdEstado", "dbo.Estadoes");
            DropIndex("dbo.PuntosFidelidads", new[] { "IdEstado" });
            DropIndex("dbo.PuntosFidelidads", new[] { "IdUsuario" });
            DropTable("dbo.Reviews");
            DropTable("dbo.PuntosFidelidads");
            DropTable("dbo.Contactos");
        }
    }
}
