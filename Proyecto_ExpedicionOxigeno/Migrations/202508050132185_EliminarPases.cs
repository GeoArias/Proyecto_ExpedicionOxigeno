namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EliminarPases : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PaseExpedicions", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.PaseExpedicions", new[] { "UserId" });
            DropTable("dbo.PaseExpedicions");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.PaseExpedicions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        CodigoPase = c.String(nullable: false),
                        FechaGeneracion = c.DateTime(nullable: false),
                        FechaExpiracion = c.DateTime(nullable: false),
                        Utilizado = c.Boolean(nullable: false),
                        FechaUso = c.DateTime(),
                        SellosUsados = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateIndex("dbo.PaseExpedicions", "UserId");
            AddForeignKey("dbo.PaseExpedicions", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
    }
}
