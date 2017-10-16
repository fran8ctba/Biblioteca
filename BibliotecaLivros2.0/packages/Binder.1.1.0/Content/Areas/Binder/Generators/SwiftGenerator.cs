using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using Binder.Areas.Binder.Models;

namespace Binder.Areas.Binder.Generators
{
    public class SwiftGenerator : AppleGenerator
    {
        public static FileStream swiftBindingZip(string absoluteUri)
        {
            //Create data singleton beginning
            var dataSingletonFile = Path.GetFileName("DataSingleton.swift");
            var dataInterfaceFile = Path.GetFileName("DataInterface.swift");
            var testDataFile = Path.GetFileName("TestData.swift");
            var webserviceFile = Path.GetFileName("Webservice.swift");
            var dtosFile = Path.GetFileName("Dtos.h");

            //Create folder structure
            var exportPath = HttpContext.Current.Server.MapPath("~/Binder-DataLayer/");
            createTopLevelStructure(exportPath);

            //Copy static files to libs folder
            var templatesFilesPath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/Templates/iOS/Swift/");
            var staticFilesPath = HttpContext.Current.Server.MapPath("~/Areas/Binder/Generators/StaticFiles/iOS/Swift/");
            CopyFilesForDirectory(staticFilesPath, exportPath + "Libraries\\");

            //Move alamofire to top level
            Directory.Move(exportPath + "\\Libraries\\Alamofire", exportPath + "\\Alamofire");

            //Get controller information
            List<string> protocolMethods = new List<string>();
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
                string methodBase = "func " + methodName + "(";
                //Iterate over all parameters to establish binder input parameters
                string parameters = "";
                string dataSingletonParameters = "";
                for (int i = 0; i < methodDescription.ParameterDescriptions.Count; i++)
                {
                    //Add new parameter
                    parameters = parameters + swiftParameterForServiceParameter(methodDescription.ParameterDescriptions[i], ref dtosDictionary);
                    if (i < methodDescription.ParameterDescriptions.Count - 1)
                    {
                        parameters += ", ";
                    }

                    //Add datasingleton parameters, which are slightly different in format
                    string paramName = methodDescription.ParameterDescriptions[i].Name; //i.e. "name" or "response"
                    if (i == 0)
                    {
                        dataSingletonParameters = paramName;
                    }
                    else {
                        dataSingletonParameters = dataSingletonParameters + ", " + paramName + ": " + paramName;
                    }
                }

                //Generate method signature
                string methodSignature = methodBase + parameters + ") -> Request";
                ProcessReturnTypeForDescription(methodDescription, ref dtosDictionary);
                protocolMethods.Add(methodSignature);
                dataSingletonMethods.Add(dataSingletonMethodForSignature(methodSignature, dataSingletonParameters, methodName));
                testDataMethods.Add(testDataMethodForSignature(methodSignature));
                webserviceMethods.Add(webserviceMethodForSignature(methodSignature, methodDescription));
            }

            writeFileToTemplate(templatesFilesPath + "DataInterface.swift", exportPath + "DataInterface.swift", String.Join("\n\t", protocolMethods.ToArray()), "<#ProtocolMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "DataSingleton.swift", exportPath + "DataSingleton.swift", String.Join("\n\t", dataSingletonMethods.ToArray()), "<#DataSingletonMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "TestData.swift", exportPath + "TestData.swift", String.Join("\n\t", testDataMethods.ToArray()), "<#TestDataMethods#>", absoluteUri);
            writeFileToTemplate(templatesFilesPath + "BinderWebService.swift", exportPath + "BinderWebService.swift", String.Join("\n\t", webserviceMethods.ToArray()), "<#BinderServiceMethods#>", absoluteUri);

            exportSwiftDtos(exportPath + "/Data/Dtos/", dtosDictionary);

            //return null;
            return generateZip(HttpContext.Current.Server.MapPath("~/Binder-DataLayer"));
        }

        private static void CopyFilesForDirectory(string currentPath, string exportPath)
        {
            var asdf = Directory.GetFiles(currentPath);
            foreach (var file in Directory.GetFiles(currentPath))
            {
                var fdsafdsa = Path.GetFileName(file);
                File.Copy(file, exportPath + Path.GetFileName(file));
            }
            foreach (var directory in Directory.GetDirectories(currentPath))
            {
                Directory.CreateDirectory(exportPath + directory.Substring(directory.LastIndexOf('\\')) + "\\");
                CopyFilesForDirectory(directory + "\\", exportPath + directory.Substring(directory.LastIndexOf('\\') + 1) + "\\");
            }
        }

