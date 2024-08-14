using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Conn.RiskEventSource.StormGeo.Core.Lightning;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace TestProject
{
	[TestClass]
	public class LightningMapperTest
	{
		public static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
		private const string TestInputFile = "TestProject.TestData.LightningAlert.txt";

		[TestMethod]
		public void LightningMapper_ReturnMappedObject() {
			var resourceStream = ExecutingAssembly.GetManifestResourceStream(TestInputFile);
			var testData = ReadDataFromFileStream(resourceStream);
			var result = JsonConvert.DeserializeObject<LightningAlerts>(testData);
		}

		private string ReadDataFromFileStream(Stream streamMultiPolygonData) {
			string fileData = null;
			if (streamMultiPolygonData != null)
				using (var expectedMultiPolygonData = new StreamReader(streamMultiPolygonData, Encoding.UTF8)) {
					fileData = expectedMultiPolygonData.ReadToEnd();
				}

			return fileData;
		}
	}
}
