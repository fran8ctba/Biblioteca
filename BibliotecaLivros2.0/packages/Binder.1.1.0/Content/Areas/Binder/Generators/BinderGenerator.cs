using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO.Compression;
using System.IO;
using System.Web.Http.Description;
using System.Text;

namespace Binder.Areas.Binder.Generators
{
    public class BinderGenerator
    {

        protected static FileStream generateZip(string startPath) {
            string zipPath = HttpContext.Current.Server.MapPath("~/") + "Binder-DataLayer.zip";

            //Delete the file, if necessary
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            //Create the zip and pass it back up the stream!
            ZipFile.CreateFromDirectory(startPath, zipPath, CompressionLevel.Fastest, false, new MyEncoder());
            return new FileStream(zipPath, FileMode.Open);
        }

        protected static void createTopLevelStructure (string path)
        {
            //Create top level data structure
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Directory.CreateDirectory(Path.Combine(path, "Data"));
            Directory.CreateDirectory(Path.Combine(path, "Data/Dtos"));
            Directory.CreateDirectory(Path.Combine(path, "Libraries"));
        }

        protected static string methodNameForActionDescriptor(ApiDescription description) {

            if (description.ActionDescriptor.ActionName.ToLower() == "get")
            {
                return "get" + description.ActionDescriptor.ControllerDescriptor.ControllerName;
            }
            else if (description.ActionDescriptor.ActionName.ToLower() == "post")
            {
                return "add" + description.ActionDescriptor.ControllerDescriptor.ControllerName;
            }
            else if (description.ActionDescriptor.ActionName.ToLower() == "put")
            {
                return "edit" + description.ActionDescriptor.ControllerDescriptor.ControllerName;
            }
            else if (description.ActionDescriptor.ActionName.ToLower() == "delete")
            {
                return "remove" + description.ActionDescriptor.ControllerDescriptor.ControllerName;
            }
            else return description.ActionDescriptor.ActionName;
        }

        protected static bool isPrimitive(string typeString) {
            var formattedType = typeString.ToLower();

            if (formattedType == "string" || formattedType == "char" || formattedType == "int" || formattedType == "int32" || formattedType == "int64" || formattedType == "long" || formattedType == "bool" || formattedType == "boolean" || formattedType == "float" || formattedType == "double" || formattedType == "decimal" || formattedType == "datetime" || formattedType == "guid")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected static void writeFileToTemplate(string templatePath, string finalPath, string contents, string stringToReplace, string absoluteUri)
        {
            if (File.Exists(templatePath))
            {
                //Copy file, if necessary
                if (!File.Exists(finalPath))
                {
                    File.Copy(templatePath, finalPath);
                }
                
                //Make edits
                string fileText = File.ReadAllText(finalPath);
                fileText = fileText.Replace(stringToReplace, contents);
                fileText = fileText.Replace("<#WebServiceAddress#>", absoluteUri.Substring(0, absoluteUri.IndexOf("/api/")));
                File.WriteAllText(finalPath, fileText);
            }
        }

        protected static Type getNestedCustomType(Type type)
        {
            var typeString =type.Name.ToLower();
            //Check for optional
            if (typeString == "nullable`1")
            {
                type = type.GenericTypeArguments[0].UnderlyingSystemType;
            }

            if (!(isPrimitive(type.Name.ToLower())))
            {
                var nestedType = type.Assembly.GetType(type.FullName);
                var instantiatedClass = (object)Activator.CreateInstance(nestedType);
                if (instantiatedClass is IEnumerable<object>)
                {
                    var listType = type.GenericTypeArguments[0].UnderlyingSystemType;
                    return listType;
                }
                else
                {
                    return type;
                }
            }

            return null;
        }
    }

    class MyEncoder : UTF8Encoding
    {
        public MyEncoder()
        {

        }
        public override byte[] GetBytes(string s)
        {
            s = s.Replace("\\", "/");
            return base.GetBytes(s);
        }
    }
}