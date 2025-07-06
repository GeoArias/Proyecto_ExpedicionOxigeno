namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModificacionReviews : DbMigration
    {
        public override void Up()
        {
            // Establecer los valores nulos a false antes de cambiar el tipo
            Sql("UPDATE dbo.Reviews SET Mostrar = 'false' WHERE Mostrar IS NULL");

            // Cambiar el tipo de columna de string a bool (no nulo)
            AlterColumn("dbo.Reviews", "Mostrar", c => c.Boolean(nullable: false, defaultValue: false));

        }

        public override void Down()
        {
            AlterColumn("dbo.Reviews", "Mostrar", c => c.String());
        }
    }
}
