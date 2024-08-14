using System;

namespace jwright.Blog {
	public class ImportantData {
		public string Name { get; set; }
		public int RecordId { get; set; }

	}

	public interface IDataAccess {
		ImportantData GetRecordFromDatabase(int recordId);
		void NeverCallThisMethod();
	}

	public class DataAccessObject : IDataAccess {
		public ImportantData GetRecordFromDatabase(int recordId) {
			throw new NotImplementedException();
		}

		public void NeverCallThisMethod() {
			MyProductionOnlyCode();
			if (1 == 1)
				Console.Write("ssd");
		}

		private void MyProductionOnlyCode() {
			throw new NotImplementedException();
		}
	}
}