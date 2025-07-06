namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddEstados : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Estadoes",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Nombre = c.String(nullable: false, maxLength: 50),
                })
                .PrimaryKey(t => t.Id);

        }

        public override void Down()
        {
            DropTable("dbo.Estadoes");
        }
    }
}
