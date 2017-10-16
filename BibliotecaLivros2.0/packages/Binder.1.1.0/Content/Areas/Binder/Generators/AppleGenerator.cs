using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace Binder.Areas.Binder.Generators
{
    public class AppleGenerator : BinderGenerator
    {
        #region Dtos

        public static void exportDtos(string dtoPath, Dictionary<string, Type> dtosDictionary)
        {
            Dictionary<string, Type> inheritanceDtosDictionary = new Dictionary<string, Type>();
            Dictionary<string, Type> createdDtosDictionary = new Dictionary<string, Type>();
            List<string> headerFileNames = new List<string>();
            foreach (var typeString in dtosDictionary.Keys)
            {
                createDtoForType(dtosDictionary[typeString], dtosDictionary, ref createdDtosDictionary, ref inheritanceDtosDictionary, ref headerFileNames, dtoPath);
            }

            //Make inherited classes dtos
            if (inheritanceDtosDictionary.Count() > 0)
            {
                exportDtos(dtoPath, inheritanceDtosDictionary);
            }

            writeHeaderFile(headerFileNames, dtoPath);
        }

        private static void createDtoForType(Type type, Dictionary<string, Type> dtosDictionary, ref Dictionary<string, Type> createdDtosDictionary, ref Dictionary<string, Type> inheritanceDtosDictionary, ref List<string> headerFileNames, string dtoPath)
        {
            // Keep track of all header file names
            Dictionary<string, string> arrayProperties = new Dictionary<string, string>();
            Dictionary<string, string> importProperties = new Dictionary<string, string>();

            var hFileName = type.Name + ".h";
            var mFileName = type.Name + ".m";
            createdDtosDictionary[type.Name] = type;

            //Add header file name to array
            if (!headerFileNames.Contains(hFileName))
            {
                headerFileNames.Add(hFileName);
            }

            //Begin class
            var hFileText = "\n@interface " + type.Name + " : NSObject\n\n";
            var mFileText = "#import \"" + hFileName + "\"\n\n@implementation " + type.Name + "\n\n";

            //Iterate over all properties and continue building file
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType != typeof(ExtensionDataObject))
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

                                    var propertyType = objcPropertyForServiceProperty(property, ref importProperties);

                                    hFileText += "@property " + propertyType + " " + namedArgument.TypedValue.Value + ";\n";

                                    if (propertyType.Equals("NSArray*"))
                                    {
                                        if (property.PropertyType.GetGenericArguments().Length == 0)
                                        {
                                            var listType = property.PropertyType.Name.Replace("[]", "");
                                            arrayProperties[namedArgument.TypedValue.Value + ""] = listType;
                                        }
                                        else
                                        {
                                            var listType = objcPropertyForServicePropertyString(property.PropertyType.GenericTypeArguments[0].UnderlyingSystemType.Name);
                                            arrayProperties[namedArgument.TypedValue.Value + ""] = listType;
                                        }

                                    }
                                }
                            }
                        }
                    }

                    if (!jsonProperty)
                    {
                        var propertyType = objcPropertyForServiceProperty(property, ref importProperties);

                        hFileText += "@property " + propertyType + " " + property.Name + ";\n";

                        if (propertyType.Equals("NSArray*"))
                        {
                            if (property.PropertyType.GetGenericArguments().Length == 0)
                            {
                                var listType = property.PropertyType.Name.Replace("[]", "");
                                arrayProperties[property.Name] = listType;
                            }
                            else
                            {
                                var listType = objcPropertyForServicePropertyString(property.PropertyType.GenericTypeArguments[0].UnderlyingSystemType.Name);
                                arrayProperties[property.Name] = listType;
                            }
                        }
                    }

                    //If property is custom Dto, recur and add to created dtos dictionary
                    Type nestedCustomType = getNestedCustomType(property.PropertyType);
                    if (nestedCustomType != null && !isPrimitive(nestedCustomType.Name))
                    {
                        if (!createdDtosDictionary.ContainsKey(nestedCustomType.Name))
                        {
                            createDtoForType(nestedCustomType, dtosDictionary, ref createdDtosDictionary, ref inheritanceDtosDictionary, ref headerFileNames, dtoPath);
                        }
                    }
                }
            }

            //Update .m file for arrays
            updateImplementationFileForArrays(ref mFileText, arrayProperties);

            foreach (var key in importProperties.Keys)
            {
                hFileText = importProperties[key] + hFileText;
            }

            hFileText = "#import <Foundation/Foundation.h>\n" + hFileText;

            //Close off class
            hFileText += "\n@end";
            mFileText += "\n@end";

            //Write files
            File.WriteAllText(dtoPath + hFileName, hFileText);
            File.WriteAllText(dtoPath + mFileName, mFileText);
        }

        private static void updateImplementationFileForArrays(ref String fileText, Dictionary<string, string> properties)
        {
            fileText += "- (id)init {\n\tself = [super init];\n\tif (self) {\n";

            foreach (var key in properties.Keys)
            {
                fileText += "\t\t[self setValue:@\"" + properties[key] + "\" forKeyPath:@\"propertyArrayMap." + key + "\"];\n";
            }

            fileText += "\t}\n\treturn self;\n}\n";
        }

        private static string objcPropertyForServiceProperty(PropertyInfo serviceProperty, ref Dictionary<string, string> importProperties)
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
                    return "NSString*";
                }
                else if (typeString == "int32" || typeString == "int")
                {
                    return "NSNumber*";
                }
                else if (typeString == "int64" || typeString == "long")
                {
                    return "NSNumber*";
                }
                else if (typeString == "bool" || typeString == "boolean")
                {
                    return "BOOL";
                }
                else if (typeString == "double" || typeString == "float" || typeString == "decimal")
                {
                    return "NSNumber*";
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


                if (collectionType.BaseType.Name == "Array")
                {
                    return "NSArray*";
                }
                else
                {
                    //var instantiatedClass = (object)Activator.CreateInstance(collectionType);
                    if (collectionType.Name == "List`1")
                    {
                        return "NSArray*";
                    }
                    else if (isListOfPrimitives(collectionType))
                    {
                        return "NSArray*";
                    }
                    else
                    {
                        //Custom class
                        importProperties[type.Name] = "#import \"" + type.Name + ".h\"\n";

                        return type.Name + "*";
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

        private static string objcPropertyForServicePropertyString(string serviceProperty)
        {
            var servicePropertyString = serviceProperty.ToLower();

            if (servicePropertyString == "string" || servicePropertyString == "datetime" || servicePropertyString == "guid")
            {
                return "NSString";
            }
            else if (servicePropertyString == "int32" || servicePropertyString == "int")
            {
                return "NSNumber";
            }
            else if (servicePropertyString == "int64" || servicePropertyString == "long")
            {
                return "NSNumber";
            }
            else if (servicePropertyString == "bool" || servicePropertyString == "boolean")
            {
                return "BOOL";
            }
            else if (servicePropertyString == "double" || servicePropertyString == "float" || servicePropertyString == "decimal")
            {
                return "NSNumber";
            }
            else if (servicePropertyString == "char")
            {
                return "char";
            }
            else
            {
                return serviceProperty;
            }
        }

        private static void writeHeaderFile(List<string> headerFileNames, string dtoPath)
        {
            // Create header file for easier imports in Swift/Obj-C projects
            var headersFileName = "BinderDtos.h";
            var headersFileText = "";

            foreach (var headerFileName in headerFileNames)
            {
                headersFileText += "#import \"" + headerFileName + "\"\n";
            }

            File.WriteAllText(dtoPath + headersFileName, headersFileText);
        }

        #endregion
    }
}