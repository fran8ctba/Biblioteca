using System.Web.Mvc;

namespace Binder.Areas.Binder
{
    public class BinderAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Binder";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "Binder_default",
                "Binder/{controller}/{action}/{id}",
                new {controller="Binder", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}