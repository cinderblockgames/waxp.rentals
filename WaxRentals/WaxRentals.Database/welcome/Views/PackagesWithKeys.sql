CREATE VIEW [welcome].[PackagesWithKeys]
AS
	SELECT PackageWithKeys.*, Address
	FROM welcome.PackageWithKeys
		 LEFT JOIN welcome.AddressWithKeys ON PackageWithKeys.PackageWithKeysId = AddressWithKeys.AddressWithKeysId;
