using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Binder.Areas.Binder.Generators
{
    public class ObjcGenerater : AppleGenerator
    {
        public static FileStream jqueryBindingZip()
        {
            return generateZip("");
        }
    }
}