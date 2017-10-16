using Binder.Areas.Binder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using System.Reflection;
using System.Runtime.Serialization;

namespace Binder.Areas.Binder.Generators
{
    public class AndroidGenerator : BinderGenerator
    {
        public static FileStream androidBindingZip(string absoluteUri)
        {
            //Create data singleton beginning
            var dataInterfaceFile = Path.GetFileName("DataInterface.java");
            var dataSingletonFile = Path.GetFileName("DataSingleton.java");
            var testDataFile = Path.GetFileName("TestData.java");
            var webserviceFile = Path.GetFileName("WebService.java");

            //Create folder structure
            var exportPath = HttpContext.Current.Server.MapPath("~/Binder-DataLayer/");
            createTopLevelStructure(exportPath);

            //Copy static files to libs folder
            var templatesFilesPath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/Templates/Android/");
            var staticFilesPath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/StaticFiles/Android/");
            foreach (var file in Directory.GetFiles(staticFilesPath))
            {
                File.Copy(file, exportPath+"Libraries/"+Path.GetFileName(file));
            }

            //Get controller information
            List<string> interfaceMethods = new List<string>();
            List<string> dataSingletonMethods = new List<string>();
            List<string> webserviceMethods = new List<string>();
            List<string> testDataMethods = new List<string>();
            Dictionary<string, Type> dtosDictionary = new Dictionary<string, Type>();
            IEnumerable<ApiDescription> apiDescriptions = GlobalConfiguration.Configuration.Services.GetApiExplorer().ApiDescriptions;
            foreach (var apiDescription in apiDescriptions)
            {
                HelpPageApiModel apiModel = GlobalConfiguration.Configuration.GetHelpPageApiModel(apiDescription.GetFriendlyId());
                var methodDescription = apiModel.ApiDescription;
            
                //Create method base
                string methodName = methodNameForActionDescriptor(methodDescription);
                methodName = Char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
                string methodBase = "public void " + methodName + "(";
                //Iterate over all parameters to establish binder input parameters
                string parameters = "";
                string dataSingletonParameters = "";
                for (int i = 0; i < methodDescription.ParameterDescriptions.Count; i++)
                {
                    //Add new parameter
                    parameters = parameters + parameterForServiceParameter(methodDescription.ParameterDescriptions[i], ref dtosDictionary) + ", ";

                    //Add datasingleton parameters, which are slightly different in format
                    string paramName = methodDescription.ParameterDescriptions[i].Name; //i.e. "name" or "response"
                    if (i == 0)
                    {
                        dataSingletonParameters = paramName;
                    }
                    else
                    {
                        dataSingletonParameters = dataSingletonParameters + ", " + paramName;
                    }
                }

                //Generate method signature
                string methodSignature = methodBase + parameters + returnTypeForDescription(methodDescription, ref dtosDictionary) + ")";

                //Create methods to be input to files
                interfaceMethods.Add(methodSignature + ";");
                dataSingletonMethods.Add(dataSingletonMethodForSignature(methodSignature, dataSingletonParameters, methodName));
                webserviceMethods.Add(webserviceMethodForSignature(methodSignature, methodDescription));
                testDataMethods.Add(testDataMethodForSignature(methodSignature, methodDescription));
            }

            //Export new methods!
            writeFileToTemplate(templatesFilesPath + "DataInterface.java", exportPath + "DataInterface.java", String.Join("\n\t", interfaceMethods.ToArray()), "<#DataInterfaceMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "DataSingleton.java", exportPath + "DataSingleton.java", "@Override\n\t" + String.Join("\n\n\t@Override\n\t", dataSingletonMethods.ToArray()), "<#DataSingletonMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "TestData.java", exportPath + "TestData.java", "@Override\n\t" + String.Join("\n\n\t@Override\n\t", testDataMethods.ToArray()), "<#TestDataMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "WebService.java", exportPath + "WebService.java", "@Override\n\t" + String.Join("\n\n\t@Override\n\t", webserviceMethods.ToArray()), "<#WebServiceMethods#>", absoluteUri);
            exportDtos(exportPath + "/Data/Dtos/", dtosDictionary);

            return generateZip(exportPath);
        }

        #region Method Generation
        private static string parameterForServiceParameter(ApiParameterDescription parameterDescription, ref Dictionary<string, Type> dtosDictionary)
        {
            Type type = parameterDescription.ParameterDescriptor.ParameterType.UnderlyingSystemType;
            string typeString = parameterDescription.ParameterDescriptor.ParameterType.UnderlyingSystemType.Name.ToLower();
            //Check for optional
            if (typeString == "nullable`1")
            {
                type = parameterDescription.ParameterDescriptor.ParameterType.GenericTypeArguments[0].UnderlyingSystemType;
                typeString = parameterDescription.ParameterDescriptor.ParameterType.GenericTypeArguments[0].UnderlyingSystemType.Name.ToLower();
            }

            if (typeString == "string" || typeString == "datetime" || typeString == "guid")
            {
                return "String " + parameterDescription.Name;
            }
            else if (typeString == "int32" || typeString == "int")
            {
                return "int " + parameterDescription.Name;
            }
            else if (typeString == "int64" || typeString == "long")
            {
                return "long " + parameterDescription.Name;
            }
            else if (typeString == "bool" || typeString == "boolean")
            {
                return "boolean " + parameterDescription.Name;
            }
            else if (typeString == "double" || typeString == "float" || typeString == "decimal")
            {
                return "double " + parameterDescription.Name;
            }
            else
            {
                //Custom object type
                dtosDictionary[type.Name] = type;
                return parameterDescription.ParameterDescriptor.ParameterType.UnderlyingSystemType.Name +" "+ parameterDescription.Name;
            }
        }

        private static string returnTypeForDescription(ApiDescription apiDescription, ref Dictionary<string, Type> dtosDictionary)
        {
            var responseType = apiDescription.ResponseDescription.ResponseType;

            if (responseType != null)
            {
                //Check to see if it is a collection
                var type = responseType.Assembly.GetType(responseType.FullName);
                var instantiatedClass = (object)Activator.CreateInstance(type);
                if (instantiatedClass is IEnumerable<object>)
                {
                    var listType = responseType.GenericTypeArguments[0].UnderlyingSystemType.Name;

                    //If not primitive type, add type to be exported as Dto
                    if (!isPrimitive(listType))
                    {
                        dtosDictionary[responseType.GenericTypeArguments[0].UnderlyingSystemType.FullName] = responseType.GenericTypeArguments[0].UnderlyingSystemType;
                    }

                    return "Response.Listener<ArrayList<" + listType + ">> listener, Response.ErrorListener errorListener";
                }
                else //Not a collection
                {
                    dtosDictionary[responseType.FullName] = responseType;
                    return "Response.Listener<" + responseType.Name + "> listener, Response.ErrorListener errorListener";
                }
                
            }

            return "Response.Listener<Boolean> listener, Response.ErrorListener errorListener";
        }

        private static string dataSingletonMethodForSignature(string methodSignature, string dataSingletonParameters, string methodName)
        {
            var parameters = (!String.IsNullOrEmpty(dataSingletonParameters)) ? dataSingletonParameters+", " : dataSingletonParameters;
            return methodSignature + " {\n\t\tthis.dataSource." + methodName + "(" + parameters + "listener, errorListener);\n\t}\n";
        }

        private static string webserviceMethodForSignature(string methodSignature, ApiDescription apiDescription)
        {
            var internalString = "";

            //Process body parameter
            ApiParameterDescription bodyParam = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Source.ToString() == "FromBody");
            string bodyParamString = (bodyParam != null) ? bodyParam.Name : "null";

            //Buld request url
            internalString += "\t\tString requestUrl = \"" + processedRequestUri(apiDescription);
            if (internalString.EndsWith(" + \"") || !(internalString.EndsWith("\"")))
            {
                internalString += "\"";
            }
            internalString += ";\n\n\t\t//Make Request\n";

            //Add the actual call
            var responseType = apiDescription.ResponseDescription.ResponseType;
            if (responseType != null)
            {
                var type = responseType.Assembly.GetType(responseType.FullName);
                var instantiatedClass = (object)Activator.CreateInstance(type);
                if (instantiatedClass is IEnumerable<object>)
                {
                    var listType = responseType.GenericTypeArguments[0].UnderlyingSystemType.Name;
                    //Make request
                internalString += "\t\tnew JsonBinderRequest<ArrayList<"+listType+">>(Request.Method."
                    + apiDescription.HttpMethod.Method
                    + ", requestUrl, " + bodyParamString + ", new TypeToken<ArrayList<" + listType + ">>(){}.getType(), this.queue, listener, errorListener);";
                }
                else {
                    //Make request
                internalString += "\t\tnew JsonBinderRequest<"+responseType.Name+">(Request.Method."
                    + apiDescription.HttpMethod.Method
                    + ", requestUrl, " + bodyParamString + ", " + responseType.Name + ".class, this.queue, listener, errorListener);";
                }
                
            }
            else
            {
                //Make request
                internalString += "\t\tnew BooleanBinderRequest(Request.Method."
                    + apiDescription.HttpMethod.Method
                    + ", requestUrl, " + bodyParamString + ", this.queue, listener, errorListener);";
            }

            return methodSignature + "{\n\n" + internalString + "\n\t}";
        }

