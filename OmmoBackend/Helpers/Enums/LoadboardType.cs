namespace OmmoBackend.Helpers.Enums
{
    public enum LoadboardType
    {
        Truckstop = 0,
        DAT = 1
    }

    public static class LoadboardMapper
    {
        public static int ToDbId(this LoadboardType type)
        {
            return type switch
            {
                LoadboardType.DAT => 4,
                LoadboardType.Truckstop => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(type), $"Invalid LoadboardType: {type}")
            };
        }

        public static LoadboardType FromDbId(int dbId)
        {
            return dbId switch
            {
                4 => LoadboardType.DAT,
                3 => LoadboardType.Truckstop,
                _ => throw new ArgumentOutOfRangeException(nameof(dbId), $"Invalid DB Loadboard Id: {dbId}")
            };
        }
    }

}
