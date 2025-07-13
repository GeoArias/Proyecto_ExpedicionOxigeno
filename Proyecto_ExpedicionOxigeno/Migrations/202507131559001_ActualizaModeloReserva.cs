namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizaModeloReserva : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reservas", "HoraInicio", c => c.DateTime(nullable: false));
            AddColumn("dbo.Reservas", "HoraFin", c => c.DateTime(nullable: false));
            DropColumn("dbo.Reservas", "HorarioElegido");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Reservas", "HorarioElegido", c => c.String(nullable: false, maxLength: 50));
            DropColumn("dbo.Reservas", "HoraFin");
            DropColumn("dbo.Reservas", "HoraInicio");
        }
    }
}
