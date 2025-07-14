namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSistemaSellos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CodigoQRs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Codigo = c.String(nullable: false),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ReservaId = c.String(nullable: false),
                        Servicio = c.String(nullable: false),
                        FechaGeneracion = c.DateTime(nullable: false),
                        FechaExpiracion = c.DateTime(nullable: false),
                        Validado = c.Boolean(nullable: false),
                        FechaValidacion = c.DateTime(),
                        ValidadoPor = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
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
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.Selloes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        CodigoQR = c.String(nullable: false),
                        Servicio = c.String(nullable: false),
                        FechaObtencion = c.DateTime(nullable: false),
                        ReservaId = c.String(nullable: false),
                        UsadoEnPase = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.ValidacionQRs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CodigoQR = c.String(nullable: false),
                        FechaIntento = c.DateTime(nullable: false),
                        Exitoso = c.Boolean(nullable: false),
                        DireccionIP = c.String(nullable: false),
                        MotivoFallo = c.String(),
                        ValidadoPor = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Selloes", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.PaseExpedicions", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.CodigoQRs", "UserId", "dbo.AspNetUsers");
            DropIndex("dbo.Selloes", new[] { "UserId" });
            DropIndex("dbo.PaseExpedicions", new[] { "UserId" });
            DropIndex("dbo.CodigoQRs", new[] { "UserId" });
            DropTable("dbo.ValidacionQRs");
            DropTable("dbo.Selloes");
            DropTable("dbo.PaseExpedicions");
            DropTable("dbo.CodigoQRs");
        }
    }
}
