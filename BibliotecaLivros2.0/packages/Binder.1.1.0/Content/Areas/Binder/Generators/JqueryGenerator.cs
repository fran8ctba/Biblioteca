using Binder.Areas.Binder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Reflection;

namespace Binder.Areas.Binder.Generators
{
    public class JqueryGenerator : BinderGenerator
    {
        public static FileStream jqueryBindingZip(string absoluteUri)
        {
            //Create data singleton beginning
            var dataInterfaceFile = Path.GetFileName("DataInterface.java");
            var dataSingletonFile = Path.GetFileName("DataSingleton.java");
            var testDataFile = Path.GetFileName("TestData.java");
            var webserviceFile = Path.GetFileName("WebService.java");

            //Create folder structure
            var exportPath = HttpContext.Current.Server.MapPath("~/Binder-DataLayer/");
            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }
            Directory.CreateDirectory(exportPath);

            //Copy static files to libs folder
            var templatesFilePath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/Templates/JQuery/WebService.js");
            //There are not static files for jquery, but we will keep this here anyway for posterity
            /*
            var staticFilesPath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/StaticFiles/Jquery/");
            foreach (var file in Directory.GetFiles(staticFilesPath))
            {
                File.Copy(file, exportPath + "Libraries/" + Path.GetFileName(file));
            }*/

            //Get controller information
            List<string> webserviceMethods = new List<string>();
            List<string> testDataMethods = new List<string>();
            IEnumerable<ApiDescription> apiDescriptions = GlobalConfiguration.Configuration.Services.GetApiExplorer().ApiDescriptions;
            foreach (var apiDescription in apiDescriptions)
            {
                HelpPageApiModel apiModel = GlobalConfiguration.Configuration.GetHelpPageApiModel(apiDescription.GetFriendlyId());
                var methodDescription = apiModel.ApiDescription;

                //Create method base
                string methodName = methodNameForActionDescriptor(methodDescription);
                methodName = Char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
                string methodBase = "function " + methodName + "(";
                //Iterate over all parameters to establish binder input parameters
                string parameters = "";
                for (int i = 0; i < methodDescription.ParameterDescriptions.Count; i++)
                {
                    //Add datasingleton parameters, which are slightly different in format
                    string paramName = methodDescription.ParameterDescriptions[i].Name; //i.e. "name" or "response"

                    if (i == 0)
                    {
                        parameters = paramName;
                    }
                    else
                    {
                        parameters = parameters + ", " + paramName;
                    }
                }

                parameters = (parameters == "") ? "" : parameters + ", ";

                //Generate method signature
                string methodSignature = methodBase + parameters + "success, error)";

                //Create methods to be input to files
                webserviceMethods.Add(webserviceMethodForSignature(methodSignature, methodDescription));
            }

            //Get unminified text


            //Export new methods!
            string fileText = File.ReadAllText(templatesFilePath);
            fileText = fileText.Replace("<#JQueryMethods#>", "\n\t" + String.Join("\n\n\t", webserviceMethods.ToArray()));

            writeFileToTemplate(templatesFilePath, exportPath + "WebService.js", "\n\t" + String.Join("\n\n\t", webserviceMethods.ToArray()), "<#JQueryMethods#>", absoluteUri);

            //Create and export minified js
            //var minifier = new Microsoft.Ajax.Utilities.Minifier();
            //var minifiedText = minifier.MinifyJavaScript(fileText);
            //File.WriteAllText(exportPath + "WebService.min.js", minifiedText);

            return generateZip(exportPath);
        }

        #region Method Generation

        private static string webserviceMethodForSignature(string methodSignature, ApiDescription apiDescription)
        {
            var internalString = "";

            //Process body parameter
            ApiParameterDescription bodyParam = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Source.ToString() == "FromBody");
            string bodyParamString = (bodyParam != null) ? bodyParam.Name : "null";

            //Buld request url
            internalString += "\t\tvar requestUrl = serviceAddress+\"" + processedRequestUri(apiDescription);
            if (internalString.EndsWith(" + \"") || !(internalString.EndsWith("\"")))
            {
                internalString += "\"";
            }
            internalString += ";\n\n\t\t//Make Request\n";

            //Add the actual call
            var responseType = apiDescription.ResponseDescription.ResponseType;

            //Make request
            internalString += "\t\trequest(requestUrl, '" + apiDescription.HttpMethod.Method.ToLower() + "', " + bodyParamString + ", success, error)";

            return methodSignature + "{\n\n" + internalString + "\n\t}";
        }

        private static string processedRequestUri(ApiDescription apiDesciption)
        {
            //Get non-processed relative path
            var relativePath = apiDesciption.RelativePath;

            foreach (var parameterDescription in apiDesciption.ParameterDescriptions)
            {
                if (parameterDescription.Source.ToString() == "FromUri")
                {
                    relativePath = relativePath.Replace("{" + parameterDescription.Name + "}", "\" + " + parameterDescription.Name + " + \"");
                }
            }

            return relativePath;
        }

        #endregion

    }
}