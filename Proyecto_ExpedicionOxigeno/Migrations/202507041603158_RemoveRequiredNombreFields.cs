namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveRequiredNombreFields : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Contactos", "Nombre", c => c.String());
            AlterColumn("dbo.Reviews", "Nombre", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Reviews", "Nombre", c => c.String(nullable: false));
            AlterColumn("dbo.Contactos", "Nombre", c => c.String(nullable: false, maxLength: 100));
        }
    }
}
