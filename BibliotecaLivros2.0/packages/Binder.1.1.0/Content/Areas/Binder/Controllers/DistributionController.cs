using Binder.Areas.Binder.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;

namespace Binder.Areas.Binder.Controllers
{
    public class DistributionController : ApiController
    {
        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("api/binder/distribution")]
        public HttpResponseMessage getDistribution(string platform)
        {
            if (!String.IsNullOrEmpty(platform))
            {
                switch (platform.ToLower())
                {
                    case "objc":
                        return getObjectiveC();
                    case "swift":
                        return getSwift(Request.RequestUri.AbsoluteUri);
                    case "android":
                        return getAndroid(Request.RequestUri.AbsoluteUri);
                    case "jquery":
                        return getJquery(Request.RequestUri.AbsoluteUri);
                    default:
                        return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
            }
           
            return Request.CreateResponse(HttpStatusCode.BadRequest);
        }

         public HttpResponseMessage getObjectiveC() {



             return Request.CreateResponse(HttpStatusCode.BadRequest);
         }

         public HttpResponseMessage getSwift(string absoluteUri)
         {

             HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
             var stream = SwiftGenerator.swiftBindingZip(absoluteUri);
             result.Content = new StreamContent(stream);
             //stream.Close();
             result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
             result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
             {
                 FileName = "Binder-Swift.zip"
             };
             return result;
         }

         public HttpResponseMessage getAndroid(string absoluteUri)
         {
             HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
             var stream = AndroidGenerator.androidBindingZip(absoluteUri);
             result.Content = new StreamContent(stream);
             //stream.Close();
             result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
             result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
             {
                 FileName = "Binder-Android.zip"
             };
             return result;
         }

         public HttpResponseMessage getJquery(string absoluteUri)
         {
             HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
             var stream = JqueryGenerator.jqueryBindingZip(absoluteUri);
             result.Content = new StreamContent(stream);
             //stream.Close();
             result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
             result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
             {
                 FileName = "Binder-Jquery.zip"
             };
             return result;
         }
        
    }
}