        private static string testDataMethodForSignature(string methodSignature, ApiDescription apiDescription)
        {
            var responseType = apiDescription.ResponseDescription.ResponseType;
            var responseString = (responseType != null) ? "null" : "true";

            return methodSignature + " {\n\t\tlistener.onResponse("+responseString+");\n\t}";
        }

        private static string processedRequestUri(ApiDescription apiDesciption)
        {
            //Get non-processed relative path
            var relativePath = apiDesciption.RelativePath;

            foreach (var parameterDescription in apiDesciption.ParameterDescriptions)
            {
                if (parameterDescription.Source.ToString() == "FromUri")
                {
                    relativePath = relativePath.Replace("{" + parameterDescription.Name + "}", "\" + " + parameterDescription.Name+" + \"");
                }
            }

            return relativePath;
        }

        #endregion

        #region Dtos

        private static void exportDtos(string dtoPath, Dictionary<string, Type> dtosDictionary)
        {
            Dictionary<string, Type> inheritanceDtosDictionary = new Dictionary<string, Type>();
            Dictionary<string, Type> createdDtosDictionary = new Dictionary<string, Type>();
            foreach (var typeString in dtosDictionary.Keys)
            {
                createDtoForType(dtosDictionary[typeString], dtosDictionary, ref createdDtosDictionary, ref inheritanceDtosDictionary, dtoPath);
            }

            //Make inherited classes dtos
            if (inheritanceDtosDictionary.Count() > 0)
            {
                exportDtos(dtoPath, inheritanceDtosDictionary);
            }
        }

