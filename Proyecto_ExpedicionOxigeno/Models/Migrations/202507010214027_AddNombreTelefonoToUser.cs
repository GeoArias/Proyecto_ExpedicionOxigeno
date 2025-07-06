namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddNombreTelefonoToUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Nombre", c => c.String());
            AddColumn("dbo.AspNetUsers", "Telefono", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Telefono");
            DropColumn("dbo.AspNetUsers", "Nombre");
        }
    }
}
