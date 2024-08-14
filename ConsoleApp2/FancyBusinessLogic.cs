namespace jwright.Blog
{
    internal class FancyBusinessLogic
    {
        private IDataAccess _dataAccessObject;
        internal IDataAccess MyDataAccessObject
        {
            get { return _dataAccessObject ?? (_dataAccessObject = new DataAccessObject()); }
            set { _dataAccessObject = value; }
        }

        public ImportantData GetImportantDataAndUpdateTheName(int recordId)
        {
            var record = MyDataAccessObject.GetRecordFromDatabase(recordId);
            record.Name = "All Your Base Are Belong To Us";
            return record;
        }

        public void NullsNotWelcomeHere(object input)
        {
            if (input == null) { MyDataAccessObject.NeverCallThisMethod(); }
            

        }

    }
}