        private static void createDtoForType(Type type, Dictionary<string, Type> dtosDictionary, ref Dictionary<string, Type> createdDtosDictionary, ref Dictionary<string, Type> inheritanceDtosDictionary, string dtoPath)
        {
            var fileName = type.Name + ".java";
            createdDtosDictionary[type.Name] = type;

            //Check for inheritance
            var baseTypeString = "";
            if (!(type.BaseType.Name == "Object") && !isPrimitive(type.Name))
            {
                baseTypeString = " extends " + type.BaseType.Name;
                inheritanceDtosDictionary[type.BaseType.FullName] = type.BaseType;
            }

            //Begin class
            var fileText = "\n\npublic class " + type.Name + baseTypeString + " {\n";

            //Iterate over all properties and continue building file
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var asdfasdf = property.PropertyType;
                if (property.PropertyType != typeof(ExtensionDataObject))
                {
                    if (type == property.DeclaringType)
                    {
                        bool jsonProperty = false;
                        foreach (var customAttribute in property.CustomAttributes)
                        {
                            if (customAttribute.AttributeType.FullName == "Newtonsoft.Json.JsonPropertyAttribute")
                            {
                                foreach (var namedArgument in customAttribute.NamedArguments)
                                {
                                    if (namedArgument.MemberName == "PropertyName")
                                    {
                                        jsonProperty = true;
                                        fileText += "\tpublic " + javaPropertyForServiceProperty(property) + " " + namedArgument.TypedValue.Value + ";\n";
                                    }
                                }
                            }
                        }

                        if (!jsonProperty)
                        {
                            fileText += "\tpublic " + javaPropertyForServiceProperty(property) + " " + property.Name + ";\n";
                        }

                        //If property is custom Dto, recur and add to created dtos dictionary
                        Type nestedCustomType = getNestedCustomType(property.PropertyType);
                        if (nestedCustomType != null)
                        {
                            if (!createdDtosDictionary.ContainsKey(nestedCustomType.Name))
                            {
                                createDtoForType(nestedCustomType, dtosDictionary, ref createdDtosDictionary, ref inheritanceDtosDictionary, dtoPath);
                            }
                        }
                    }
                }
            }

