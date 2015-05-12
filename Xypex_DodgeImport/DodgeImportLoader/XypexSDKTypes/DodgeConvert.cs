using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DodgeImportLoader
{
    public class DodgeConvert
    {
        public static int enumNotDefined = -100;

        public static DateTime ToDate(string datestring)
        {
            DateTime date = new DateTime(Convert.ToInt32(datestring.Substring(0, 4)), Convert.ToInt32(datestring.Substring(4, 2)), Convert.ToInt32(datestring.Substring(6, 2)));
            return date;
        }
        
        /// <summary>
        /// Returns a ktc_projectktc_project_owner_class enum value as int
        /// </summary>
        /// <param name="textValue"></param>
        /// <returns></returns>
        public static int GetKtcProjectOwnerClassEnum(string textValue)
        {
            switch (textValue)
            {
                case "Federal":
                    return (int) ktc_projectktc_project_owner_class.Federal;
                   
                case "LocalGovernment":
                    return (int)ktc_projectktc_project_owner_class.LocalGovernment;
                case "Military":
                    return (int)ktc_projectktc_project_owner_class.Military;
                case "Private":
                    return (int)ktc_projectktc_project_owner_class.Private;
                default:
                    return enumNotDefined;
            }

        }
    }
}
