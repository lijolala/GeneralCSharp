//using CodeBeautify;

using Conn.RiskEventSource.Dataminr.ApiAccess.Models;

//using Conn.RiskEventSource.Dataminr.ApiAccess.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diagnostics {
	class Program {
		static void Main(string[] args)
		{
			
			string str = System.IO.File.ReadAllText(@"D:\Codes\GeneralCSharp\Diagnostics\jsonText.txt");
			JObject jObject = JObject.Parse(str);
			var res =  JsonConvert.DeserializeObject<DataminrRoot>(str);

		}
	}
}