            //Close off class
            fileText += "}";

            //Check for ArrayList in file 
            if (fileText.Contains("ArrayList<"))
            {
                fileText = "\nimport java.util.ArrayList;\n" + fileText;
            }

            //Write file
            File.WriteAllText(dtoPath + fileName, fileText);
        }

        private static string javaPropertyForServiceProperty(PropertyInfo serviceProperty)
        {

            Type type = serviceProperty.PropertyType.UnderlyingSystemType;
            var typeString = serviceProperty.PropertyType.UnderlyingSystemType.Name.ToLower();
            //Check for optional
            if (typeString == "nullable`1")
            {
                type = serviceProperty.PropertyType.UnderlyingSystemType.GenericTypeArguments[0].UnderlyingSystemType;
                typeString = serviceProperty.PropertyType.UnderlyingSystemType.GenericTypeArguments[0].UnderlyingSystemType.Name.ToLower();
            }

            if (isPrimitive(typeString))
            {
                if (typeString == "string" || typeString == "datetime" || typeString == "guid")
                {
                    return "String";
                }
                else if (typeString == "int32" || typeString == "int")
                {
                    return "int";
                }
                else if (typeString == "int64" || typeString == "long")
                {
                    return "long";
                }
                else if (typeString == "bool" || typeString == "boolean")
                {
                    return "boolean";
                }
                else if (typeString == "double" || typeString == "float" || typeString == "decimal")
                {
                    return "double";
                }
                else
                {
                    return "unknownType";
                }
            }
            else
            {
                //Check for 
                var collectionType = serviceProperty.PropertyType.UnderlyingSystemType.Assembly.GetType(serviceProperty.PropertyType.UnderlyingSystemType.FullName);
                //var collectionType = Type.GetType(serviceProperty.PropertyType.UnderlyingSystemType.FullName);

                if (collectionType.BaseType.Name == "Array")
                {
                    return "ArrayList<" + collectionType.Name.Replace("[]", "") + ">";
                }
                else
                {
                    // var instantiatedClass = (object)Activator.CreateInstance(collectionType);
                    if (collectionType.Name == "List`1")
                    {
                        var listType = serviceProperty.PropertyType.GenericTypeArguments[0].UnderlyingSystemType.Name;

                        return "ArrayList<" + javaPropertyForServicePropertyString(listType) + ">";
                    }
                    else
                    {
                        return serviceProperty.PropertyType.UnderlyingSystemType.Name;
                    }
                }
            }
        }

        private static bool isListOfPrimitives(object instantiatedClass) {
            if (instantiatedClass is IEnumerable<long> || instantiatedClass is IEnumerable<int> || instantiatedClass is IEnumerable<string> || instantiatedClass is IEnumerable<bool> || instantiatedClass is IEnumerable<short> || instantiatedClass is IEnumerable<float> || instantiatedClass is IEnumerable<double> || instantiatedClass is IEnumerable<decimal>)
	        {
                return true;
	        }

            return false;
        }

        private static string javaPropertyForServicePropertyString(string serviceProperty)
        {
            var lowerServiceProperty = serviceProperty.ToLower();

            if (lowerServiceProperty == "string" || lowerServiceProperty == "datetime" || lowerServiceProperty == "guid")
            {
                return "String";
            }
            else if (lowerServiceProperty == "int32")
            {
                return "Integer";
            }
            else if (lowerServiceProperty == "int64")
            {
                return "Long";
            }
            else if (lowerServiceProperty == "bool" || lowerServiceProperty == "boolean")
            {
                return "Boolean";
            }
            else if (lowerServiceProperty == "double" || lowerServiceProperty == "float" || lowerServiceProperty == "decimal")
            {
                return "Double";
            }
            else
            {
                return serviceProperty;
            }
        }

        #endregion
    }
}