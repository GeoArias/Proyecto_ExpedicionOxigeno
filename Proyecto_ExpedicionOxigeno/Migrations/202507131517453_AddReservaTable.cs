namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReservaTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Reservas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Usuario = c.String(nullable: false, maxLength: 256),
                        Nombre = c.String(nullable: false, maxLength: 256),
                        TipoActividad = c.String(nullable: false, maxLength: 256),
                        HorarioElegido = c.String(nullable: false, maxLength: 50),
                        DiaReserva = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Reservas");
        }
    }
}
