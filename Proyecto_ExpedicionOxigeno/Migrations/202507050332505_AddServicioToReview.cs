namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddServicioToReview : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Reviews", "Servicio", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.Reviews", "Servicio");
        }
    }
}
