namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CreaciónTablas : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reviews", "Mostrar", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Reviews", "Mostrar");
        }
    }
}
