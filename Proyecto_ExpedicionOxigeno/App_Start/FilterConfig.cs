﻿using System.Web.Mvc;

namespace Proyecto_ExpedicionOxigeno
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
