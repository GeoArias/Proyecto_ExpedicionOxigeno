namespace Proyecto_ExpedicionOxigeno.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Add_MSBookingsGUIDStaff : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "MsBookingsId", c => c.Guid(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "MsBookingsId");
        }
    }
}