        private static string swiftParameterForServiceParameter(ApiParameterDescription parameterDescription, ref Dictionary<string, Type> dtosDictionary)
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
                return parameterDescription.Name + ": String";
            }
            else if (typeString == "int32" || typeString == "int64")
            {
                return parameterDescription.Name + ": Int";
            }
            else if (typeString == "bool" || typeString == "boolean")
            {
                return parameterDescription.Name + ": Bool";
            }
            else if (typeString == "double" || typeString == "float" || typeString == "decimal")
            {
                return parameterDescription.Name + ": Double";
            }
            else if (typeString == "datetime")
            {
                return parameterDescription.Name + ": NSDate";
            }
            else
            {
                //Custom object type
                dtosDictionary[type.Name] = type;
                return parameterDescription.Name + ": " + parameterDescription.ParameterDescriptor.ParameterType.UnderlyingSystemType.Name;
            }
        }

        private static string ProcessReturnTypeForDescription(ApiDescription apiDescription, ref Dictionary<string, Type> dtosDictionary)
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

                    return "response: ([" + listType + "]) -> Void";
                }
                else //Not a collection
                {
                    if (responseType.Name != "List`1")
                    {
                        dtosDictionary[responseType.FullName] = responseType;
                    }

                    return "response: (" + responseType.Name + ") -> Void";
                }

            }

            return "response: (Void) -> Void";
        }

        #region Method Generation

        private static string dataSingletonMethodForSignature(string methodSignature, string dataSingletonParameters, string methodName)
        {
            return methodSignature + " {\n\t\treturn dataSource!." + methodName + "(" + dataSingletonParameters + ")\n\t}\n";
        }

        private static string testDataMethodForSignature(string methodSignature)
        {
            string testDataMethodBody = "\n\t\tdispatch_async(dispatch_get_global_queue(DISPATCH_QUEUE_PRIORITY_BACKGROUND, 0), { () -> Void in\n\t\t\tsleep(1)\n\n\t\t\t/*Add and process your test data here*/\n\n\t\t\tdispatch_async(dispatch_get_main_queue(), { () -> Void in\n\n\t\t\t})\n\t\t})\n\t\treturn Alamofire.request(.GET, \"\")";

            return methodSignature + " {" + testDataMethodBody + "\n\t}\n";
        }

        private static string webserviceMethodForSignature(string methodSignature, ApiDescription apiDescription)
        {
            var internalString = "";

            //Process body parameter
            ApiParameterDescription bodyParam = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Source.ToString() == "FromBody");
            string bodyParamString = (bodyParam != null) ? bodyParam.Name + ".toJSON()" : "nil";

            //Buld request url
            internalString += "\t\tlet requestUrl = \"" + processedRequestUri(apiDescription);
            if (internalString.EndsWith(" + \"") || !(internalString.EndsWith("\"")))
            {
                internalString += "\"";
            }

            //Build request string
            internalString += "\n\n\t\t//Make Request\n";
            internalString += "\t\treturn Alamofire.request(." + apiDescription.HttpMethod.Method + ", requestUrl, parameters: " + bodyParamString + ", encoding: .JSON, headers: binderHeaders())";

            //Create doc format
            var responseType = "";
            if (apiDescription.ResponseDescription.ResponseType != null)
            {
                responseType = (apiDescription.ResponseDescription.ResponseType.Name == "List`1") ? "[" + apiDescription.ResponseDescription.ResponseType.GenericTypeArguments[0].UnderlyingSystemType.Name + "]" : apiDescription.ResponseDescription.ResponseType.Name;
            }
            else
            {
                responseType = "void";
            }

            var documentationHeader = "///Return Type: " + responseType + "\n\t";

            return documentationHeader + methodSignature + " {\n" + internalString + "\n\t}\n";
        }

        private static string processedRequestUri(ApiDescription apiDesciption)
        {
            //Get non-processed relative path
            var relativePath = "\\(binderWebServiceBaseAddress)/" + apiDesciption.RelativePath;

            foreach (var parameterDescription in apiDesciption.ParameterDescriptions)
            {
                if (parameterDescription.Source.ToString() == "FromUri")
                {
                    relativePath = relativePath.Replace("{" + parameterDescription.Name + "}", "\\(" + parameterDescription.Name + ")");
                }
            }

            return relativePath;
        }

        #endregion

        #region Export Dtos

        private static void exportSwiftDtos(string dtoPath, Dictionary<string, Type> dtosDictionary)
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
                exportSwiftDtos(dtoPath, inheritanceDtosDictionary);
            }
        }

        private static void createDtoForType(Type type, Dictionary<string, Type> dtosDictionary, ref Dictionary<string, Type> createdDtosDictionary, ref Dictionary<string, Type> inheritanceDtosDictionary, string dtoPath)
        {
            var fileName = type.Name + ".swift";
            createdDtosDictionary[type.Name] = type;

            //Check for inheritance
            var baseTypeString = "";
            var mappableString = "";
            if (!(type.BaseType.Name == "Object") && !isPrimitive(type.Name))
            {
                baseTypeString = " : " + type.BaseType.Name;
                inheritanceDtosDictionary[type.BaseType.FullName] = type.BaseType;
                mappableString = ", Mappable";
            }
            else
            {
                mappableString = ": Mappable";
            }

            //Begin class
            var fileText = "import Foundation\n\nclass " + type.Name + baseTypeString + mappableString + " {\n";

            //Iterate over all properties and continue building file
            var properties = type.GetProperties();
            var propertiesDictionary = new Dictionary<object, string>();
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
                                        fileText += "\tvar " + namedArgument.TypedValue.Value + ": " + SwiftPropertyForServiceProperty(property) + "?\n";
                                        AddObjectMapping(namedArgument.TypedValue.Value, SwiftPropertyForServiceProperty(property), ref propertiesDictionary);
                                    }
                                }
                            }
                        }

                        if (!jsonProperty)
                        {
                            fileText += "\tvar " + property.Name + ": " + SwiftPropertyForServiceProperty(property) + "?\n";
                            AddObjectMapping(property.Name, SwiftPropertyForServiceProperty(property), ref propertiesDictionary);
                        }

                        //If property is custom Dto, recur and add to created dtos dictionary
                        Type nestedCustomType = getNestedCustomType(property.PropertyType);
                        if (nestedCustomType != null && !isPrimitive(nestedCustomType.Name))
                        {
                            if (!createdDtosDictionary.ContainsKey(nestedCustomType.Name) && nestedCustomType.Name != "List`1")
                            {
                                createDtoForType(nestedCustomType, dtosDictionary, ref createdDtosDictionary, ref inheritanceDtosDictionary, dtoPath);
                            }
                        }
                    }
                }
            }

            //Create mappable protocol methods
            fileText += "\n\trequired init?(_ map: Map) {}\n\n";
            fileText += "\t// Mappable\n\tfunc mapping(map: Map) {\n";
            foreach (var item in propertiesDictionary)
            {
                fileText += "\t\t" + item.Key + " <- " + item.Value + "\n";
            }

            fileText += "\t}";

            //Close off class
            fileText += "}";

            //Write file
            File.WriteAllText(dtoPath + fileName, fileText);
        }

        private static void AddObjectMapping(object name, string type, ref Dictionary<object, string> propertiesDictionary)
        {
            if (type == "NSDate")
            {
                propertiesDictionary[name] = "(map[\"" + name + "\"], DateTransform())";
            }
            else
            {
                propertiesDictionary[name] = "map[\"" + name + "\"]";
            }
        }

        private static string SwiftPropertyForServiceProperty(PropertyInfo serviceProperty)
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
                if (typeString == "string" || typeString == "guid")
                {
                    return "String";
                }
                else if (typeString == "int32" || typeString == "int")
                {
                    return "Int";
                }
                else if (typeString == "int64" || typeString == "long")
                {
                    return "Int";
                }
                else if (typeString == "bool" || typeString == "boolean")
                {
                    return "Bool";
                }
                else if (typeString == "double" || typeString == "float" || typeString == "decimal")
                {
                    return "Double";
                }
                else if (typeString == "datetime")
                {
                    return "NSDate";
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
                    return "[" + collectionType.Name.Replace("[]", "") + "]";
                }
                else
                {
                    // var instantiatedClass = (object)Activator.CreateInstance(collectionType);
                    if (collectionType.Name == "List`1")
                    {
                        var listType = serviceProperty.PropertyType.GenericTypeArguments[0].UnderlyingSystemType.Name;

                        return "[" + SwiftPropertyForServicePropertyString(listType) + "]";
                    }
                    else
                    {
                        return serviceProperty.PropertyType.UnderlyingSystemType.Name;
                    }
                }
            }
        }

        private static bool isListOfPrimitives(object instantiatedClass)
        {
            if (instantiatedClass is IEnumerable<long> || instantiatedClass is IEnumerable<int> || instantiatedClass is IEnumerable<string> || instantiatedClass is IEnumerable<bool> || instantiatedClass is IEnumerable<short> || instantiatedClass is IEnumerable<float> || instantiatedClass is IEnumerable<double> || instantiatedClass is IEnumerable<decimal>)
            {
                return true;
            }

            return false;
        }

        private static string SwiftPropertyForServicePropertyString(string serviceProperty)
        {
            var lowerServiceProperty = serviceProperty.ToLower();

            if (lowerServiceProperty == "string" || lowerServiceProperty == "guid")
            {
                return "String";
            }
            else if (lowerServiceProperty == "int32")
            {
                return "Int";
            }
            else if (lowerServiceProperty == "int64")
            {
                return "Int";
            }
            else if (lowerServiceProperty == "bool" || lowerServiceProperty == "boolean")
            {
                return "Boolean";
            }
            else if (lowerServiceProperty == "double" || lowerServiceProperty == "float" || lowerServiceProperty == "decimal")
            {
                return "Double";
            }
            else if (lowerServiceProperty == "datetime")
            {
                return "NSDate";
            }
            else
            {
                return serviceProperty;
            }
        }

        #endregion
    }
}